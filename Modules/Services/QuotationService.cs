using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Modules.Mappers;
using CRM.Api.Modules.Models;
using CRM.Api.Shared.Models;
using MongoDB.Driver;
using System.Linq;

namespace CRM.Api.Modules.Services;

public sealed class QuotationService : IQuotationService
{
    private readonly IQuotationRepository _quotationRepo;
    private readonly IDealRepository _dealRepo;
    private readonly ICustomerRepository _customerRepo;

    public QuotationService(
        IQuotationRepository quotationRepo,
        IDealRepository dealRepo,
        ICustomerRepository customerRepo)
    {
        _quotationRepo = quotationRepo;
        _dealRepo = dealRepo;
        _customerRepo = customerRepo;
    }

    public async Task<List<QuotationResponse>> GetListByDealIdAsync(string dealId, CancellationToken ct = default)
    {
        var filter = Builders<Quotation>.Filter.Eq(x => x.dealId, dealId);
        var sort = Builders<Quotation>.Sort.Descending(x => x.createdAt);
        
        var quotes = await _quotationRepo.FindAsync(filter, sort, ct);
        return quotes.Select(q => q.ToResponse()).ToList();
    }

    public async Task<ServiceResult<QuotationResponse>> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var q = await _quotationRepo.FindByIdAsync(id, ct);
        if (q == null)
            return ServiceResult<QuotationResponse>.Fail("NOT_FOUND", "Không tìm thấy báo giá.");
        
        return ServiceResult<QuotationResponse>.Ok(q.ToResponse());
    }

    public async Task<ServiceResult<QuotationResponse>> CreateAsync(string dealId, CreateQuotationRequest request, CancellationToken ct = default)
    {
        var deal = await _dealRepo.FindByIdAsync(dealId, ct);
        if (deal == null)
            return ServiceResult<QuotationResponse>.Fail("NOT_FOUND", "Không tìm thấy giao dịch.");

        var customerName = string.Empty;
        if (!string.IsNullOrEmpty(deal.customerId))
        {
            var customer = await _customerRepo.FindByIdAsync(deal.customerId, ct);
            if (customer != null) customerName = customer.name;
        }

        var year = DateTime.UtcNow.Year;
        var latestCode = await _quotationRepo.GetLatestQuotationCodeAsync(year, ct);
        
        var nextNumber = 1;
        if (!string.IsNullOrEmpty(latestCode) && latestCode.StartsWith($"QUO-{year}-"))
        {
            var numStr = latestCode.Substring($"QUO-{year}-".Length);
            if (int.TryParse(numStr, out var parsed))
            {
                nextNumber = parsed + 1;
            }
        }
        
        var code = $"QUO-{year}-{nextNumber:D4}";

        var quote = new Quotation
        {
            dealId = dealId,
            customerName = customerName,
            code = code,
            totalValue = request.Items != null && request.Items.Any()
                ? request.Items.Sum(item => item.Quantity * item.UnitPrice)
                : request.TotalValue,
            currency = request.Currency,
            status = request.Status,
            notes = request.Notes,
            items = request.Items?.Select(item => new QuotationItem
            {
                description = item.Description,
                category = item.Category,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                total = item.Quantity * item.UnitPrice,
            }).ToList() ?? new List<QuotationItem>(),
            version = 1,
            validUntil = request.ValidUntil,
        };

        await _quotationRepo.InsertAsync(quote, ct);

        // Update Deal
        var updateDeal = Builders<Deal>.Update
            .Push(x => x.quotations, quote.id)
            .Set(x => x.updatedAt, DateTime.UtcNow);
        await _dealRepo.UpdateAsync(dealId, updateDeal, ct);

        return ServiceResult<QuotationResponse>.Ok(quote.ToResponse());
    }

    public async Task<ServiceResult<QuotationResponse>> UpdateAsync(string id, UpdateQuotationRequest request, CancellationToken ct = default)
    {
        var existing = await _quotationRepo.FindByIdAsync(id, ct);
        if (existing == null)
            return ServiceResult<QuotationResponse>.Fail("NOT_FOUND", "Không tìm thấy báo giá.");

        var update = Builders<Quotation>.Update
            .Set(x => x.updatedAt, DateTime.UtcNow)
            .Inc(x => x.version, 1);

        if (request.Items != null)
        {
            var items = request.Items.Select(item => new QuotationItem
            {
                description = item.Description,
                category = item.Category,
                quantity = item.Quantity,
                unitPrice = item.UnitPrice,
                total = item.Quantity * item.UnitPrice,
            }).ToList();
            update = update.Set(x => x.items, items);
            update = update.Set(x => x.totalValue, items.Sum(i => i.total));
        }
        else if (request.TotalValue.HasValue)
        {
            update = update.Set(x => x.totalValue, request.TotalValue.Value);
        }

        if (request.Currency != null) update = update.Set(x => x.currency, request.Currency);
        if (request.Status != null) update = update.Set(x => x.status, request.Status);
        if (request.Notes != null) update = update.Set(x => x.notes, request.Notes);
        if (request.ValidUntil.HasValue) update = update.Set(x => x.validUntil, request.ValidUntil);

        await _quotationRepo.UpdateAsync(id, update, ct);
        
        var updated = await _quotationRepo.FindByIdAsync(id, ct);
        return ServiceResult<QuotationResponse>.Ok(updated!.ToResponse());
    }

    public async Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default)
    {
        var quote = await _quotationRepo.FindByIdAsync(id, ct);
        if (quote == null)
            return ServiceResult.Fail("NOT_FOUND", "Không tìm thấy báo giá.");

        var success = await _quotationRepo.SoftDeleteAsync(id, ct);
        if (success)
        {
            // Remove from Deal.quotations
            var updateDeal = Builders<Deal>.Update
                .Pull(x => x.quotations, id)
                .Set(x => x.updatedAt, DateTime.UtcNow);
            await _dealRepo.UpdateAsync(quote.dealId, updateDeal, ct);
            return ServiceResult.Ok();
        }

        return ServiceResult.Fail("DELETE_FAILED", "Không thể xóa báo giá.");
    }
}
