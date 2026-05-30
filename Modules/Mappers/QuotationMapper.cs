using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class QuotationMapper
{
    public static QuotationResponse ToResponse(this Quotation q)
        => new()
        {
            Id = q.id,
            DealId = q.dealId,
            CustomerName = q.customerName,
            Code = q.code,
            TotalValue = q.totalValue,
            Currency = q.currency,
            Status = q.status,
            Notes = q.notes,
            Items = q.items.Select(i => new QuotationItemResponse
            {
                Description = i.description,
                Category = i.category,
                Quantity = i.quantity,
                UnitPrice = i.unitPrice,
                Total = i.total,
            }).ToList(),
            Version = q.version,
            ValidUntil = q.validUntil,
            CreatedAt = q.createdAt,
            UpdatedAt = q.updatedAt
        };
}
