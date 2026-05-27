using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class ContactMapper
{
    public static ContactResponse ToResponse(this Contact c)
        => new()
        {
            Id        = c.id,
            Name      = c.name,
            Role      = c.role,
            Email     = c.email,
            Phone     = c.phone,
            IsPrimary = c.isPrimary,
            CreatedAt = c.createdAt,
        };

    public static List<ContactResponse> ToResponseList(this IEnumerable<Contact> contacts)
        => contacts.Select(c => c.ToResponse()).ToList();
}
