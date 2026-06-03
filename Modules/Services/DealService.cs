using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace CRM.Api.Modules.Services;

public sealed class DealService : IDealService
{
    private readonly IDealRepository _dealRepo;
    private readonly ICustomerRepository _customerRepo;
    private readonly IUserRepository _userRepo;
    private readonly IPipelineStageService _pipelineStageService;
    private readonly IActivityAutoLogService _activityAutoLog;
    private readonly CurrentUser _currentUser;

    public DealService(
        IDealRepository dealRepo,
        ICustomerRepository customerRepo,
        IUserRepository userRepo,
        IPipelineStageService pipelineStageService,
        IActivityAutoLogService activityAutoLog,
        CurrentUser currentUser)
    {
        _dealRepo = dealRepo;
        _customerRepo = customerRepo;
        _userRepo = userRepo;
        _pipelineStageService = pipelineStageService;
        _activityAutoLog = activityAutoLog;
        _currentUser = currentUser;
    }

    public async Task<List<DealResponse>> GetListAsync(DealListFilterRequest request, CancellationToken ct = default)
    {
        var filter = Builders<Deal>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(request.Stage))
            filter &= Builders<Deal>.Filter.Eq(x => x.stage, request.Stage);
        if (!string.IsNullOrWhiteSpace(request.Owner))
            filter &= Builders<Deal>.Filter.Eq(x => x.ownerId, request.Owner);
        if (!string.IsNullOrWhiteSpace(request.CustomerId))
            filter &= Builders<Deal>.Filter.Eq(x => x.customerId, request.CustomerId);
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var regex = new BsonRegularExpression(request.Search, "i");
            filter &= Builders<Deal>.Filter.Or(
                Builders<Deal>.Filter.Regex(x => x.title, regex),
                Builders<Deal>.Filter.Regex(x => x.notes, regex));
        }

        var sort = BuildSort(request.Sort);
        var deals = await _dealRepo.FindAsync(filter, sort, ct);

        var ownerIds = deals.Select(x => x.ownerId).Distinct().ToList();
        var customerIds = deals.Select(x => x.customerId).Distinct().ToList();

        var owners = new Dictionary<string, User>();
        foreach (var ownerId in ownerIds)
        {
            var owner = await _userRepo.FindByIdAsync(ownerId, ct);
            if (owner != null) owners[owner.id] = owner;
        }

        var customers = new Dictionary<string, Customer>();
        foreach (var customerId in customerIds)
        {
            var customer = await _customerRepo.FindByIdAsync(customerId, ct);
            if (customer != null) customers[customer.id] = customer;
        }

        return deals.Select(d => d.ToResponse(
            owners.GetValueOrDefault(d.ownerId),
            customers.GetValueOrDefault(d.customerId))).ToList();
    }

    public async Task<DealStatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var totalCount = await _dealRepo.CountAsync(Builders<Deal>.Filter.Empty, ct);
        var wonCount = await _dealRepo.CountByStageAsync("Won", ct);

        return new DealStatsResponse
        {
            TotalCount = totalCount,
            WonCount = wonCount
        };
    }

    public async Task<ServiceResult<DealResponse>> CreateAsync(CreateDealRequest request, CancellationToken ct = default)
    {
        var validation = await ValidateDealDataAsync(request.Customer, request.Owner, request.Stage, request.Probability, ct);
        if (!validation.IsSuccess)
            return validation;

        var ownerId = string.IsNullOrWhiteSpace(request.Owner) ? _currentUser.UserId : request.Owner!;

        var deal = new Deal
        {
            title = request.Title.Trim(),
            customerId = request.Customer,
            value = request.Value,
            expectedRevenue = request.ExpectedRevenue ?? 0,
            currency = request.Currency.Trim().ToUpperInvariant(),
            expectedCloseDate = request.ExpectedCloseDate,
            ownerId = ownerId,
            stage = request.Stage.Trim(),
            probability = request.Probability,
            notes = request.Notes?.Trim(),
            contacts = request.Contacts ?? [],
            quotations = request.Quotations ?? [],
            stageHistory =
            [
                new DealStageHistoryItem
                {
                    stage = request.Stage.Trim(),
                    changedAt = DateTime.UtcNow,
                    changedBy = _currentUser.UserId
                }
            ],
            createdAt = DateTime.UtcNow,
            updatedAt = DateTime.UtcNow
        };

        await _dealRepo.InsertAsync(deal, ct);
        return await GetByIdAsync(deal.id, ct);
    }

    public async Task<ServiceResult<DealResponse>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var deal = await _dealRepo.FindByIdAsync(id, ct);
        if (deal == null)
            return ServiceResult<DealResponse>.Fail("NOT_FOUND", "Không tìm thấy deal.");

        var owner = await _userRepo.FindByIdAsync(deal.ownerId, ct);
        var customer = await _customerRepo.FindByIdAsync(deal.customerId, ct);

        return ServiceResult<DealResponse>.Ok(deal.ToResponse(owner, customer));
    }

    public async Task<ServiceResult<DealResponse>> UpdateAsync(string id, UpdateDealRequest request, CancellationToken ct = default)
    {
        var existing = await _dealRepo.FindByIdAsync(id, ct);
        if (existing == null)
            return ServiceResult<DealResponse>.Fail("NOT_FOUND", "Không tìm thấy deal.");

        if (request.Probability is < 0 or > 100)
            throw new ValidationException("probability", "Probability phải trong khoảng 0-100.");

        if (request.Stage != null)
        {
            var stages = await _pipelineStageService.GetAsync(ct);
            if (!stages.Any(x => x.Name.Equals(request.Stage, StringComparison.OrdinalIgnoreCase)))
                throw new ValidationException("stage", "Stage không hợp lệ.");
        }

        var update = Builders<Deal>.Update.Set(x => x.updatedAt, DateTime.UtcNow);
        if (request.Title != null) update = update.Set(x => x.title, request.Title.Trim());
        if (request.Customer != null) update = update.Set(x => x.customerId, request.Customer);
        if (request.Value.HasValue) update = update.Set(x => x.value, request.Value.Value);
        if (request.ExpectedRevenue.HasValue) update = update.Set(x => x.expectedRevenue, request.ExpectedRevenue.Value);
        if (request.Currency != null) update = update.Set(x => x.currency, request.Currency.Trim().ToUpperInvariant());
        if (request.ExpectedCloseDate.HasValue) update = update.Set(x => x.expectedCloseDate, request.ExpectedCloseDate.Value);
        if (request.Owner != null) update = update.Set(x => x.ownerId, request.Owner);
        if (request.Probability.HasValue) update = update.Set(x => x.probability, request.Probability.Value);
        if (request.Notes != null) update = update.Set(x => x.notes, request.Notes.Trim());
        if (request.Contacts != null) update = update.Set(x => x.contacts, request.Contacts);
        if (request.Quotations != null) update = update.Set(x => x.quotations, request.Quotations);

        if (request.Stage != null && !request.Stage.Equals(existing.stage, StringComparison.OrdinalIgnoreCase))
        {
            var history = new DealStageHistoryItem
            {
                stage = request.Stage,
                changedAt = DateTime.UtcNow,
                changedBy = _currentUser.UserId
            };
            update = update
                .Set(x => x.stage, request.Stage)
                .Push(x => x.stageHistory, history);
        }

        await _dealRepo.UpdateAsync(id, update, ct);

        if (request.Stage != null && !request.Stage.Equals(existing.stage, StringComparison.OrdinalIgnoreCase))
        {
            await _activityAutoLog.LogDealStageChangedAsync(
                existing.customerId, id, existing.title, existing.stage, request.Stage, ct);
        }

        return await GetByIdAsync(id, ct);
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var success = await _dealRepo.SoftDeleteAsync(id, ct);
        return success
            ? ServiceResult.Ok()
            : ServiceResult.Fail("NOT_FOUND", "Không tìm thấy deal.");
    }

    public async Task<ServiceResult<DealResponse>> ChangeStageAsync(string id, ChangeDealStageRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Stage))
            throw new ValidationException("stage", "Stage là bắt buộc.");

        var stages = await _pipelineStageService.GetAsync(ct);
        if (!stages.Any(x => x.Name.Equals(request.Stage, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("stage", "Stage không hợp lệ.");

        var existing = await _dealRepo.FindByIdAsync(id, ct);
        if (existing == null)
            return ServiceResult<DealResponse>.Fail("NOT_FOUND", "Không tìm thấy deal.");

        var oldStage = existing.stage;

        var history = new DealStageHistoryItem
        {
            stage = request.Stage,
            changedAt = DateTime.UtcNow,
            changedBy = _currentUser.UserId
        };

        var update = Builders<Deal>.Update
            .Set(x => x.stage, request.Stage)
            .Set(x => x.updatedAt, DateTime.UtcNow)
            .Push(x => x.stageHistory, history);

        await _dealRepo.UpdateAsync(id, update, ct);

        await _activityAutoLog.LogDealStageChangedAsync(
            existing.customerId, id, existing.title, oldStage, request.Stage, ct);

        return await GetByIdAsync(id, ct);
    }

    private async Task<ServiceResult<DealResponse>> ValidateDealDataAsync(string customerId, string? ownerId, string stage, int probability, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(customerId))
            throw new ValidationException("customer", "Customer là bắt buộc.");
        if (string.IsNullOrWhiteSpace(stage))
            throw new ValidationException("stage", "Stage là bắt buộc.");
        if (probability is < 0 or > 100)
            throw new ValidationException("probability", "Probability phải trong khoảng 0-100.");

        var customer = await _customerRepo.FindByIdAsync(customerId, ct);
        if (customer == null)
            return ServiceResult.Fail("INVALID_CUSTOMER", "Customer không tồn tại.").ToTyped<DealResponse>();

        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            var owner = await _userRepo.FindByIdAsync(ownerId, ct);
            if (owner == null)
                return ServiceResult.Fail("INVALID_OWNER", "Owner không tồn tại.").ToTyped<DealResponse>();
        }

        var stages = await _pipelineStageService.GetAsync(ct);
        if (!stages.Any(x => x.Name.Equals(stage, StringComparison.OrdinalIgnoreCase)))
            throw new ValidationException("stage", "Stage không hợp lệ.");

        return ServiceResult<DealResponse>.Ok(new DealResponse());
    }

    private static SortDefinition<Deal> BuildSort(string? sort)
    {
        var field = "updatedAt";
        var direction = "desc";

        if (!string.IsNullOrWhiteSpace(sort))
        {
            var parts = sort.Split(':', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (parts.Length > 0) field = parts[0];
            if (parts.Length > 1) direction = parts[1];
        }

        var desc = direction.Equals("desc", StringComparison.OrdinalIgnoreCase);

        return field.ToLowerInvariant() switch
        {
            "title" => desc ? Builders<Deal>.Sort.Descending(x => x.title) : Builders<Deal>.Sort.Ascending(x => x.title),
            "value" => desc ? Builders<Deal>.Sort.Descending(x => x.value) : Builders<Deal>.Sort.Ascending(x => x.value),
            "createdat" => desc ? Builders<Deal>.Sort.Descending(x => x.createdAt) : Builders<Deal>.Sort.Ascending(x => x.createdAt),
            _ => desc ? Builders<Deal>.Sort.Descending(x => x.updatedAt) : Builders<Deal>.Sort.Ascending(x => x.updatedAt)
        };
    }
}

