using System.Security.Cryptography;
using System.Text;
using CRM.Api.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CRM.Api.Infrastructure.Google;

public interface IGoogleTokenProtector
{
    string Protect(string plainText);
    string Unprotect(string cipherText);
}

public sealed class GoogleTokenProtector : IGoogleTokenProtector
{
    private readonly byte[] _key;

    public GoogleTokenProtector(IOptions<GoogleSettings> options)
    {
        var keyMaterial = options.Value.TokenEncryptionKey;
        if (string.IsNullOrWhiteSpace(keyMaterial))
            keyMaterial = "crm360-default-dev-key-change-in-production-32";

        _key = SHA256.HashData(Encoding.UTF8.GetBytes(keyMaterial));
    }

    public string Protect(string plainText)
    {
        using var aes = Aes.Create();
        aes.Key = _key;
        aes.GenerateIV();
        using var encryptor = aes.CreateEncryptor();
        var plainBytes = Encoding.UTF8.GetBytes(plainText);
        var cipherBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        var combined = aes.IV.Concat(cipherBytes).ToArray();
        return Convert.ToBase64String(combined);
    }

    public string Unprotect(string cipherText)
    {
        var combined = Convert.FromBase64String(cipherText);
        using var aes = Aes.Create();
        aes.Key = _key;
        var iv = combined.Take(16).ToArray();
        var cipherBytes = combined.Skip(16).ToArray();
        aes.IV = iv;
        using var decryptor = aes.CreateDecryptor();
        var plainBytes = decryptor.TransformFinalBlock(cipherBytes, 0, cipherBytes.Length);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
