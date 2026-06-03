using System.Text.RegularExpressions;
using CRM.Api.Modules.Interfaces.Repositories;
using CRM.Api.Modules.Interfaces.Services;
using CRM.Api.Shared.Exceptions;
using CRM.Api.Shared.Models;

namespace CRM.Api.Modules.Services;

/// <summary>
/// Auto-generate CustomerCode (format: CUST-YYYY-NNNN).
/// Dùng collection counters (atomic findAndModify) để cấp sequence thread-safe.
/// </summary>
public sealed class CustomerCodeGenerator : ICustomerCodeGenerator
{
    private readonly ICustomerRepository _customerRepo;
    private readonly CurrentUser _currentUser;

    public CustomerCodeGenerator(ICustomerRepository customerRepo, CurrentUser currentUser)
    {
        _customerRepo = customerRepo;
        _currentUser = currentUser;
    }

    public async Task<string> GenerateAsync(CancellationToken ct = default)
    {
        var currentYear = DateTime.UtcNow.Year;
        // Key format: customer_code_orgId_2024
        var sequenceKey = $"customer_code_{_currentUser.OrganizationId}_{currentYear}";
        var maxRetries = 10;

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            var seq = await _customerRepo.GetNextSequenceAsync(sequenceKey, ct);
            var customerCode = $"CUST-{currentYear}-{seq:D4}";

            if (await _customerRepo.IsCustomerCodeUniqueAsync(customerCode, ct))
            {
                return customerCode;
            }
        }
        throw new InvalidOperationException(
            $"Cannot generate unique CustomerCode after {maxRetries} attempts."
        );
    }

    public bool IsValidFormat(string customerCode)
    {
        if (string.IsNullOrWhiteSpace(customerCode))
            return false;
        // Regex: CUST-4 chữ số-4 chữ số
        return Regex.IsMatch(customerCode, @"^CUST-\d{4}-\d{4}$");
    }
}
