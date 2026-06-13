using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class CustomerMapper
{
    public static CustomerResponse ToResponse(
        this Customer c, User? owner = null, OrgDepartment? dept = null)
        => new()
        {
            Id             = c.id,
            CustomerCode   = c.customerCode,
            Name           = c.name,
            Status         = c.status,
            Source         = c.source,
            Email          = c.email,
            Phone          = c.phone,
            OwnerId        = c.ownerId,
            OwnerName      = owner?.displayName ?? string.Empty,
            OwnerAvatarUrl = owner?.avatarUrl,
            DepartmentId   = c.departmentId,
            DepartmentName = dept?.name ?? string.Empty,
            Contacts       = c.contacts.ToResponseList(),
            CustomFields   = c.customFields?.ToDictionary(),
            CreatedAt      = c.createdAt,
            UpdatedAt      = c.updatedAt,
        };

    public static CustomerSummaryResponse ToSummaryResponse(
        this Customer c, User? owner = null)
        => new()
        {
            Id             = c.id,
            CustomerCode   = c.customerCode,
            Name           = c.name,
            Status         = c.status,
            Source         = c.source,
            OwnerName      = owner?.displayName ?? string.Empty,
            OwnerAvatarUrl = owner?.avatarUrl,
            DepartmentName = string.Empty,
            Email          = c.email,
            Phone          = c.phone,
            CreatedAt      = c.createdAt,
            UpdatedAt      = c.updatedAt,
        };

    public static CustomerInfoTabResponse ToInfoTab(
        this Customer c, User? owner = null, OrgDepartment? dept = null)
        => new()
        {
            Id             = c.id,
            CustomerCode   = c.customerCode,
            Name           = c.name,
            Status         = c.status,
            Source         = c.source,
            Email          = c.email,
            Phone          = c.phone,
            OwnerId        = c.ownerId,
            OwnerName      = owner?.displayName ?? string.Empty,
            OwnerAvatarUrl = owner?.avatarUrl,
            DepartmentId   = c.departmentId,
            DepartmentName = dept?.name ?? string.Empty,
            CustomFields   = c.customFields?.ToDictionary(),
            CreatedAt      = c.createdAt,
            UpdatedAt      = c.updatedAt,
        };

    public static List<CustomerSummaryResponse> ToSummaryResponseList(
        this IEnumerable<Customer> customers, Func<string, User?> ownerLookup)
        => customers.Select(c => c.ToSummaryResponse(ownerLookup(c.ownerId))).ToList();
}
