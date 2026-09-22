namespace DuongNhan.ApiService.Services;

internal interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream content, string originalFileName, string contentType, CancellationToken ct = default);
    Task<(Stream Stream, string ContentType)?> GetFileAsync(string storageKey, CancellationToken ct = default);
    Task DeleteFileAsync(string storageKey, CancellationToken ct = default);
}
