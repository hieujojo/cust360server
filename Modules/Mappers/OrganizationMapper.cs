using CRM.Api.Modules.DTOs;
using CRM.Api.Modules.Models;

namespace CRM.Api.Modules.Mappers;

public static class OrganizationMapper
{
    public static OrganizationProfileResponse ToProfileResponse(this Organization org)
        => new()
        {
            Id = org.id,
            OrganizationId = org.organizationId,
            Name = org.name ?? string.Empty,
            LogoUrl = org.logoUrl,
            Timezone = org.timezone,
            Currency = org.currency,
            Language = org.language,
        };
}
