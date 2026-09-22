namespace DuongNhan.ApiService.Services;

internal sealed class LocalFileStorageService(IWebHostEnvironment environment) : IFileStorageService
{
    private readonly string _basePath = Path.Combine(environment.ContentRootPath, "uploads");

    public async Task<string> SaveFileAsync(
        Stream content,
        string originalFileName,
        string contentType,
        CancellationToken ct = default)
    {
        var ext = Path.GetExtension(originalFileName).ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(ext))
        {
            ext = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };
        }

        var now = DateTime.UtcNow;
        var relativeDir = Path.Combine("skin-images", now.ToString("yyyy"), now.ToString("MM"));
        var targetDir = Path.Combine(_basePath, relativeDir);

        Directory.CreateDirectory(targetDir);

        var fileId = Guid.NewGuid().ToString("N");
        var fileName = $"{fileId}{ext}";
        var fullPath = Path.Combine(targetDir, fileName);

        await using (var fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true))
        {
            await content.CopyToAsync(fs, ct);
        }

        var storageKey = Path.Combine(relativeDir, fileName).Replace('\\', '/');
        return storageKey;
    }

    public Task<(Stream Stream, string ContentType)?> GetFileAsync(string storageKey, CancellationToken ct = default)
    {
        var sanitizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, sanitizedKey);

        if (!File.Exists(fullPath))
        {
            return Task.FromResult<(Stream Stream, string ContentType)?>(null);
        }

        var ext = Path.GetExtension(fullPath).ToLowerInvariant();
        var contentType = ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".gif" => "image/gif",
            _ => "image/jpeg"
        };

        Stream fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult<(Stream Stream, string ContentType)?>((fs, contentType));
    }

    public Task DeleteFileAsync(string storageKey, CancellationToken ct = default)
    {
        var sanitizedKey = storageKey.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
        var fullPath = Path.Combine(_basePath, sanitizedKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }
}
