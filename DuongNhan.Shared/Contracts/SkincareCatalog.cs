namespace DuongNhan.Shared.Contracts;

/// <summary>One step of the skincare routine (cleanse → treat → serum → moisturise → protect).</summary>
public sealed record SkincareStepInfo(int Number, string Code, string Label, string Subtitle, string When);

/// <summary>A skin type a product is suited to.</summary>
public sealed record SkinTypeInfo(string Code, string Label);

/// <summary>
/// Single source of truth for the routine steps and skin types used by the products table
/// (<c>products."Step"</c> / <c>products."SkinType"</c>), the Excel import, the recommendation
/// endpoint and the /recommendations page.
/// </summary>
public static class SkincareCatalog
{
    /// <summary>Product is suitable for every skin type.</summary>
    public const string AllSkinTypes = "All";

    public const int MinStep = 1;
    public const int MaxStep = 6;

    public static readonly IReadOnlyList<SkincareStepInfo> Steps =
    [
        new(1, "cleanse",  "Làm sạch",         "Sữa rửa mặt, gel rửa mặt",              "Sáng & tối"),
        new(2, "treat",    "Điều trị / Tẩy da", "Treatment, BHA/AHA, tẩy tế bào chết",   "Theo nhu cầu"),
        new(3, "serum",    "Tinh chất",         "Serum, essence cấp ẩm & dưỡng sáng",    "Sáng & tối"),
        new(4, "moisturize", "Dưỡng ẩm",        "Kem dưỡng, gel dưỡng phục hồi",         "Sáng & tối"),
        new(5, "protect",  "Chống nắng",        "Kem chống nắng phổ rộng",               "Buổi sáng"),
        new(6, "extra",    "Chăm sóc thêm",     "Mặt nạ và sản phẩm bổ trợ",             "1–2 lần / tuần"),
    ];

    public static readonly IReadOnlyList<SkinTypeInfo> SkinTypes =
    [
        new("Oily",        "Da dầu"),
        new("Dry",         "Da khô"),
        new("Combination", "Da hỗn hợp"),
        new("Normal",      "Da thường"),
        new("Sensitive",   "Da nhạy cảm"),
    ];

    public static SkincareStepInfo? GetStep(int? number)
        => number is null ? null : Steps.FirstOrDefault(s => s.Number == number);

    public static string SkinTypeLabel(string code)
        => string.Equals(code, AllSkinTypes, StringComparison.OrdinalIgnoreCase)
            ? "Mọi loại da"
            : SkinTypes.FirstOrDefault(t => t.Code.Equals(code, StringComparison.OrdinalIgnoreCase))?.Label ?? code;

    /// <summary>Default routine step for a product category (used when no explicit step is set).</summary>
    public static int StepForCategory(string? category) => (category ?? string.Empty).Trim().ToLowerInvariant() switch
    {
        "cleanser" => 1,
        "treatment" or "exfoliant" or "toner" => 2,
        "serum" or "essence" or "ampoule" => 3,
        "moisturizer" or "moisturiser" or "cream" => 4,
        "sunscreen" => 5,
        _ => 6
    };

    /// <summary>
    /// Parses a step from a number ("3") or any English/Vietnamese label ("Serum", "Làm sạch", "cleanse").
    /// Returns null for empty or unrecognised input.
    /// </summary>
    public static int? ParseStep(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return null;
        var value = raw.Trim();

        if (int.TryParse(value, out var n))
            return n is >= MinStep and <= MaxStep ? n : null;

        var key = Fold(value);
        foreach (var step in Steps)
        {
            if (Fold(step.Code) == key || Fold(step.Label) == key) return step.Number;
        }

        // Also accept product category names ("Cleanser", "Sunscreen", ...).
        return key switch
        {
            "cleanser" => 1,
            "treatment" or "exfoliant" or "toner" => 2,
            "serum" or "essence" or "ampoule" => 3,
            "moisturizer" or "moisturiser" or "cream" => 4,
            "sunscreen" => 5,
            "mask" => 6,
            _ => null
        };
    }

    /// <summary>
    /// Normalises a free-text skin type list ("oily, da khô", "All", "Da nhạy cảm") to canonical,
    /// comma-separated codes, e.g. "Oily,Dry". Empty input means every skin type ("All").
    /// Unknown tokens are dropped.
    /// </summary>
    public static string NormalizeSkinTypes(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return AllSkinTypes;

        var found = new List<string>();
        foreach (var token in raw.Split([',', ';', '/', '|'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var key = Fold(token);
            if (key is "all" or "tatca" or "moiloaida" or "moida") return AllSkinTypes;

            var match = SkinTypes.FirstOrDefault(t => Fold(t.Code) == key || Fold(t.Label) == key);
            if (match is not null && !found.Contains(match.Code)) found.Add(match.Code);
        }

        return found.Count == 0 ? AllSkinTypes : string.Join(',', found);
    }

    /// <summary>True when a product's stored skin types include <paramref name="skinType"/> (or are "All").</summary>
    public static bool SuitsSkinType(string? productSkinTypes, string? skinType)
    {
        if (string.IsNullOrWhiteSpace(skinType)) return true;
        if (string.IsNullOrWhiteSpace(productSkinTypes)) return true;

        var stored = productSkinTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return stored.Any(s => s.Equals(AllSkinTypes, StringComparison.OrdinalIgnoreCase)
                            || s.Equals(skinType, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Lower-cases, strips accents and drops everything that is not a letter or digit.</summary>
    private static string Fold(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Replace('đ', 'd')
            .Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            if (char.IsLetterOrDigit(ch)) sb.Append(ch);
        }
        return sb.ToString();
    }
}
