using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

public interface IQuotationService
{
    Task<List<QuotationResponse>> GetListByDealIdAsync(string dealId, CancellationToken ct = default);
    Task<ServiceResult<QuotationResponse>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<QuotationResponse>> CreateAsync(string dealId, CreateQuotationRequest request, CancellationToken ct = default);
    Task<ServiceResult<QuotationResponse>> UpdateAsync(string id, UpdateQuotationRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
}
