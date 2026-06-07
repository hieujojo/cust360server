using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class DealMapper
{
    public static DealResponse ToResponse(this Deal deal, User? owner = null, Customer? customer = null)
        => new()
        {
            Id = deal.id,
            Title = deal.title,
            CustomerId = deal.customerId,
            CustomerName = customer?.name ?? string.Empty,
            Value = deal.value,
            ExpectedRevenue = deal.expectedRevenue,
            Currency = deal.currency,
            ExpectedCloseDate = deal.expectedCloseDate,
            OwnerId = deal.ownerId,
            OwnerName = owner?.displayName ?? string.Empty,
            Stage = deal.stage,
            Probability = deal.probability,
            Notes = deal.notes,
            StageHistory = deal.stageHistory
                .OrderByDescending(x => x.changedAt)
                .Select(x => new DealStageHistoryResponse
                {
                    Stage = x.stage,
                    ChangedAt = x.changedAt,
                    ChangedBy = x.changedBy
                }).ToList(),
            Contacts = deal.contacts,
            Quotations = deal.quotations,
            CreatedAt = deal.createdAt,
            UpdatedAt = deal.updatedAt
        };

    public static PipelineStageResponse ToResponse(this PipelineStage stage)
        => new()
        {
            Id = stage.id,
            Name = stage.name,
            Order = stage.order,
            Color = stage.color,
            DefaultProbability = stage.defaultProbability,
            StuckThreshold = stage.stuckThreshold
        };
}

