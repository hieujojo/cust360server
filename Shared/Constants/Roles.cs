namespace CRM.Api.Shared.Constants;

/// <summary>1 = Owner | 2 = Admin | 3 = User</summary>
public static class Roles
{
    public const int Owner = 1;
    public const int Admin = 2;
    public const int User  = 3;

    public static string GetName(int role) => role switch
    {
        Owner => "Owner",
        Admin => "Admin",
        User  => "User",
        _     => "Unknown"
    };

    public static bool IsValid(int role) => role is Owner or Admin or User;
}
