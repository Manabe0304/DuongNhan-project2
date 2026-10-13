using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Skin;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Products;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Products.Recommend;

public sealed record RecommendRequest
{
    [QueryParam]
    public Guid DiagnosisId { get; init; }
}

internal sealed class RecommendEndpoint(AppDbContext db) : Endpoint<RecommendRequest, List<ProductRecommendationDto>>
{
    public override void Configure()
    {
        Get(ApiRoutes.Products.Recommend);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "Get product recommendations for diagnosis";
            s.Description = "Returns a personalized routine (one group of products per skincare step) tailored to the conditions and skin type found in the skin diagnosis.";
            s.Responses[200] = "List of product recommendations.";
        });
    }

    public override async Task HandleAsync(RecommendRequest req, CancellationToken ct)
    {
        var diagnosis = await db.Diagnoses
            .Include(d => d.Conditions)
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == req.DiagnosisId, ct);

        var detectedCodes = diagnosis?.Conditions
            .Select(c => c.ConditionCode.ToLowerInvariant())
            .ToHashSet() ?? ["healthy"];

        // The diagnosis' skin type narrows products to those suited to it. The AI answers in free text
        // ("Da hỗn hợp thiên dầu"), so map it to catalogue codes; an empty list means "do not filter".
        IReadOnlyList<string> skinTypes = diagnosis is null
            ? []
            : SkincareCatalog.InferSkinTypes(DiagnosisMapping.ToDto(diagnosis).SkinType);

        var allProducts = await db.Products
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var recommendations = new List<ProductRecommendationDto>();

        foreach (var product in allProducts)
        {
            // Skip products explicitly made for other skin types ("All" always passes).
            if (!SkincareCatalog.SuitsAnySkinType(product.SkinType, skinTypes)) continue;

            var targetList = (product.TargetConditions ?? "")
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(t => t.ToLowerInvariant())
                .ToList();

            var matches = targetList.Intersect(detectedCodes).ToList();
            var matchCount = matches.Count;

            int matchPercentage = matchCount switch
            {
                >= 2 => 96,
                1 => 90,
                _ => targetList.Contains("healthy") ? 82 : 75
            };

            var skinMatch = skinTypes.Count > 0
                && !product.SkinType.Equals(SkincareCatalog.AllSkinTypes, StringComparison.OrdinalIgnoreCase)
                && SkincareCatalog.SuitsAnySkinType(product.SkinType, skinTypes);
            if (skinMatch) matchPercentage = Math.Min(99, matchPercentage + 3);

            // An explicit routine step wins; otherwise fall back to the category's default step.
            int stepOrder = SkincareCatalog.ResolveStep(product.Step, product.Category);

            string reason = matchCount > 0
                ? $"Phù hợp tối ưu để điều trị tình trạng {string.Join(" & ", matches.Select(FormatConditionName))} và bảo vệ da toàn diện."
                : "Sản phẩm căn bản giúp duy trì độ ẩm và củng cố hàng rào bảo vệ da trong chu trình skincare.";
            if (skinMatch)
                reason += $" Dành cho {string.Join(" / ", skinTypes.Select(t => SkincareCatalog.SkinTypeLabel(t).ToLowerInvariant()))}.";

            var productDto = new ProductDto(
                Id: product.Id,
                Name: product.Name,
                Brand: product.Brand,
                Category: product.Category,
                Price: product.Price,
                ImageUrl: product.ImageUrl,
                Description: product.Description,
                TargetConditions: product.TargetConditions,
                UsageInstructions: product.UsageInstructions,
                AffiliateUrl: product.AffiliateUrl,
                SkinType: product.SkinType,
                Step: product.Step);

            recommendations.Add(new ProductRecommendationDto(
                Id: Guid.NewGuid(),
                ProductId: product.Id,
                Product: productDto,
                Reason: reason,
                MatchPercentage: matchPercentage,
                StepOrder: stepOrder));
        }

        // Return sorted by skincare routine steps, with highest match first within each step
        var result = recommendations
            .GroupBy(r => r.StepOrder)
            .OrderBy(g => g.Key)
            .SelectMany(g => g.OrderByDescending(r => r.MatchPercentage).Take(3))
            .ToList();

        await Send.OkAsync(result, ct);
    }

    private static string FormatConditionName(string code) => code switch
    {
        "acne" => "Mụn trứng cá",
        "enlargedpores" => "Lỗ chân lông to",
        "hyperpigmentation" => "Thâm mụn & Sắc tố",
        "rosacea" => "Đỏ da & Giãn mao mạch",
        "eczema" => "Da khô & Nhạy cảm",
        "melasma" => "Sạm nám da",
        "seborrheicdermatitis" => "Viêm da tiết bã",
        "contactdermatitis" => "Kích ứng da",
        "psoriasis" => "Vảy nến",
        "fungalinfection" => "Nấm da",
        "healthy" => "Da khỏe mạnh",
        _ => code
    };
}
