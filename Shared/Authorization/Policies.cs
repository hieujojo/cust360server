namespace CRM.Api.Shared.Authorization;

/// <summary>Policy name constants dùng trong [Authorize(Policy = ...)].</summary>
public static class Policies
{
    public const string OwnerOnly    = "OwnerOnly";    // role = 1
    public const string AdminOrAbove = "AdminOrAbove"; // role <= 2
    public const string AnyRole      = "AnyRole";      // role <= 3
}
