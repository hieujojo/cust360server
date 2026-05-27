using MongoDB.Driver;
using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

/// <summary>Xử lý nghiệp vụ quản lý team.</summary>
public sealed class TeamService : ITeamService
{
    private readonly ITeamRepository       _teamRepo;
    private readonly IDepartmentRepository _deptRepo;

    // Dùng BsonDocument collection để lookup tên lead từ users
    // Tránh circular dependency với Identity module
    private readonly Infrastructure.MongoDB.MongoDbContext _dbContext;
    private readonly CurrentUser _currentUser;

    public TeamService(
        ITeamRepository teamRepo,
        IDepartmentRepository deptRepo,
        Infrastructure.MongoDB.MongoDbContext dbContext,
        CurrentUser currentUser)
    {
        _teamRepo    = teamRepo;
        _deptRepo    = deptRepo;
        _dbContext   = dbContext;
        _currentUser = currentUser;
    }

    public async Task<ServiceResult<TeamResponse>> CreateAsync(
        string departmentId, CreateTeamRequest request, CancellationToken ct = default)
    {
        var dept = await _deptRepo.FindByIdAsync(departmentId, ct);
        if (dept is null)
            return ServiceResult<TeamResponse>.Fail("DEPT_NOT_FOUND", "Không tìm thấy phòng ban.");

        if (await _teamRepo.NameExistsInDepartmentAsync(request.Name, departmentId, ct: ct))
            return ServiceResult<TeamResponse>.Fail("NAME_EXISTS",
                $"Team '{request.Name}' đã tồn tại trong phòng ban này.");

        // Validate leadId nếu có
        if (!string.IsNullOrEmpty(request.LeadId))
        {
            var leadExists = await UserExistsAsync(request.LeadId, ct);
            if (!leadExists)
                return ServiceResult<TeamResponse>.Fail("LEAD_NOT_FOUND", "Không tìm thấy user được chỉ định làm lead.");
        }

        var team = new Team
        {
            organizationId = _currentUser.OrganizationId,
            departmentId   = departmentId,
            name           = request.Name.Trim(),
            description    = request.Description?.Trim(),
            leadId         = string.IsNullOrEmpty(request.LeadId) ? null : request.LeadId,
            createdBy      = _currentUser.UserId,
            createdAt      = DateTime.UtcNow,
            updatedAt      = DateTime.UtcNow,
        };

        await _teamRepo.InsertAsync(team, ct);

        var leadName = await GetUserDisplayNameAsync(team.leadId, ct);
        return ServiceResult<TeamResponse>.Ok(team.ToResponse(dept.name, leadName));
    }

    public async Task<ServiceResult<TeamResponse>> UpdateAsync(
        string departmentId, string id, UpdateTeamRequest request, CancellationToken ct = default)
    {
        var dept = await _deptRepo.FindByIdAsync(departmentId, ct);
        if (dept is null)
            return ServiceResult<TeamResponse>.Fail("DEPT_NOT_FOUND", "Không tìm thấy phòng ban.");

        var team = await _teamRepo.FindByIdAsync(id, ct);
        if (team is null || team.departmentId != departmentId)
            return ServiceResult<TeamResponse>.Fail("NOT_FOUND", "Không tìm thấy team.");

        if (request.Name != null && await _teamRepo.NameExistsInDepartmentAsync(request.Name, departmentId, excludeId: id, ct: ct))
            return ServiceResult<TeamResponse>.Fail("NAME_EXISTS",
                $"Team '{request.Name}' đã tồn tại trong phòng ban này.");

        // Validate leadId nếu có
        if (!string.IsNullOrEmpty(request.LeadId))
        {
            var leadExists = await UserExistsAsync(request.LeadId, ct);
            if (!leadExists)
                return ServiceResult<TeamResponse>.Fail("LEAD_NOT_FOUND", "Không tìm thấy user được chỉ định làm lead.");
        }

        var update = Builders<Team>.Update.Set(x => x.updatedAt, DateTime.UtcNow);

        if (request.Name        != null) update = update.Set(x => x.name,        request.Name.Trim());
        if (request.Description != null) update = update.Set(x => x.description, request.Description.Trim());

        if (request.ClearLead)
            update = update.Set(x => x.leadId, (string?)null);
        else if (!string.IsNullOrEmpty(request.LeadId))
            update = update.Set(x => x.leadId, request.LeadId);

        await _teamRepo.UpdateAsync(id, update, ct);

        var updated     = await _teamRepo.FindByIdAsync(id, ct);
        var memberCount = await _teamRepo.CountMembersAsync(id, ct);
        var leadName    = await GetUserDisplayNameAsync(updated!.leadId, ct);

        return ServiceResult<TeamResponse>.Ok(updated.ToResponse(dept.name, leadName, memberCount));
    }

