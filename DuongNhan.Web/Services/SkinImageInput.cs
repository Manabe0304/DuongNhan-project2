namespace DuongNhan.Web.Services;

/// <summary>
/// A captured or selected skin image, normalised so the camera and file-picker
/// paths produce the same payload for upload.
/// </summary>
public sealed record SkinImageInput(byte[] Bytes, string FileName, string ContentType)
{
    public string ToDataUrl() => $"data:{ContentType};base64,{Convert.ToBase64String(Bytes)}";

    public static SkinImageInput? FromDataUrl(string dataUrl, string fallbackName = "capture.jpg")
    {
        if (string.IsNullOrWhiteSpace(dataUrl) || !dataUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return null;

        var comma = dataUrl.IndexOf(',');
        if (comma <= 0) return null;

        var header = dataUrl[5..comma];
        var payload = dataUrl[(comma + 1)..];
        if (!header.Contains("base64", StringComparison.OrdinalIgnoreCase)) return null;

        var contentType = header.Split(';')[0];
        if (string.IsNullOrWhiteSpace(contentType)) contentType = "image/jpeg";

        try
        {
            var bytes = Convert.FromBase64String(payload);
            var extension = contentType switch
            {
                "image/png" => ".png",
                "image/webp" => ".webp",
                _ => ".jpg"
            };

            return new SkinImageInput(bytes, Path.ChangeExtension(fallbackName, extension), contentType);
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
