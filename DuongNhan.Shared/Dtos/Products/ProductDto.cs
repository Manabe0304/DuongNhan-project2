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
    string? UsageInstructions
);