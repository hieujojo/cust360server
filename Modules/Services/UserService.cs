using CRM.Api.Infrastructure.Email;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Constants;
using CRM.Api.Shared.Helpers;
using CRM.Api.Shared.Models;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

/// <summary>Xử lý nghiệp vụ quản lý user.</summary>
public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepo;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailService _emailService;
    private readonly IDepartmentRepository _deptRepo;
    private readonly ITeamRepository _teamRepo;
    private readonly CurrentUser _currentUser;

    public UserService(
        IUserRepository userRepo,
        IAuditLogService auditLogService,
        IEmailService emailService,
        IDepartmentRepository deptRepo,
        ITeamRepository teamRepo,
        CurrentUser currentUser,
        ILogger<UserService> logger
    )
    {
        _userRepo = userRepo;
        _auditLogService = auditLogService;
        _emailService = emailService;
        _deptRepo = deptRepo;
        _teamRepo = teamRepo;
        _currentUser = currentUser;
    }

    // ─── Create ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<UserResponse>> CreateUserAsync(
        CreateUserRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        var validation = ValidateCreateRequest(request);
        if (!validation.IsSuccess)
            return validation.ToTyped<UserResponse>();

        // Validate department và team
        var deptTeamValidation = await ValidateDepartmentAndTeamAsync(
            request.DepartmentId,
            request.TeamId,
            ct
        );
        if (!deptTeamValidation.IsSuccess)
            return deptTeamValidation.ToTyped<UserResponse>();

        if (await _userRepo.EmailExistsAsync(request.Email, ct))
            return ServiceResult<UserResponse>.Fail(
                "EMAIL_EXISTS",
                $"Email '{request.Email}' đã được sử dụng trong tổ chức."
            );

        for (var attempt = 0; attempt < 5; attempt++)
        {
            var employeeCode = await GenerateEmployeeCodeAsync(ct);
            var user = BuildUserEntity(request, employeeCode);

            try
            {
                await _userRepo.InsertAsync(user, ct);
                SendWelcomeEmailFireAndForget(user.email, user.displayName, request.Password);

                await _auditLogService.LogAsync(
                    action: AuditActions.UserCreated,
                    targetUserId: user.id,
                    targetUserEmail: user.email,
                    metadata: new Dictionary<string, string>
                    {
                        ["role"] = Roles.GetName(user.role),
                    },
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    ct: ct
                );

                return ServiceResult<UserResponse>.Ok(user.ToResponse());
            }
            catch (MongoDB.Driver.MongoWriteException ex) when (ex.Message.Contains("E11000"))
            {
                if (ex.Message.Contains("org_email_unique"))
                    return ServiceResult<UserResponse>.Fail(
                        "EMAIL_EXISTS",
                        $"Email '{request.Email}' đã được sử dụng trong tổ chức."
                    );

                if (ex.Message.Contains("org_employee_code_unique"))
                {
                    if (attempt == 4)
                        return ServiceResult<UserResponse>.Fail(
                            "EMPLOYEE_CODE_CONFLICT",
                            "Mã nhân viên đã tồn tại. Vui lòng thử lại."
                        );

                    continue;
                }

                throw;
            }
        }

        return ServiceResult<UserResponse>.Fail(
            "EMPLOYEE_CODE_CONFLICT",
            "Không thể tạo mã nhân viên. Vui lòng thử lại."
        );
    }

    // ─── Update ──────────────────────────────────────────────────────────────

    public async Task<ServiceResult<UserResponse>> UpdateUserAsync(
        string userId,
        UpdateUserRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.FindByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult<UserResponse>.Fail("NOT_FOUND", "Không tìm thấy user.");

        if (user.role == Roles.Owner && request.Role.HasValue && request.Role.Value != Roles.Owner)
            return ServiceResult<UserResponse>.Fail(
                "CANNOT_CHANGE_OWNER_ROLE",
                "Không thể thay đổi role của Owner."
            );

        // Validate department và team nếu có thay đổi
        var deptId = request.DepartmentId ?? user.departmentId;
        var teamId = request.TeamId ?? user.teamId;
        var deptTeamValidation = await ValidateDepartmentAndTeamAsync(deptId, teamId, ct);
        if (!deptTeamValidation.IsSuccess)
            return deptTeamValidation.ToTyped<UserResponse>();

        await _userRepo.UpdateAsync(userId, BuildUpdateDefinition(request), ct);

        await _auditLogService.LogAsync(
            action: AuditActions.UserUpdated,
            targetUserId: user.id,
            targetUserEmail: user.email,
            ipAddress: ipAddress,
            userAgent: userAgent,
            ct: ct
        );

        var updated = await _userRepo.FindByIdAsync(userId, ct);
        return ServiceResult<UserResponse>.Ok(await EnrichUserResponseAsync(updated!, ct));
    }

    // ─── Toggle Status ───────────────────────────────────────────────────────

    public async Task<ServiceResult> ToggleUserStatusAsync(
        string userId,
        ToggleUserStatusRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.FindByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy user.");

        if (user.role == Roles.Owner)
            return ServiceResult.Fail(
                "CANNOT_DEACTIVATE_OWNER",
                "Không thể vô hiệu hóa tài khoản Owner."
            );

        if (user.isActive == request.IsActive)
            return ServiceResult.Fail(
                "NO_CHANGE",
                $"Tài khoản đã ở trạng thái {(request.IsActive ? "active" : "inactive")}."
            );

        await _userRepo.SetActiveStatusAsync(userId, request.IsActive, ct);

        if (!request.IsActive)
            SendDeactivatedEmailFireAndForget(user.email, user.displayName);

        var action = request.IsActive ? AuditActions.UserActivated : AuditActions.UserDeactivated;
        var metadata = string.IsNullOrWhiteSpace(request.Reason)
            ? null
            : new Dictionary<string, string> { ["reason"] = request.Reason };

        await _auditLogService.LogAsync(
            action: action,
            targetUserId: user.id,
            targetUserEmail: user.email,
            metadata: metadata,
            ipAddress: ipAddress,
            userAgent: userAgent,
            ct: ct
        );

        return ServiceResult.Ok();
    }

    // ─── Read ────────────────────────────────────────────────────────────────

    public async Task<ServiceResult<UserResponse>> GetByIdAsync(
        string userId,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.FindByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult<UserResponse>.Fail("NOT_FOUND", "Không tìm thấy user.");

        return ServiceResult<UserResponse>.Ok(await EnrichUserResponseAsync(user, ct));
    }

    public async Task<PagedResult<UserResponse>> GetPagedAsync(
        GetUsersRequest request,
        CancellationToken ct = default
    )
    {
        var deptFilter = ResolveDepartmentFilter(request.DepartmentId);

        var (items, total) = await _userRepo.FindPagedAsync(
            request.Role,
            deptFilter,
            request.IsActive,
            request.Search,
            request.Page,
            request.PageSize,
            ct
        );

        // Enrich từng user với dept/team name
        var enrichedItems = new List<UserResponse>();
        foreach (var user in items)
        {
            enrichedItems.Add(await EnrichUserResponseAsync(user, ct));
        }

        return PagedResult<UserResponse>.Create(
            enrichedItems,
            total,
            request.Page,
            request.PageSize
        );
    }

    public async Task<ServiceResult<List<UserResponse>>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await _userRepo.FindAllAsync(ct);
        var enrichedItems = new List<UserResponse>();
        foreach (var user in items)
        {
            enrichedItems.Add(await EnrichUserResponseAsync(user, ct));
        }
        return ServiceResult<List<UserResponse>>.Ok(enrichedItems);
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    /// <summary>Lookup dept/team name và check isTeamLead.</summary>
    private async Task<UserResponse> EnrichUserResponseAsync(User user, CancellationToken ct)
    {
        string? deptName = null;
        string? teamName = null;
        var isTeamLead = false;

        if (!string.IsNullOrEmpty(user.departmentId))
        {
            var dept = await _deptRepo.FindByIdAsync(user.departmentId, ct);
            deptName = dept?.name;
        }

        if (!string.IsNullOrEmpty(user.teamId))
        {
            var team = await _teamRepo.FindByIdAsync(user.teamId, ct);
            teamName = team?.name;
            isTeamLead = team?.leadId == user.id;
        }

        return user.ToResponse(deptName, teamName, isTeamLead);
    }

    private static ServiceResult ValidateCreateRequest(CreateUserRequest request)
    {
        if (!Roles.IsValid(request.Role))
            return ServiceResult.Fail("INVALID_ROLE", "Role không hợp lệ.");

        if (request.Role == Roles.Owner)
            return ServiceResult.Fail(
                "CANNOT_CREATE_OWNER",
                "Không thể tạo tài khoản Owner qua API."
            );

        if (request.Role == Roles.User && string.IsNullOrWhiteSpace(request.DepartmentId))
            return ServiceResult.Fail(
                "DEPT_REQUIRED",
                "User (Role 3) phải được gán vào một phòng ban."
            );

        return ServiceResult.Ok();
    }

    private async Task<ServiceResult> ValidateDepartmentAndTeamAsync(
        string? departmentId,
        string? teamId,
        CancellationToken ct
    )
    {
        // Validate departmentId nếu có
        if (!string.IsNullOrWhiteSpace(departmentId))
        {
            if (!MongoDB.Bson.ObjectId.TryParse(departmentId, out _))
                return ServiceResult.Fail("DEPT_INVALID", "ID phòng ban không hợp lệ.");

            var dept = await _deptRepo.FindByIdAsync(departmentId, ct);
            if (dept is null)
                return ServiceResult.Fail("DEPT_NOT_FOUND", "Không tìm thấy phòng ban.");
        }

        // Validate teamId nếu có
        if (!string.IsNullOrWhiteSpace(teamId))
        {
            if (string.IsNullOrWhiteSpace(departmentId))
                return ServiceResult.Fail(
                    "DEPT_REQUIRED_FOR_TEAM",
                    "Phải chỉ định phòng ban khi gán team."
                );

            if (!MongoDB.Bson.ObjectId.TryParse(teamId, out _))
                return ServiceResult.Fail("TEAM_INVALID", "ID team không hợp lệ.");

            var team = await _teamRepo.FindByIdAsync(teamId, ct);
            if (team is null)
                return ServiceResult.Fail("TEAM_NOT_FOUND", "Không tìm thấy team.");

            if (team.departmentId != departmentId)
                return ServiceResult.Fail(
                    "TEAM_DEPT_MISMATCH",
                    "Team không thuộc phòng ban được chỉ định."
                );
        }

        return ServiceResult.Ok();
    }

    private User BuildUserEntity(CreateUserRequest request, string employeeCode) =>
        new()
        {
            id = MongoDB.Bson.ObjectId.GenerateNewId().ToString(),
            organizationId = _currentUser.OrganizationId,
            email = request.Email.ToLowerInvariant(),
            employeeCode = employeeCode,
            password = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            role = request.Role,
            departmentId = request.DepartmentId,
            teamId = request.TeamId,
            displayName = request.DisplayName.Trim(),
            phone = request.Phone?.Trim(),
            isActive = true,
            createdBy = _currentUser.UserId,
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow,
        };

    /// <summary>Format: NV-YYYY-NNNN.</summary>
    private async Task<string> GenerateEmployeeCodeAsync(CancellationToken ct)
    {
        var sequence = await _userRepo.CountAllAsync(ct) + 1;
        var code = CodeGenerator.Employee(sequence);

        while (await _userRepo.CodeExistsAsync(code, ct))
        {
            sequence++;
            code = CodeGenerator.Employee(sequence);
        }

        return code;
    }

    private static UpdateDefinition<User> BuildUpdateDefinition(UpdateUserRequest request)
    {
        var update = Builders<User>.Update.Set(x => x.updatedAt, DateTime.UtcNow);

        if (request.DisplayName != null)
            update = update.Set(x => x.displayName, request.DisplayName.Trim());
        if (request.Role.HasValue)
            update = update.Set(x => x.role, request.Role.Value);
        if (request.Phone != null)
            update = update.Set(x => x.phone, request.Phone.Trim());
        if (request.DepartmentId != null)
            update = update.Set(x => x.departmentId, request.DepartmentId);
        if (request.TeamId != null)
            update = update.Set(x => x.teamId, request.TeamId);
        if (request.AvatarUrl != null)
            update = update.Set(x => x.avatarUrl, request.AvatarUrl.Trim());

        return update;
    }

    /// <summary>Role 3 chỉ thấy dept mình. Admin/Owner thấy tất cả.</summary>
    private string? ResolveDepartmentFilter(string? requestedDeptId)
    {
        if (!_currentUser.IsAdminOrAbove && !string.IsNullOrEmpty(_currentUser.DepartmentId))
            return _currentUser.DepartmentId;

        return string.IsNullOrWhiteSpace(requestedDeptId) ? null : requestedDeptId;
    }

    private void SendWelcomeEmailFireAndForget(string email, string displayName, string password) =>
        _ = Task.Run(async () =>
            await _emailService.SendAccountCreatedAsync(
                email,
                displayName,
                password,
                CancellationToken.None
            )
        );

    private void SendDeactivatedEmailFireAndForget(string email, string displayName) =>
        _ = Task.Run(async () =>
            await _emailService.SendAccountDeactivatedAsync(
                email,
                displayName,
                CancellationToken.None
            )
        );

    // ─── Password Management ─────────────────────────────────────────────────

    public async Task<ServiceResult> ChangePasswordAsync(
        string userId,
        ChangePasswordRequest request,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.FindByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy user.");

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.password))
            return ServiceResult.Fail("WRONG_PASSWORD", "Mật khẩu hiện tại không đúng.");

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword, workFactor: 12);
        var update = Builders<User>
            .Update.Set(x => x.password, newPasswordHash)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await _userRepo.UpdateAsync(userId, update, ct);

        await _auditLogService.LogAsync(
            action: AuditActions.UserPasswordChanged,
            targetUserId: user.id,
            targetUserEmail: user.email,
            ipAddress: ipAddress,
            userAgent: userAgent,
            ct: ct
        );

        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> ResetPasswordAsync(
        string userId,
        string newPassword,
        string? ipAddress = null,
        string? userAgent = null,
        CancellationToken ct = default
    )
    {
        var user = await _userRepo.FindByIdAsync(userId, ct);
        if (user is null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy user.");

        var newPasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword, workFactor: 12);
        var update = Builders<User>
            .Update.Set(x => x.password, newPasswordHash)
            .Set(x => x.updatedAt, DateTime.UtcNow);

        await _userRepo.UpdateAsync(userId, update, ct);

        SendPasswordResetEmailFireAndForget(user.email, user.displayName, newPassword);

        await _auditLogService.LogAsync(
            action: AuditActions.UserPasswordReset,
            targetUserId: user.id,
            targetUserEmail: user.email,
            ipAddress: ipAddress,
            userAgent: userAgent,
            ct: ct
        );

        return ServiceResult.Ok();
    }

    private void SendPasswordResetEmailFireAndForget(
        string email,
        string displayName,
        string newPassword
    ) =>
        _ = Task.Run(async () =>
            await _emailService.SendPasswordResetAsync(
                email,
                displayName,
                newPassword,
                CancellationToken.None
            )
        );
}
