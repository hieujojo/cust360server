namespace CRM.Api.Infrastructure.Storage;

public interface ICloudinaryStorageService
{
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folder,
        CancellationToken ct = default);

    Task DeleteByUrlAsync(string? fileUrl, CancellationToken ct = default);
}
