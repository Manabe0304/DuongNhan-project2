namespace DuongNhan.Shared.Dtos.Products;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Brand,
    string Category,
    decimal Price,
    string? ImageUrl,
    string? Description,
    string? TargetConditions,
    string? UsageInstructions,
    string? AffiliateUrl,
    // Comma-separated skin type codes ("Oily,Combination") or "All". See SkincareCatalog.
    string? SkinType = null,
    // Routine step 1-6 (cleanse → extra). See SkincareCatalog.Steps.
    int? Step = null
);
