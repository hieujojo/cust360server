using System.Security.Cryptography;
using System.Text;
using CRM.Api.Infrastructure.Google;
using CRM.Api.Infrastructure.Settings;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Microsoft.Extensions.Options;

namespace CRM.Api.Modules.Services;

public sealed class GoogleAuthService : IGoogleAuthService
{
    private static readonly string[] Scopes =
    [
        "https://www.googleapis.com/auth/gmail.readonly",
        "https://www.googleapis.com/auth/calendar.readonly",
        "https://www.googleapis.com/auth/userinfo.email",
    ];

    private readonly IGoogleSyncService _syncService;
    private readonly GoogleSettings _settings;
    private readonly IUserGoogleTokenRepository _tokenRepo;
    private readonly IGoogleTokenProtector _protector;
    private readonly CurrentUser _currentUser;

    public GoogleAuthService(
        IOptions<GoogleSettings> settings,
        IUserGoogleTokenRepository tokenRepo,
        IGoogleTokenProtector protector,
        CurrentUser currentUser,
        IGoogleSyncService syncService
    )
    {
        _settings = settings.Value;
        _tokenRepo = tokenRepo;
        _protector = protector;
        _currentUser = currentUser;
        _syncService = syncService;
    }

    public string GetAuthorizationUrl()
    {
        EnsureConfigured();
        var state = CreateSignedState(_currentUser.UserId, _currentUser.OrganizationId);

        var flow = CreateFlow();
        var request = flow.CreateAuthorizationCodeRequest(_settings.RedirectUri);
        request.State = state;

        var uri = request.Build().ToString();

        // SDK này không expose AccessType/ApprovalPrompt — append thủ công
        // nhưng chỉ thêm nếu chưa có để tránh duplicate
        if (!uri.Contains("access_type="))
            uri += "&access_type=offline";
        if (!uri.Contains("prompt="))
            uri += "&prompt=consent";

        return uri;
    }

    public async Task HandleCallbackAsync(string code, string state, CancellationToken ct = default)
    {
        EnsureConfigured();

        var (userId, orgId) = ParseSignedState(state);

        var flow = CreateFlow();
        var tokenResponse = await flow.ExchangeCodeForTokenAsync(
            userId,
            code,
            _settings.RedirectUri,
            ct
        );

        var credential = new UserCredential(flow, userId, tokenResponse);
        var email = await GetGoogleEmailAsync(credential, ct);

        var doc = new UserGoogleToken
        {
            organizationId = orgId,
            userId = userId,
            googleEmail = email,
            encryptedRefreshToken = _protector.Protect(tokenResponse.RefreshToken ?? string.Empty),
            accessToken = tokenResponse.AccessToken,
            accessTokenExpiresAt = tokenResponse.IssuedUtc.AddSeconds(
                tokenResponse.ExpiresInSeconds ?? 3600
            ),
            calendarSyncEnabled = true,
            gmailSyncEnabled = true,
        };

        await _tokenRepo.UpsertAsync(doc, ct);
        await _syncService.RegisterCalendarWatchAsync(userId, orgId, credential, ct);
    }

    public async Task<GoogleConnectionStatusResponse> GetStatusAsync(CancellationToken ct = default)
    {
        var token = await _tokenRepo.FindByUserIdAsync(_currentUser.UserId, ct);
        if (token == null)
            return new GoogleConnectionStatusResponse { Connected = false };

        return new GoogleConnectionStatusResponse
        {
            Connected = true,
            Email = token.googleEmail,
            CalendarSyncEnabled = token.calendarSyncEnabled,
            GmailSyncEnabled = token.gmailSyncEnabled,
        };
    }

    public async Task DisconnectAsync(CancellationToken ct = default) =>
        await _tokenRepo.DeleteByUserIdAsync(_currentUser.UserId, ct);

    private GoogleAuthorizationCodeFlow CreateFlow() =>
        new(
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

    private void EnsureConfigured()
    {
        if (
            string.IsNullOrWhiteSpace(_settings.ClientId)
            || string.IsNullOrWhiteSpace(_settings.ClientSecret)
        )
            throw new ValidationException("google", "Google OAuth chưa được cấu hình trên server.");
    }

    private string CreateSignedState(string userId, string orgId)
    {
        var payload = $"{userId}|{orgId}";
        var sig = Sign(payload);
        var raw = $"{payload}|{sig}";
        return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw));
    }

    private (string userId, string orgId) ParseSignedState(string state)
    {
        var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(state));
        var parts = decoded.Split('|', 3);
        if (parts.Length != 3)
            throw new ValidationException("state", "State OAuth không hợp lệ.");

        var payload = $"{parts[0]}|{parts[1]}";
        if (!string.Equals(Sign(payload), parts[2], StringComparison.Ordinal))
            throw new ValidationException("state", "State OAuth không hợp lệ.");

        return (parts[0], parts[1]);
    }

    private string Sign(string payload)
    {
        var key = Encoding.UTF8.GetBytes(
            string.IsNullOrWhiteSpace(_settings.TokenEncryptionKey)
                ? "crm360-oauth-state-key"
                : _settings.TokenEncryptionKey
        );
        using var hmac = new HMACSHA256(key);
        return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload)));
    }

    private static async Task<string> GetGoogleEmailAsync(
        UserCredential credential,
        CancellationToken ct
    )
    {
        var accessToken = await credential.GetAccessTokenForRequestAsync(cancellationToken: ct);
        using var http = new HttpClient();
        http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var json = await http.GetStringAsync("https://www.googleapis.com/oauth2/v2/userinfo", ct);
        using var doc = System.Text.Json.JsonDocument.Parse(json);
        return doc.RootElement.GetProperty("email").GetString() ?? string.Empty;
    }
}
