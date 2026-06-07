using CRM.Api.Infrastructure.MongoDB;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;

namespace CRM.Api.Modules.Repositories;

public sealed class OrganizationRepository : BaseRepository<Organization>, IOrganizationRepository
{
    private const string CollectionName = "organizations";
    private readonly CurrentUser _currentUser;
    public OrganizationRepository(
        MongoDbContext context,
        CurrentUser currentUser
    )
        : base(context, CollectionName, currentUser)
    {
        _currentUser = currentUser;
    }

    public async Task<Organization?> GetCurrentAsync(CancellationToken ct = default)
    {
        var items = await FindManyAsync(Builders<Organization>.Filter.Empty, limit: 1, ct: ct);
        return items.FirstOrDefault();
    }

    public async Task<Organization> GetOrCreateCurrentAsync(CancellationToken ct = default)
    {
        var existing = await GetCurrentAsync(ct);
        if (existing != null)
        {
            return existing;
        }

        var org = new Organization
        {
            organizationId = _currentUser.OrganizationId,
            name = "My Organization",
            timezone = "Asia/Ho_Chi_Minh",
            currency = "VND",
            language = "vi",
            pipelineStages =
            [
                new PipelineStage
                {
                    name = "Lead",
                    order = 1,
                    color = "#3b82f6",
                    defaultProbability = 10,
                    stuckThreshold = 7,
                },
                new PipelineStage
                {
                    name = "Qualified",
                    order = 2,
                    color = "#06b6d4",
                    defaultProbability = 25,
                    stuckThreshold = 7,
                },
                new PipelineStage
                {
                    name = "Proposal",
                    order = 3,
                    color = "#a855f7",
                    defaultProbability = 50,
                    stuckThreshold = 7,
                },
                new PipelineStage
                {
                    name = "Negotiation",
                    order = 4,
                    color = "#f97316",
                    defaultProbability = 75,
                    stuckThreshold = 7,
                },
                new PipelineStage
                {
                    name = "Won",
                    order = 5,
                    color = "#22c55e",
                    defaultProbability = 100,
                    stuckThreshold = 30,
                },
            ],
        };

        await InsertAsync(org, ct);
        return org;
    }

    public async Task UpdatePipelineStagesAsync(
        List<PipelineStage> stages,
        CancellationToken ct = default
    )
    {
        var org = await GetOrCreateCurrentAsync(ct);
        var update = Builders<Organization>.Update.Set(x => x.pipelineStages, stages);
        await UpdateAsync(org.id, update, ct);
    }

    public async Task UpdateProfileAsync(
        string name,
        string timezone,
        string currency,
        string language,
        CancellationToken ct = default
    )
    {
        var org = await GetOrCreateCurrentAsync(ct);
        var update = Builders<Organization>
            .Update.Set(x => x.name, name.Trim())
            .Set(x => x.timezone, timezone.Trim())
            .Set(x => x.currency, currency.Trim())
            .Set(x => x.language, language.Trim());

        await UpdateAsync(org.id, update, ct);
    }

    public async Task UpdateLogoUrlAsync(string? logoUrl, CancellationToken ct = default)
    {
        var org = await GetOrCreateCurrentAsync(ct);
        var update = Builders<Organization>.Update.Set(x => x.logoUrl, logoUrl);
        await UpdateAsync(org.id, update, ct);
    }

    public async Task UpdateDepartmentsAsync(
        List<OrgDepartment> departments,
        CancellationToken ct = default
    )
    {
        var org = await GetOrCreateCurrentAsync(ct);
        var update = Builders<Organization>.Update.Set(x => x.departments, departments);
        await UpdateAsync(org.id, update, ct);
    }

    public async Task<OrgDepartment?> GetDepartmentByIdAsync(string id, CancellationToken ct = default)
    {
        var org = await GetOrCreateCurrentAsync(ct);
        return org.departments.FirstOrDefault(d => d.id == id);
    }
}
