namespace DuongNhan.Shared.Contracts;

/// <summary>
/// One step of the skincare routine. <see cref="Number"/> is only the stored sort key (products."Step");
/// the UI shows <see cref="Icon"/> (a Phosphor icon class) and the illustration /images/step-{Code}.svg instead of a number.
/// </summary>
public sealed record SkincareStepInfo(int Number, string Code, string Label, string Subtitle, string When, string Icon);

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
    public const int MaxStep = 7;

    public static readonly IReadOnlyList<SkincareStepInfo> Steps =
    [
        new(1, "remove",     "Tẩy trang",       "Nước tẩy trang, dầu tẩy trang, sáp tẩy trang",  "Buổi tối",        "ph-eraser"),
        new(2, "cleanse",    "Rửa mặt",         "Sữa rửa mặt, gel rửa mặt",                      "Sáng & tối",      "ph-hand-soap"),
        new(3, "toner",      "Toner",           "Toner, nước cân bằng, BHA/AHA dạng lỏng",       "Sáng & tối",      "ph-drop"),
        new(4, "serum",      "Serum",           "Serum, essence, tinh chất đặc trị",             "Sáng & tối",      "ph-eyedropper"),
        new(5, "moisturize", "Dưỡng ẩm",        "Kem dưỡng, gel dưỡng phục hồi",                 "Sáng & tối",      "ph-jar"),
        new(6, "protect",    "Chống nắng",      "Kem chống nắng phổ rộng",                       "Buổi sáng",       "ph-sun"),
        new(7, "extra",      "Chăm sóc thêm",   "Mặt nạ, chấm mụn và sản phẩm bổ trợ",           "Theo nhu cầu",    "ph-flower-lotus"),
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
    public static int StepForCategory(string? category) => Fold(category ?? string.Empty) switch
    {
        "makeupremover" or "makeupremoval" or "remover" or "cleansingoil" or "cleansingbalm"
            or "micellar" or "micellarwater" or "taytrang" => 1,
        "cleanser" or "cleanse" or "cleansing" => 2,
        "toner" or "exfoliant" => 3,
        "serum" or "essence" or "ampoule" => 4,
        "moisturizer" or "moisturiser" or "cream" => 5,
        "sunscreen" => 6,
        _ => 7
    };

    /// <summary>
    /// The routine step to use for a product: its stored step when valid (1-7), otherwise the category default.
    /// The products."Step" column is NOT NULL with a default of 0, so 0 means "not set".
    /// </summary>
    public static int ResolveStep(int? step, string? category)
        => step is >= MinStep and <= MaxStep ? step.Value : StepForCategory(category);

    /// <summary>
    /// Parses a step from a number ("4") or any English/Vietnamese name ("Serum", "Tẩy trang", "makeup removal",
    /// "additional care"). Returns null for empty or unrecognised input.
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

        return key switch
        {
            "makeupremoval" or "makeupremover" or "remover" or "cleansingoil" or "cleansingbalm"
                or "micellar" or "micellarwater" => 1,
            "cleanser" or "cleansing" or "lamsach" => 2,
            "exfoliant" or "canbangda" or "nuoccanbang" => 3,
            "essence" or "ampoule" or "tinhchat" => 4,
            "moisturizer" or "moisturiser" or "cream" or "duongam" => 5,
            "sunscreen" or "chongnang" or "spf" => 6,
            "additionalcare" or "mask" or "treatment" or "spottreatment" => 7,
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

    /// <summary>
    /// Maps the free-text skin type the AI diagnosis returns ("Da hỗn hợp thiên dầu", "Da nhạy cảm kích ứng",
    /// "Da thường thiên khô") to catalogue codes. A "leaning" type yields both codes, e.g. combination-leaning-oily
    /// gives <c>Combination</c> and <c>Oily</c>, so products for either match. Words are matched whole, so "không"
    /// or "khỏe" are not mistaken for "khô". Returns an empty list when nothing is recognised, meaning
    /// "do not filter by skin type". (<see cref="NormalizeSkinTypes"/> only understands exact names such as
    /// "Da dầu", so it falls back to "All" for these compound phrases.)
    /// </summary>
    public static IReadOnlyList<string> InferSkinTypes(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return [];

        var padded = " " + FoldWords(text) + " ";
        var codes = new List<string>();
        if (HasAny(padded, " nhay cam ", " sensitive ")) codes.Add("Sensitive");
        if (HasAny(padded, " hon hop ", " combination ")) codes.Add("Combination");
        if (HasAny(padded, " dau ", " oily ")) codes.Add("Oily");
        if (HasAny(padded, " kho ", " dry ")) codes.Add("Dry");
        if (HasAny(padded, " thuong ", " normal ")) codes.Add("Normal");
        return codes;
    }

    /// <summary>True when the product suits at least one of <paramref name="skinTypes"/> (or there is nothing to match).</summary>
    public static bool SuitsAnySkinType(string? productSkinTypes, IReadOnlyCollection<string> skinTypes)
        => skinTypes.Count == 0 || skinTypes.Any(t => SuitsSkinType(productSkinTypes, t));

    private static bool HasAny(string padded, params string[] words) => words.Any(padded.Contains);

    /// <summary>Lower-cases, strips accents and turns everything that is not a letter or digit into a single space.</summary>
    private static string FoldWords(string value)
    {
        var decomposed = value.Trim().ToLowerInvariant().Replace('đ', 'd')
            .Normalize(System.Text.NormalizationForm.FormD);
        var sb = new System.Text.StringBuilder(decomposed.Length);
        foreach (var ch in decomposed)
        {
            if (System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch) == System.Globalization.UnicodeCategory.NonSpacingMark) continue;
            sb.Append(char.IsLetterOrDigit(ch) ? ch : ' ');
        }
        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
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
