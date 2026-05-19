namespace CRM.Api.Shared.Constants;

public static class CacheKeys
{
    public static string UserActive(string userId) => $"user:active:{userId}";
}
