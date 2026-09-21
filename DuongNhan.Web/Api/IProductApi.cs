using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Products;
using Refit;

namespace DuongNhan.Web.Api;

public interface IProductApi
{
    [Get(ApiRoutes.Products.List)]
    Task<List<ProductDto>> ListAsync(CancellationToken ct = default);

    [Get(ApiRoutes.Products.Recommend)]
    Task<List<ProductRecommendationDto>> RecommendAsync(Guid diagnosisId, CancellationToken ct = default);
}