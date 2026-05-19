using Microsoft.AspNetCore.Authorization;

namespace CRM.Api.Shared.Authorization;

/// <summary>Role <= MaxRole thì pass.</summary>
public sealed class RoleRequirement : IAuthorizationRequirement
{
    public int MaxRole { get; }
    public RoleRequirement(int maxRole) => MaxRole = maxRole;
}
