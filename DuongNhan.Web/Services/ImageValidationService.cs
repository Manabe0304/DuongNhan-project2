using Microsoft.JSInterop;

namespace DuongNhan.Web.Services;

public sealed record ImageValidationResult(bool IsValid, string? Message);

/// <summary>Raw metrics produced by the browser for a candidate image.</summary>
public sealed record ImageQualityMetrics(
    int Width,
    int Height,
    double Brightness,
    double Contrast,
    double Sharpness,
    double UnderexposedRatio,
    double OverexposedRatio);

/// <summary>One graded aspect of the capture. <see cref="Status"/> is pass/warn/fail.</summary>
public sealed record QualityCheck(string Label, string Status, string Detail);

public sealed record ImageQualityReport(bool IsAcceptable, int Score, IReadOnlyList<QualityCheck> Checks);

/// <summary>
/// Validates uploads before they reach the API. Format and size are checked in
/// this class; exposure, contrast and focus are measured in the browser and
/// graded here. The goal is to reject an unusable image up front instead of
/// spending an AI run (and the user's quota) on it.
/// </summary>
public sealed class ImageValidationService(IJSRuntime js)
{
    private static readonly HashSet<string> AllowedTypes =
        new(StringComparer.OrdinalIgnoreCase) { "image/jpeg", "image/png", "image/webp" };

    private const long MaxBytes = 10 * 1024 * 1024;
    private const int IdealMinimumSide = 1024;

    public Task<ImageValidationResult> ValidateAsync(Stream stream, string fileName, string contentType)
    {
        if (!AllowedTypes.Contains(contentType))
        {
            return Task.FromResult(new ImageValidationResult(
                false, "Chỉ hỗ trợ ảnh JPEG, PNG hoặc WebP."));
        }

        if (stream.Length > MaxBytes)
        {
            return Task.FromResult(new ImageValidationResult(
                false, "Ảnh vượt quá 10 MB. Vui lòng chọn ảnh nhỏ hơn."));
        }

        return Task.FromResult(new ImageValidationResult(true, null));
    }

    public Task<ImageValidationResult> ValidateAsync(SkinImageInput input)
    {
        if (!AllowedTypes.Contains(input.ContentType))
            return Task.FromResult(new ImageValidationResult(false, "Chỉ hỗ trợ ảnh JPEG, PNG hoặc WebP."));

        if (input.Bytes.LongLength > MaxBytes)
            return Task.FromResult(new ImageValidationResult(false, "Ảnh vượt quá 10 MB. Vui lòng chọn ảnh nhỏ hơn."));

        return Task.FromResult(new ImageValidationResult(true, null));
    }

    /// <summary>
    /// Measures brightness, contrast and focus in the browser and grades them.
    /// Returns null when the browser cannot analyse the image (for example
    /// during prerendering), so the flow can fall back to server-side checks.
    /// </summary>
    public async Task<ImageQualityReport?> AssessAsync(string dataUrl, CancellationToken ct = default)
    {
        try
        {
            var module = await js.InvokeAsync<IJSObjectReference>("import", ct, "./js/camera.js");
            var metrics = await module.InvokeAsync<ImageQualityMetrics?>("analyzeImage", ct, dataUrl);
            await module.DisposeAsync();
            return metrics is null ? null : Grade(metrics);
        }
        catch
        {
            return null;
        }
    }

    private static ImageQualityReport Grade(ImageQualityMetrics metrics)
    {
        var checks = new List<QualityCheck>();
        var score = 0;

        var minimumSide = Math.Min(metrics.Width, metrics.Height);
        var resolution = $"{metrics.Width}×{metrics.Height}px";

        if (minimumSide >= IdealMinimumSide)
        {
            checks.Add(new QualityCheck("Độ phân giải", "pass", resolution));
            score += 25;
        }
        else if (minimumSide >= 720)
        {
            checks.Add(new QualityCheck("Độ phân giải", "warn", $"{resolution} — nên đạt tối thiểu {IdealMinimumSide}px"));
            score += 14;
        }
        else
        {
            checks.Add(new QualityCheck("Độ phân giải", "fail", $"{resolution} — quá thấp, hãy chụp gần hơn"));
        }

        if (metrics.Brightness is >= 70 and <= 190)
        {
            checks.Add(new QualityCheck("Độ sáng", "pass", $"{(int)metrics.Brightness}/255"));
            score += 20;
        }
        else if (metrics.Brightness is >= 45 and <= 215)
        {
            checks.Add(new QualityCheck("Độ sáng", "warn", $"{(int)metrics.Brightness}/255 — ánh sáng chưa lý tưởng"));
            score += 11;
        }
        else
        {
            var advice = metrics.Brightness < 45 ? "quá tối, hãy tăng ánh sáng" : "quá chói, hãy giảm ánh sáng trực tiếp";
            checks.Add(new QualityCheck("Độ sáng", "fail", $"{(int)metrics.Brightness}/255 — {advice}"));
        }

        if (metrics.UnderexposedRatio > 0.35)
            checks.Add(new QualityCheck("Vùng tối", "fail", "Nhiều vùng bị thiếu sáng, dễ che mất chi tiết da"));
        else if (metrics.UnderexposedRatio > 0.18)
            checks.Add(new QualityCheck("Vùng tối", "warn", "Có vùng tối đáng kể trong khung hình"));
        else
            checks.Add(new QualityCheck("Vùng tối", "pass", "Phân bố sáng đều"));

        if (metrics.OverexposedRatio > 0.25)
            checks.Add(new QualityCheck("Cháy sáng", "fail", "Ảnh bị cháy sáng (glare), hãy tránh đèn chiếu trực tiếp"));
        else if (metrics.OverexposedRatio > 0.12)
            checks.Add(new QualityCheck("Cháy sáng", "warn", "Có điểm cháy sáng trên da"));
        else
            checks.Add(new QualityCheck("Cháy sáng", "pass", "Không có điểm cháy sáng"));

        if (metrics.Sharpness >= 12)
        {
            checks.Add(new QualityCheck("Độ nét", "pass", "Ảnh rõ nét"));
            score += 30;
        }
        else if (metrics.Sharpness >= 6)
        {
            checks.Add(new QualityCheck("Độ nét", "warn", "Ảnh hơi mờ, hãy giữ máy ổn định"));
            score += 16;
        }
        else
        {
            checks.Add(new QualityCheck("Độ nét", "fail", "Ảnh mờ — giữ chắc tay và lấy nét vào vùng da"));
        }

        if (metrics.Contrast >= 22)
        {
            checks.Add(new QualityCheck("Tương phản", "pass", "Tương phản tốt"));
            score += 25;
        }
        else if (metrics.Contrast >= 12)
        {
            checks.Add(new QualityCheck("Tương phản", "warn", "Tương phản thấp, chi tiết da khó phân biệt"));
            score += 12;
        }
        else
        {
            checks.Add(new QualityCheck("Tương phản", "fail", "Ảnh quá phẳng, hãy dùng ánh sáng đều và rõ hơn"));
        }

        var acceptable = checks.All(c => c.Status != "fail");
        return new ImageQualityReport(acceptable, Math.Clamp(score, 0, 100), checks);
    }
}
