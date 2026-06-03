using CRM.Api.Infrastructure.Google;
using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Calendar.v3;
using Google.Apis.Calendar.v3.Data;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

public sealed class GoogleSyncService : IGoogleSyncService
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/gmail.readonly",
        "https://www.googleapis.com/auth/calendar.readonly",
    ];

    private readonly GoogleSettings _settings;
    private readonly IUserGoogleTokenRepository _tokenRepo;
    private readonly IActivityRepository _activityRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IGoogleTokenProtector _protector;
    private readonly ILogger<GoogleSyncService> _logger;

    public GoogleSyncService(
        IOptions<GoogleSettings> settings,
        IUserGoogleTokenRepository tokenRepo,
        IActivityRepository activityRepo,
        ICustomerRepository customerRepo,
        IGoogleTokenProtector protector,
        ILogger<GoogleSyncService> logger
    )
    {
        _settings = settings.Value;
        _tokenRepo = tokenRepo;
        _activityRepo = activityRepo;
        _customerRepo = customerRepo;
        _protector = protector;
        _logger = logger;
    }

    public async Task RegisterCalendarWatchAsync(
        string userId,
        string organizationId,
        UserCredential credential,
        CancellationToken ct = default
    )
    {
        var service = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CRM360",
            }
        );

        var channel = new Google.Apis.Calendar.v3.Data.Channel
        {
            Id = Guid.NewGuid().ToString(),
            Type = "web_hook",
            Address = $"{_settings.BaseUrl}/api/webhooks/calendar",
            Expiration = DateTimeOffset.UtcNow.AddDays(7).ToUnixTimeMilliseconds(),
        };

        await service.Events.Watch(channel, "primary").ExecuteAsync(ct);
    }

    public async Task SyncAllConnectedUsersAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId))
            return;

        var tokens = await _tokenRepo.FindAllWithSyncEnabledAsync(ct);
        foreach (var token in tokens)
        {
            if (token.calendarSyncEnabled)
            {
                try
                {
                    await SyncCalendarForUserAsync(token.userId, token.organizationId, ct);
                }
                catch (Exception)
                {
                    // Log and continue with next user
                }
            }
        }
    }

    public async Task SyncCalendarForUserAsync(
        string userId,
        string organizationId,
        CancellationToken ct = default
    )
    {
        var tokenDoc = await FindTokenAsync(userId, organizationId, ct);
        if (tokenDoc == null || !tokenDoc.calendarSyncEnabled)
            return;

        var credential = await BuildCredentialAsync(tokenDoc, ct);
        var service = new CalendarService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CRM360",
            }
        );

        var request = service.Events.List("primary");
        request.SingleEvents = true;
        request.OrderBy = EventsResource.ListRequest.OrderByEnum.StartTime;
        request.MaxResults = 100;
        if (!string.IsNullOrEmpty(tokenDoc.calendarSyncToken))
            request.SyncToken = tokenDoc.calendarSyncToken;
        else
            request.TimeMinDateTimeOffset = DateTimeOffset.UtcNow.AddDays(-30);

        Events events;
        try
        {
            events = await request.ExecuteAsync(ct);
        }
        catch (Google.GoogleApiException ex)
            when (ex.HttpStatusCode == System.Net.HttpStatusCode.Gone)
        {
            tokenDoc.calendarSyncToken = null;
            await _tokenRepo.UpsertAsync(tokenDoc, ct);
            return;
        }

        foreach (var ev in events.Items ?? [])
        {
            var occurredAt =
                ev.Start?.DateTimeDateTimeOffset?.UtcDateTime
                ?? (ev.Start?.Date != null ? DateTime.Parse(ev.Start.Date) : (DateTime?)null);
            if (occurredAt == null)
                continue;

            var emails = CollectEmails(ev);
            foreach (var email in emails)
            {
                var customer = await _customerRepo.FindByContactEmailInOrgAsync(
                    organizationId,
                    email,
                    ct
                );
                if (customer == null)
                    continue;

                var externalId = $"cal:{ev.Id}";
                if (
                    await _activityRepo.FindByExternalIdInOrgAsync(organizationId, externalId, ct)
                    != null
                )
                    continue;

                var activity = new Activity
                {
                    organizationId = organizationId,
                    customerId = customer.id,
                    departmentId = customer.departmentId,
                    type = ActivityTypes.Meeting,
                    source = ActivitySources.Calendar,
                    isAutoSync = true,
                    externalId = externalId,
                    createdBy = userId,
                    occurredAt = occurredAt.Value,
                    createdAt = DateTime.UtcNow,
                    updatedAt = DateTime.UtcNow,
                    summary = ev.Summary ?? "Calendar event",
                    location = ev.Location,
                    attendees = emails.ToList(),
                };

                await _activityRepo.InsertForOrgAsync(activity, ct);
                break;
            }
        }

        if (!string.IsNullOrEmpty(events.NextSyncToken))
        {
            tokenDoc.calendarSyncToken = events.NextSyncToken;
            await _tokenRepo.UpsertAsync(tokenDoc, ct);
        }

        await EnsureGmailWatchAsync(tokenDoc, credential, ct);
    }

    public async Task ProcessGmailNotificationAsync(
        string emailAddress,
        string historyId,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_settings.ClientId))
            return;

        var tokens = await _tokenRepo.FindAllWithSyncEnabledAsync(ct);
        var tokenDoc = tokens.FirstOrDefault(t =>
            t.googleEmail.Equals(emailAddress, StringComparison.OrdinalIgnoreCase)
            && t.gmailSyncEnabled
        );

        if (tokenDoc == null)
            return;

        var credential = await BuildCredentialAsync(tokenDoc, ct);
        var gmail = new GmailService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CRM360",
            }
        );

        var startHistoryId = tokenDoc.gmailHistoryId ?? historyId;
        var historyRequest = gmail.Users.History.List("me");
        historyRequest.StartHistoryId = ulong.Parse(startHistoryId);

        Google.Apis.Gmail.v1.Data.ListHistoryResponse history;
        try
        {
            history = await historyRequest.ExecuteAsync(ct);
        }
        catch
        {
            return;
        }

        foreach (var record in history.History ?? [])
        {
            foreach (var added in record.MessagesAdded ?? [])
            {
                if (added.Message?.Id == null)
                    continue;
                try
                {
                    await UpsertGmailMessageAsync(gmail, tokenDoc, added.Message.Id, ct);
                }
                catch (Google.GoogleApiException ex)
                    when (ex.HttpStatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Message {MessageId} not found, skipping", added.Message.Id);
                }
            }
        }

        tokenDoc.gmailHistoryId = historyId;
        await _tokenRepo.UpsertAsync(tokenDoc, ct);
    }

    private async Task UpsertGmailMessageAsync(
        GmailService gmail,
        UserGoogleToken tokenDoc,
        string messageId,
        CancellationToken ct
    )
    {
        var externalId = $"gmail:{messageId}";
        if (
            await _activityRepo.FindByExternalIdInOrgAsync(tokenDoc.organizationId, externalId, ct)
            != null
        )
            return;

        var message = await gmail.Users.Messages.Get("me", messageId).ExecuteAsync(ct);
        var headers = message.Payload?.Headers ?? [];
        var subject = headers.FirstOrDefault(h => h.Name == "Subject")?.Value ?? "(No subject)";
        var from = headers.FirstOrDefault(h => h.Name == "From")?.Value ?? string.Empty;
        var to = headers.FirstOrDefault(h => h.Name == "To")?.Value ?? string.Empty;

        var contactEmails = ExtractEmails(from).Concat(ExtractEmails(to)).Distinct();
        foreach (var email in contactEmails)
        {
            if (email.Equals(tokenDoc.googleEmail, StringComparison.OrdinalIgnoreCase))
                continue;

            var customer = await _customerRepo.FindByContactEmailInOrgAsync(
                tokenDoc.organizationId,
                email,
                ct
            );
            if (customer == null)
                continue;

            var direction = from.Contains(tokenDoc.googleEmail, StringComparison.OrdinalIgnoreCase)
                ? "outbound"
                : "inbound";

            var activity = new Activity
            {
                organizationId = tokenDoc.organizationId,
                customerId = customer.id,
                departmentId = customer.departmentId,
                type = ActivityTypes.Email,
                source = ActivitySources.Gmail,
                isAutoSync = true,
                externalId = externalId,
                createdBy = tokenDoc.userId,
                occurredAt = DateTimeOffset
                    .FromUnixTimeMilliseconds(message.InternalDate ?? 0)
                    .UtcDateTime,
                createdAt = DateTime.UtcNow,
                updatedAt = DateTime.UtcNow,
                subject = subject,
                summary = message.Snippet,
                direction = direction,
            };

            try
            {
                await _activityRepo.InsertForOrgAsync(activity, ct);
            }
            catch (MongoWriteException ex)
                when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Race condition — bỏ qua, record đã được insert bởi request khác
            }
            return;
        }
    }

    private async Task EnsureGmailWatchAsync(
        UserGoogleToken tokenDoc,
        UserCredential credential,
        CancellationToken ct
    )
    {
        if (!tokenDoc.gmailSyncEnabled || string.IsNullOrWhiteSpace(_settings.PubSubTopic))
            return;
        if (tokenDoc.gmailWatchExpiration > DateTime.UtcNow.AddHours(1))
            return;

        var gmail = new GmailService(
            new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "CRM360",
            }
        );

        var watchRequest = new WatchRequest
        {
            TopicName = _settings.PubSubTopic,
            LabelIds = ["INBOX", "SENT"],
        };
        var response = await gmail.Users.Watch(watchRequest, "me").ExecuteAsync(ct);

        tokenDoc.gmailWatchExpiration = DateTimeOffset
            .FromUnixTimeMilliseconds(response.Expiration ?? 0)
            .UtcDateTime;
        tokenDoc.gmailHistoryId = response.HistoryId?.ToString();
        await _tokenRepo.UpsertAsync(tokenDoc, ct);
    }

    private async Task<UserGoogleToken?> FindTokenAsync(
        string userId,
        string organizationId,
        CancellationToken ct
    )
    {
        var tokens = await _tokenRepo.FindAllWithSyncEnabledAsync(ct);
        return tokens.FirstOrDefault(t => t.userId == userId && t.organizationId == organizationId);
    }

    private async Task<UserCredential> BuildCredentialAsync(
        UserGoogleToken tokenDoc,
        CancellationToken ct
    )
    {
        var flow = new GoogleAuthorizationCodeFlow(
            new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = _settings.ClientId,
                    ClientSecret = _settings.ClientSecret,
                },
                Scopes = Scopes,
            }
        );

        var refreshToken = _protector.Unprotect(tokenDoc.encryptedRefreshToken);
        if (string.IsNullOrWhiteSpace(refreshToken))
            throw new InvalidOperationException(
                $"RefreshToken is empty for user {tokenDoc.userId}"
            );
        var token = new TokenResponse
        {
            AccessToken = tokenDoc.accessToken,
            RefreshToken = refreshToken,
            ExpiresInSeconds = tokenDoc.accessTokenExpiresAt.HasValue
                ? Math.Max(
                    0,
                    (long)(tokenDoc.accessTokenExpiresAt.Value - DateTime.UtcNow).TotalSeconds
                )
                : 0,
            IssuedUtc = DateTime.UtcNow, // ✅ FIX: cần set để SDK tính IsStale đúng
        };

        var credential = new UserCredential(flow, tokenDoc.userId, token);
        if (credential.Token.IsStale)
            credential.Token.RefreshToken = refreshToken;
        await credential.RefreshTokenAsync(ct);
        var newRefreshToken = credential.Token.RefreshToken;
        if (!string.IsNullOrWhiteSpace(newRefreshToken) && newRefreshToken != refreshToken)
            tokenDoc.encryptedRefreshToken = _protector.Protect(newRefreshToken);

        tokenDoc.accessToken = credential.Token.AccessToken;
        tokenDoc.accessTokenExpiresAt = credential.Token.IssuedUtc.AddSeconds(
            credential.Token.ExpiresInSeconds ?? 3600
        );
        await _tokenRepo.UpsertAsync(tokenDoc, ct);

        return credential;
    }

    private static IEnumerable<string> CollectEmails(Event ev)
    {
        foreach (var attendee in ev.Attendees ?? [])
        {
            if (!string.IsNullOrEmpty(attendee.Email))
                yield return attendee.Email;
        }
    }

    private static IEnumerable<string> ExtractEmails(string header)
    {
        var matches = System.Text.RegularExpressions.Regex.Matches(
            header,
            @"[\w\.-]+@[\w\.-]+\.\w+"
        );
        foreach (System.Text.RegularExpressions.Match match in matches)
            yield return match.Value;
    }
}
