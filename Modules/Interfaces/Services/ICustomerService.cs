using CRM.Api.Modules.DTOs;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>
/// Service quản lý Customer và Contact (embedded).
/// </summary>
public interface ICustomerService
{
    // ─── CRUD ─────────────────────────────────────────────────────────────────
    Task<ServiceResult<CustomerResponse>> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<ServiceResult<CustomerResponse>> GetByIdAsync(string id, CancellationToken ct = default);
    Task<ServiceResult<CustomerResponse>> UpdateAsync(string id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<ServiceResult> DeleteAsync(string id, CancellationToken ct = default);
    Task<ServiceResult> RestoreAsync(string id, CancellationToken ct = default);

    // ─── List & Search ────────────────────────────────────────────────────────
    Task<CustomerListResponse> GetListAsync(CustomerListFilterRequest filter, CancellationToken ct = default);
    Task<CustomerSearchResponse> SearchAsync(string query, CancellationToken ct = default);
    Task<CustomerStatsResponse> GetStatsAsync(CancellationToken ct = default);

    // ─── 360 View ─────────────────────────────────────────────────────────────
    Task<ServiceResult<Customer360ViewResponse>> Get360ViewAsync(string id, CancellationToken ct = default);

    // ─── Status & Owner ───────────────────────────────────────────────────────
    Task<ServiceResult> UpdateStatusAsync(string id, UpdateCustomerStatusRequest request, CancellationToken ct = default);
    Task<ServiceResult> UpdateOwnerAsync(string id, UpdateCustomerOwnerRequest request, CancellationToken ct = default);

    // ─── Contacts ─────────────────────────────────────────────────────────────
    Task<ServiceResult<ContactResponse>> AddContactAsync(string customerId, CreateContactRequest request, CancellationToken ct = default);
    Task<ServiceResult<ContactResponse>> UpdateContactAsync(string customerId, string contactId, CreateContactRequest request, CancellationToken ct = default);
    Task<ServiceResult> RemoveContactAsync(string customerId, string contactId, CancellationToken ct = default);
    Task<ServiceResult> SetPrimaryContactAsync(string customerId, string contactId, CancellationToken ct = default);
}
