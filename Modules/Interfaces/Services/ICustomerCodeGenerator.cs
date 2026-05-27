namespace CRM.Api.Modules.Interfaces.Services;

/// <summary>
/// Service sinh mã khách hàng tự động (Customer Code).
/// </summary>
public interface ICustomerCodeGenerator
{
    /// <summary>Sinh mã mới format: CUST-YYYY-NNNN</summary>
    Task<string> GenerateAsync(CancellationToken ct = default);

    /// <summary>Kiểm tra mã hợp lệ theo regex.</summary>
    bool IsValidFormat(string customerCode);
}
