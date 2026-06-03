using System.Globalization;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

public sealed class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IDealRepository _dealRepo;
    private readonly IUserRepository _userRepo;
    private readonly CurrentUser _currentUser;

    private static readonly HashSet<string> ManualTypes =
    [
        ActivityTypes.Call,
        ActivityTypes.Email,
        ActivityTypes.Meeting,
        ActivityTypes.Note
    ];

    public ActivityService(
        IActivityRepository activityRepo,
        ICustomerRepository customerRepo,
        IDealRepository dealRepo,
        IUserRepository userRepo,
        CurrentUser currentUser)
    {
        _activityRepo = activityRepo;
        _customerRepo = customerRepo;
        _dealRepo = dealRepo;
        _userRepo = userRepo;
        _currentUser = currentUser;
    }

    public async Task<ActivityListResponse> GetListAsync(ActivityListFilterRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.CustomerId) && string.IsNullOrWhiteSpace(request.DealId))
            throw new ValidationException("filter", "customerId hoặc dealId là bắt buộc.");

        var limit = request.Limit is < 1 or > 50 ? 20 : request.Limit;
        var filter = Builders<Activity>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            filter &= Builders<Activity>.Filter.Eq(x => x.customerId, request.CustomerId);
        if (!string.IsNullOrWhiteSpace(request.DealId))
            filter &= Builders<Activity>.Filter.Eq(x => x.dealId, request.DealId);

        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            await EnsureCustomerAccessAsync(request.CustomerId, ct);

        ParseCursor(request.Cursor, out var cursorOccurredAt, out var cursorId);

        var items = await _activityRepo.FindCursorAsync(filter, cursorOccurredAt, cursorId, limit + 1, ct);
        var hasMore = items.Count > limit;
        if (hasMore)
            items = items.Take(limit).ToList();

        var userIds = items.Select(x => x.createdBy).Where(x => !string.IsNullOrEmpty(x)).Distinct().ToList();
        var users = await LoadUsersAsync(userIds, ct);
        var responses = items.Select(a =>
            a.ToResponse(users.GetValueOrDefault(a.createdBy))).ToList();

        string? nextCursor = null;
        if (hasMore && items.Count > 0)
        {
            var last = items[^1];
            nextCursor = EncodeCursor(last.occurredAt, last.id);
        }

        return new ActivityListResponse
        {
            Items = responses,
            NextCursor = nextCursor,
            HasMore = hasMore
        };
    }

    public async Task<ServiceResult<ActivityResponse>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var activity = await _activityRepo.FindByIdAsync(id, ct);
        if (activity == null)
            return ServiceResult<ActivityResponse>.Fail("NOT_FOUND", "Không tìm thấy hoạt động.");

        await EnsureCustomerAccessAsync(activity.customerId, ct);

        var creator = await _userRepo.FindByIdAsync(activity.createdBy, ct);
        return ServiceResult<ActivityResponse>.Ok(activity.ToResponse(creator));
    }

    public async Task<ServiceResult<ActivityResponse>> CreateAsync(CreateActivityRequest request, CancellationToken ct = default)
    {
        ValidateManualType(request.Type);
        ValidateTypeFields(request.Type, request);

        var customer = await _customerRepo.FindByIdAsync(request.CustomerId, ct);
        if (customer == null)
            return ServiceResult<ActivityResponse>.Fail("INVALID_CUSTOMER", "Customer không tồn tại.");

        await EnsureCustomerAccessAsync(customer.id, ct);

        if (!string.IsNullOrWhiteSpace(request.DealId))
        {
            var deal = await _dealRepo.FindByIdAsync(request.DealId, ct);
            if (deal == null || deal.customerId != customer.id)
                return ServiceResult<ActivityResponse>.Fail("INVALID_DEAL", "Deal không hợp lệ.");
        }

        var now = DateTime.UtcNow;
        var activity = new Activity
        {
            organizationId = _currentUser.OrganizationId,
            customerId = customer.id,
            dealId = request.DealId,
            departmentId = customer.departmentId,
            type = request.Type.ToLowerInvariant(),
            source = ActivitySources.Manual,
            isAutoSync = false,
            createdBy = _currentUser.UserId,
            occurredAt = request.OccurredAt?.ToUniversalTime() ?? now,
            createdAt = now,
            updatedAt = now,
            outcome = request.Outcome,
            durationMinutes = request.DurationMinutes,
            note = request.Note,
            subject = request.Subject,
            summary = request.Summary,
            location = request.Location,
            attendees = request.Attendees ?? [],
            nextSteps = request.NextSteps,
            body = request.Body
        };

        await _activityRepo.InsertAsync(activity, ct);
        var creator = await _userRepo.FindByIdAsync(activity.createdBy, ct);
        return ServiceResult<ActivityResponse>.Ok(activity.ToResponse(creator));
    }

    public async Task<ServiceResult<ActivityResponse>> UpdateAsync(string id, UpdateActivityRequest request, CancellationToken ct = default)
    {
        var activity = await _activityRepo.FindByIdAsync(id, ct);
        if (activity == null)
            return ServiceResult<ActivityResponse>.Fail("NOT_FOUND", "Không tìm thấy hoạt động.");

        if (activity.source != ActivitySources.Manual)
            return ServiceResult<ActivityResponse>.Fail("NOT_EDITABLE", "Chỉ có thể sửa hoạt động thủ công.");

        await EnsureCustomerAccessAsync(activity.customerId, ct);

        if (_currentUser.Role > 2 && activity.createdBy != _currentUser.UserId)
            throw new ForbiddenException("Bạn chỉ có thể sửa hoạt động do mình tạo.");

        var update = Builders<Activity>.Update.Set(x => x.updatedAt, DateTime.UtcNow);

        if (request.OccurredAt.HasValue)
            update = update.Set(x => x.occurredAt, request.OccurredAt.Value.ToUniversalTime());

        switch (activity.type)
        {
            case ActivityTypes.Call:
                if (request.Outcome != null) update = update.Set(x => x.outcome, request.Outcome);
                if (request.DurationMinutes.HasValue) update = update.Set(x => x.durationMinutes, request.DurationMinutes);
                if (request.Note != null) update = update.Set(x => x.note, request.Note);
                break;
            case ActivityTypes.Email:
                if (request.Subject != null) update = update.Set(x => x.subject, request.Subject);
                if (request.Summary != null) update = update.Set(x => x.summary, request.Summary);
                break;
            case ActivityTypes.Meeting:
                if (request.Location != null) update = update.Set(x => x.location, request.Location);
                if (request.Attendees != null) update = update.Set(x => x.attendees, request.Attendees);
                if (request.Summary != null) update = update.Set(x => x.summary, request.Summary);
                if (request.NextSteps != null) update = update.Set(x => x.nextSteps, request.NextSteps);
                break;
            case ActivityTypes.Note:
                if (request.Body != null) update = update.Set(x => x.body, request.Body);
                break;
        }

        await _activityRepo.UpdateAsync(id, update, ct);
        return await GetByIdAsync(id, ct);
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var activity = await _activityRepo.FindByIdAsync(id, ct);
        if (activity == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy hoạt động.");

        if (activity.source != ActivitySources.Manual)
            return ServiceResult.Fail("NOT_DELETABLE", "Chỉ có thể xóa hoạt động thủ công.");

        await EnsureCustomerAccessAsync(activity.customerId, ct);

        if (_currentUser.Role > 2 && activity.createdBy != _currentUser.UserId)
            throw new ForbiddenException("Bạn chỉ có thể xóa hoạt động do mình tạo.");

        var success = await _activityRepo.SoftDeleteAsync(id, ct);
        return success ? ServiceResult.Ok() : ServiceResult.Fail("NOT_FOUND", "Không tìm thấy hoạt động.");
    }

    private async Task EnsureCustomerAccessAsync(string customerId, CancellationToken ct)
    {
        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            throw new ValidationException("customerId", "Customer không tồn tại.");

        if (_currentUser.Role == 3 && customer.departmentId != _currentUser.DepartmentId)
            throw new ForbiddenException("Bạn không có quyền truy cập khách hàng của phòng ban khác.");
    }

    private static void ValidateManualType(string type)
    {
        if (string.IsNullOrWhiteSpace(type) || !ManualTypes.Contains(type.ToLowerInvariant()))
            throw new ValidationException("type", "Type phải là call, email, meeting hoặc note.");
    }

    private static void ValidateTypeFields(string type, CreateActivityRequest request)
    {
        switch (type.ToLowerInvariant())
        {
            case ActivityTypes.Call:
                if (string.IsNullOrWhiteSpace(request.Outcome))
                    throw new ValidationException("outcome", "Outcome là bắt buộc cho cuộc gọi.");
                break;
            case ActivityTypes.Email:
                if (string.IsNullOrWhiteSpace(request.Subject))
                    throw new ValidationException("subject", "Subject là bắt buộc cho email.");
                break;
            case ActivityTypes.Meeting:
                if (string.IsNullOrWhiteSpace(request.Summary))
                    throw new ValidationException("summary", "Summary là bắt buộc cho meeting.");
                break;
            case ActivityTypes.Note:
                if (string.IsNullOrWhiteSpace(request.Body))
                    throw new ValidationException("body", "Nội dung note là bắt buộc.");
                break;
        }
    }

    private async Task<Dictionary<string, User>> LoadUsersAsync(List<string> userIds, CancellationToken ct)
    {
        var dict = new Dictionary<string, User>();
        foreach (var userId in userIds)
        {
            if (string.IsNullOrEmpty(userId)) continue;
            var user = await _userRepo.FindByIdAsync(userId, ct);
            if (user != null)
                dict[userId] = user;
        }
        return dict;
    }

    internal static string EncodeCursor(DateTime occurredAt, string id)
        => $"{occurredAt.ToUniversalTime():O}|{id}";

  internal static void ParseCursor(string? cursor, out DateTime? occurredAt, out string? id)
    {
        occurredAt = null;
        id = null;
        if (string.IsNullOrWhiteSpace(cursor)) return;

        var parts = cursor.Split('|', 2);
        if (parts.Length != 2) return;
        if (DateTime.TryParse(parts[0], null, DateTimeStyles.RoundtripKind, out var dt))
            occurredAt = dt.ToUniversalTime();
        id = parts[1];
    }
}
