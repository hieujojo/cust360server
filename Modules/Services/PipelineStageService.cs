using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

public sealed class PipelineStageService : IPipelineStageService
{
    private readonly IOrganizationRepository _organizationRepo;
    private readonly IDealRepository _dealRepo;

    public PipelineStageService(IOrganizationRepository organizationRepo, IDealRepository dealRepo)
    {
        _organizationRepo = organizationRepo;
        _dealRepo = dealRepo;
    }

    public async Task<List<PipelineStageResponse>> GetAsync(CancellationToken ct = default)
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        return org.pipelineStages
            .OrderBy(x => x.order)
            .Select(x => x.ToResponse())
            .ToList();
    }

    public async Task<ServiceResult<List<PipelineStageResponse>>> CreateAsync(UpsertPipelineStageRequest request, CancellationToken ct = default)
    {
        ValidateStageRequest(request);
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);

        if (org.pipelineStages.Any(x => x.name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return ServiceResult<List<PipelineStageResponse>>.Fail("DUPLICATE_STAGE", "Stage đã tồn tại.");

        org.pipelineStages.Add(new PipelineStage
        {
            name = request.Name.Trim(),
            color = request.Color,
            stuckThreshold = request.StuckThreshold,
            order = org.pipelineStages.Count == 0 ? 1 : org.pipelineStages.Max(x => x.order) + 1
        });

        await _organizationRepo.UpdatePipelineStagesAsync(org.pipelineStages, ct);
        return ServiceResult<List<PipelineStageResponse>>.Ok((await GetAsync(ct)));
    }

    public async Task<ServiceResult<List<PipelineStageResponse>>> UpdateAsync(string id, UpsertPipelineStageRequest request, CancellationToken ct = default)
    {
        ValidateStageRequest(request);
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var stage = org.pipelineStages.FirstOrDefault(x => x.id == id);
        if (stage == null)
            return ServiceResult<List<PipelineStageResponse>>.Fail("NOT_FOUND", "Không tìm thấy stage.");

        if (org.pipelineStages.Any(x => x.id != id && x.name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
            return ServiceResult<List<PipelineStageResponse>>.Fail("DUPLICATE_STAGE", "Tên stage đã tồn tại.");

        stage.name = request.Name.Trim();
        stage.color = request.Color;
        stage.stuckThreshold = request.StuckThreshold;

        await _organizationRepo.UpdatePipelineStagesAsync(org.pipelineStages, ct);
        return ServiceResult<List<PipelineStageResponse>>.Ok((await GetAsync(ct)));
    }

    public async Task<ServiceResult<List<PipelineStageResponse>>> DeleteAsync(string id, CancellationToken ct = default)
    {
        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        var stage = org.pipelineStages.FirstOrDefault(x => x.id == id);
        if (stage == null)
            return ServiceResult<List<PipelineStageResponse>>.Fail("NOT_FOUND", "Không tìm thấy stage.");

        var inUseCount = await _dealRepo.CountByStageAsync(stage.name, ct);
        if (inUseCount > 0)
            return ServiceResult<List<PipelineStageResponse>>.Fail("STAGE_IN_USE", "Không thể xóa stage vì còn deal đang sử dụng.");

        org.pipelineStages.RemoveAll(x => x.id == id);
        var order = 1;
        foreach (var item in org.pipelineStages.OrderBy(x => x.order))
            item.order = order++;

        await _organizationRepo.UpdatePipelineStagesAsync(org.pipelineStages, ct);
        return ServiceResult<List<PipelineStageResponse>>.Ok((await GetAsync(ct)));
    }

    public async Task<ServiceResult<List<PipelineStageResponse>>> ReorderAsync(ReorderPipelineStagesRequest request, CancellationToken ct = default)
    {
        if (request.StageIds.Count == 0)
            throw new ValidationException("stageIds", "Danh sách stageIds không được rỗng.");

        var org = await _organizationRepo.GetOrCreateCurrentAsync(ct);
        if (org.pipelineStages.Count != request.StageIds.Count || org.pipelineStages.Any(x => !request.StageIds.Contains(x.id)))
            throw new ValidationException("stageIds", "Danh sách stageIds không hợp lệ.");

        for (var i = 0; i < request.StageIds.Count; i++)
        {
            var stage = org.pipelineStages.First(x => x.id == request.StageIds[i]);
            stage.order = i + 1;
        }

        await _organizationRepo.UpdatePipelineStagesAsync(org.pipelineStages, ct);
        return ServiceResult<List<PipelineStageResponse>>.Ok((await GetAsync(ct)));
    }

    private static void ValidateStageRequest(UpsertPipelineStageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("name", "Tên stage là bắt buộc.");
        if (request.StuckThreshold < 0)
            throw new ValidationException("stuckThreshold", "stuckThreshold phải >= 0.");
    }
}

