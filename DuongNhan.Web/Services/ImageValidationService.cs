namespace DuongNhan.Web.Services;

public sealed class ImageValidationService
{
    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    public Task<ImageValidationResult> ValidateAsync(Stream stream, string fileName, string contentType)
    {
        if (!AllowedTypes.Contains(contentType))
        {
            return Task.FromResult(new ImageValidationResult(
                false, "Only JPEG, PNG, or WebP images are allowed."));
        }

        if (stream.Length > 10 * 1024 * 1024)
        {
            return Task.FromResult(new ImageValidationResult(
                false, "Image must be under 10 MB."));
        }

        return Task.FromResult(new ImageValidationResult(true, null));
    }
}

public sealed record ImageValidationResult(bool IsValid, string? Message);