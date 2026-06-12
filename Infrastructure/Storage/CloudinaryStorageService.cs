using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using CRM.Api.Infrastructure.Settings;
using Microsoft.Extensions.Options;

namespace CRM.Api.Infrastructure.Storage;

public sealed class CloudinaryStorageService : ICloudinaryStorageService
{
    private readonly Cloudinary? _cloudinary;

    public CloudinaryStorageService(IOptions<CloudinarySettings> options)
    {
        var settings = options.Value;
        if (!string.IsNullOrWhiteSpace(settings.CloudName))
        {
            var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
            _cloudinary = new Cloudinary(account);
            _cloudinary.Api.Secure = true;
        }
    }

    public async Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default
    )
    {
        var uploadParams = new ImageUploadParams
        {
            File = new FileDescription(fileName, content),
            Folder = folder,
            UseFilename = true,
            UniqueFilename = true,
            Overwrite = false,
        };

        if (_cloudinary == null)
        {
            throw new InvalidOperationException("Cloudinary is not configured.");
        }

        var uploadResult = await _cloudinary.UploadAsync(uploadParams, ct);

        if (uploadResult.Error != null)
        {
            throw new Exception(uploadResult.Error.Message);
        }

        return uploadResult.SecureUrl.ToString();
    }

    public async Task DeleteByUrlAsync(string? fileUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(fileUrl))
            return;

        try
        {
            // Extract publicId from URL
            // Format: https://res.cloudinary.com/<cloud_name>/image/upload/v<version>/<folder>/<filename>.<ext>
            var uri = new Uri(fileUrl);
            var path = uri.AbsolutePath;
            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            // Find "upload" and the subsequent version "v1234"
            var uploadIndex = Array.IndexOf(segments, "upload");
            if (uploadIndex == -1 || uploadIndex + 1 >= segments.Length)
                return;

            var startIndex = uploadIndex + 1;
            // Skip version if present
            if (
                segments[startIndex].StartsWith("v")
                && segments[startIndex].Length > 1
                && char.IsDigit(segments[startIndex][1])
            )
            {
                startIndex++;
            }

            var publicIdWithExtension = string.Join("/", segments.Skip(startIndex));
            var publicId = Path.GetFileNameWithoutExtension(publicIdWithExtension);
            var folderPath = Path.GetDirectoryName(publicIdWithExtension)?.Replace('\\', '/');

            if (!string.IsNullOrEmpty(folderPath))
            {
                publicId = $"{folderPath}/{publicId}";
            }

            var deletionParams = new DeletionParams(publicId) { ResourceType = ResourceType.Image };

            if (_cloudinary != null)
            {
                await _cloudinary.DestroyAsync(deletionParams);
            }
        }
        catch
        {
            // Ignore deletion errors
        }
    }
}