    public async Task<ServiceResult> DeleteAsync(string departmentId, string id, CancellationToken ct = default)
    {
        var team = await _teamRepo.FindByIdAsync(id, ct);
        if (team is null || team.departmentId != departmentId)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy team.");

        await _teamRepo.SoftDeleteAsync(id, ct);
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<TeamResponse>> GetByIdAsync(
        string departmentId, string id, CancellationToken ct = default)
    {
        var dept = await _deptRepo.FindByIdAsync(departmentId, ct);
        if (dept is null)
            return ServiceResult<TeamResponse>.Fail("DEPT_NOT_FOUND", "Không tìm thấy phòng ban.");

        var team = await _teamRepo.FindByIdAsync(id, ct);
        if (team is null || team.departmentId != departmentId)
            return ServiceResult<TeamResponse>.Fail("NOT_FOUND", "Không tìm thấy team.");

        var memberCount = await _teamRepo.CountMembersAsync(id, ct);
        var leadName    = await GetUserDisplayNameAsync(team.leadId, ct);

        return ServiceResult<TeamResponse>.Ok(team.ToResponse(dept.name, leadName, memberCount));
    }

    public async Task<List<TeamResponse>> GetByDepartmentAsync(string departmentId, CancellationToken ct = default)
    {
        var dept = await _deptRepo.FindByIdAsync(departmentId, ct);
        Console.WriteLine($"[DEBUG] departmentId={departmentId}, dept={dept?.name ?? "NULL"}");
        if (dept is null) return [];

        var teams = await _teamRepo.FindByDepartmentAsync(departmentId, ct);
    Console.WriteLine($"[DEBUG] teams count={teams.Count}");

        // Batch load lead names
        var leadIds = teams
            .Where(t => !string.IsNullOrEmpty(t.leadId))
            .Select(t => t.leadId!)
            .Distinct()
            .ToList();

        var leadNames = await GetUserDisplayNamesAsync(leadIds, ct);

        var result = new List<TeamResponse>();
        foreach (var team in teams)
        {
            var memberCount = await _teamRepo.CountMembersAsync(team.id, ct);
            var leadName    = team.leadId != null ? leadNames.GetValueOrDefault(team.leadId) : null;
            result.Add(team.ToResponse(dept.name, leadName, memberCount));
        }

        return result;
    }

    // ─── Private helpers ─────────────────────────────────────────────────────

    private async Task<bool> UserExistsAsync(string userId, CancellationToken ct)
    {
        var collection = _dbContext.GetCollection<MongoDB.Bson.BsonDocument>("users");
        var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.And(
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(userId)),
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("organizationId", _currentUser.OrganizationId),
            MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("isDeleted", false));

        return await collection.Find(filter).AnyAsync(ct);
    }

    private async Task<string?> GetUserDisplayNameAsync(string? userId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(userId)) return null;

        var collection = _dbContext.GetCollection<MongoDB.Bson.BsonDocument>("users");
        var filter = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.Eq("_id", MongoDB.Bson.ObjectId.Parse(userId));
        var projection = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Projection.Include("displayName");

        var doc = await collection.Find(filter).Project(projection).FirstOrDefaultAsync(ct);
        return doc?.GetValue("displayName", null)?.AsString;
    }

    private async Task<Dictionary<string, string>> GetUserDisplayNamesAsync(List<string> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0) return [];

        var collection = _dbContext.GetCollection<MongoDB.Bson.BsonDocument>("users");
        var objectIds  = userIds.Select(MongoDB.Bson.ObjectId.Parse).ToList();

        var filter     = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Filter.In("_id", objectIds);
        var projection = MongoDB.Driver.Builders<MongoDB.Bson.BsonDocument>.Projection
            .Include("_id").Include("displayName");

        var docs = await collection.Find(filter).Project(projection).ToListAsync(ct);

        return docs.ToDictionary(
            d => d["_id"].AsObjectId.ToString(),
            d => d.GetValue("displayName", "").AsString);
    }
}
