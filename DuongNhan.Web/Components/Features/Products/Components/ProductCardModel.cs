using System.Text.Json;
using DuongNhan.Shared.Contracts;

namespace DuongNhan.Web.Components.Features.Products.Components;

/// <summary>View model shared by the catalogue (/products) and the personalised routine (/recommendations).</summary>
public sealed record ProductCardModel(
    Guid? Id,
    string Name,
    string Brand,
    string Category,
    decimal Price,
    string? Image,
    string? Description,
    string? Usage,
    string? AffiliateUrl,
    IReadOnlyList<string> Conditions,
    string SkinTypes,
    int Step,
    int? MatchPercentage = null,
    string? Reason = null)
{
    public bool IsShopee => AffiliateUrl is not null
        && Uri.TryCreate(AffiliateUrl, UriKind.Absolute, out var uri)
        && uri.Host.Contains("shopee", StringComparison.OrdinalIgnoreCase);

    public string PriceText => Price <= 0
        ? "Liên hệ"
        : Price.ToString("N0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') + " ₫";

    public IEnumerable<string> SkinTypeCodes => SkinTypes
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    /// <summary>Parses a product as returned by GET /api/products (camelCase JSON).</summary>
    public static ProductCardModel FromJson(JsonElement e, int? match = null, string? reason = null)
    {
        var category = Text(e, "category");
        return new ProductCardModel(
            Id: Guid.TryParse(Text(e, "id"), out var id) ? id : null,
            Name: Text(e, "name", "Sản phẩm chăm sóc da"),
            Brand: Text(e, "brand", "Dưỡng Nhan"),
            Category: category,
            Price: Number(e, "price"),
            Image: NullIfEmpty(Text(e, "imageUrl")),
            Description: NullIfEmpty(Text(e, "description")),
            Usage: NullIfEmpty(Text(e, "usageInstructions")),
            AffiliateUrl: NullIfEmpty(Text(e, "affiliateUrl")),
            Conditions: Text(e, "targetConditions")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(c => !HiddenCodes.Contains(c))
                .Select(ConditionLabel)
                .ToList(),
            SkinTypes: SkincareCatalog.NormalizeSkinTypes(Text(e, "skinType")),
            Step: SkincareCatalog.ResolveStep(Int(e, "step"), category),
            MatchPercentage: match,
            Reason: reason);
    }

    /// <summary>Parses one entry of GET /api/products/recommend ({ product, reason, matchPercentage, stepOrder }).</summary>
    public static ProductCardModel? FromRecommendation(JsonElement e)
    {
        if (!e.TryGetProperty("product", out var product)) return null;

        var model = FromJson(product,
            match: (int?)Number(e, "matchPercentage"),
            reason: NullIfEmpty(Text(e, "reason")));

        var order = Int(e, "stepOrder");
        return order is >= SkincareCatalog.MinStep and <= SkincareCatalog.MaxStep ? model with { Step = order.Value } : model;
    }

    /// <summary>Codes that are stored in TargetConditions but are not shown as a "#tag" on the card.</summary>
    private static readonly HashSet<string> HiddenCodes = new(StringComparer.OrdinalIgnoreCase)
    {
        "healthy",
        "allskintypes" // old Excel files put the skin type here; it is shown through SkinTypes instead
    };

    public static string ConditionLabel(string code) => code.Trim().ToLowerInvariant() switch
    {
        // Skin conditions
        "acne" or "acnecare" => "Mụn",
        "enlargedpores" => "Lỗ chân lông to",
        "hyperpigmentation" => "Thâm & sắc tố",
        "rosacea" => "Đỏ da",
        "eczema" => "Da khô / chàm",
        "melasma" => "Nám da",
        "seborrheicdermatitis" => "Viêm da tiết bã",
        "contactdermatitis" => "Kích ứng da",
        "psoriasis" => "Vảy nến",
        "fungalinfection" => "Nấm da",

        // Product goals
        "makeupremoval" => "Tẩy trang",
        "deepcleansing" => "Làm sạch sâu",
        "facialcleansing" => "Làm sạch da mặt",
        "phbalance" => "Cân bằng pH",
        "hydration" => "Cấp nước",
        "moisturizing" => "Dưỡng ẩm",
        "skinrepair" => "Phục hồi da",
        "antiaging" => "Chống lão hóa",
        "skinbrightening" => "Làm sáng da",
        _ => code
    };

    private static string? NullIfEmpty(string value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string Text(JsonElement e, string name, string fallback = "")
    {
        if (e.TryGetProperty(name, out var p) && p.ValueKind != JsonValueKind.Null)
            return p.ValueKind == JsonValueKind.String ? p.GetString() ?? fallback : p.ToString();
        return fallback;
    }

    private static decimal Number(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetDecimal(out var d) ? d : 0;

    private static int? Int(JsonElement e, string name)
        => e.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.Number && p.TryGetInt32(out var n) ? n : null;
}
