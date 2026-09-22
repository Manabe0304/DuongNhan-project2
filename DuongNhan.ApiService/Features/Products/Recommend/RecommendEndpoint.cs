using DuongNhan.ApiService.Data;
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
            s.Description = "Returns a personalized list of skincare products tailored to the conditions found in the skin diagnosis.";
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

        var allProducts = await db.Products
            .Where(p => p.IsActive)
            .AsNoTracking()
            .ToListAsync(ct);

        var recommendations = new List<ProductRecommendationDto>();

        foreach (var product in allProducts)
        {
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

            int stepOrder = product.Category.ToLowerInvariant() switch
            {
                "cleanser" => 1,
                "treatment" => 2,
                "exfoliant" => 2,
                "serum" => 3,
                "essence" => 3,
                "moisturizer" => 4,
                "sunscreen" => 5,
                _ => 6
            };

            string reason = matchCount > 0
                ? $"Phù hợp tối ưu để điều trị tình trạng {string.Join(" & ", matches.Select(FormatConditionName))} và bảo vệ da toàn diện."
                : $"Sản phẩm căn bản giúp duy trì độ ẩm và củng cố hàng rào bảo vệ da trong chu trình skincare.";

            var productDto = new ProductDto(
                Id: product.Id,
                Name: product.Name,
                Brand: product.Brand,
                Category: product.Category,
                Price: product.Price,
                ImageUrl: product.ImageUrl,
                Description: product.Description,
                TargetConditions: product.TargetConditions,
                UsageInstructions: product.UsageInstructions);

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
            .SelectMany(g => g.OrderByDescending(r => r.MatchPercentage).Take(2))
            .ToList();

        await Send.OkAsync(result, ct);
    }

    private static string FormatConditionName(string code) => code switch
    {
        "acne" => "Mụn trứng cá",
        "enlargedpores" => "Lỗ chân lông to",
        "hyperpigmentation" => "Thâm mụn & Sắc tố",
        "rosacea" => "Đỏ da & Giãn mao mạch",
        "eczema" => "Khô rát nhạy cảm",
        "melasma" => "Sạm nám da",
        _ => code
    };
}
