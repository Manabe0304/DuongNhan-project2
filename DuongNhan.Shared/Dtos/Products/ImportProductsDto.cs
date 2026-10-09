namespace DuongNhan.Shared.Dtos.Products;

public sealed record ImportProductRow(
    string Name,
    string Brand,
    string Category,
    decimal Price,
    string? ImageUrl,
    string? Description,
    string? TargetConditions,
    string? UsageInstructions,
    string AffiliateUrl,
    string? SkinType = null,
    string? Step = null
);

public sealed record ImportProductsRequest(List<ImportProductRow> Products);

public sealed record ImportProductsResponse(int Created, int Updated, int Skipped);
