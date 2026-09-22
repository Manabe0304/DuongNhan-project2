namespace DuongNhan.Shared.Dtos.Products;

public sealed record ProductRecommendationDto(
    Guid Id,
    Guid ProductId,
    ProductDto Product,
    string Reason,
    int MatchPercentage,
    int StepOrder
);