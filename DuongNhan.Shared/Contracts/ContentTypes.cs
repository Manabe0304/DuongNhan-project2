namespace DuongNhan.Shared.Contracts;

public static class SupportedContentTypes
{
    public const string Jpeg = "image/jpeg";
    public const string Png = "image/png";
    public const string WebP = "image/webp";

    public static readonly IReadOnlySet<string> AllowedImages =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            Jpeg, Png, WebP
        };
}