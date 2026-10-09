using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using Refit;

namespace DuongNhan.Web.Api;

/// <summary>
/// Admin-only endpoints. Every call carries the bearer token explicitly: the HttpClientFactory handler
/// runs outside the Blazor circuit and cannot always read it from browser storage.
/// </summary>
public interface IAdminApi
{
    [Get(ApiRoutes.Admin.Stats)]
    Task<AdminStatsDto> GetStatsAsync(
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    [Get(ApiRoutes.Admin.Users)]
    Task<AdminPagedResult<AdminUserDto>> GetUsersAsync(
        [Header("Authorization")] string authorization,
        [Query] string? search,
        [Query] string? status,
        [Query] int page,
        [Query] int pageSize,
        CancellationToken ct = default);

    // Refit does not understand ASP.NET route constraints such as "{id:guid}",
    // so these use the constraint-free form of the same routes.
    [Put("/api/admin/users/{id}/status")]
    Task<AdminUserDto> UpdateUserStatusAsync(
        Guid id,
        [Body] UpdateUserStatusRequest body,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    [Get(ApiRoutes.Admin.Products)]
    Task<List<AdminProductDto>> GetProductsAsync(
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);

    [Put("/api/admin/products/{id}")]
    Task<AdminProductDto> UpdateProductAsync(
        Guid id,
        [Body] UpdateAdminProductRequest body,
        [Header("Authorization")] string authorization,
        CancellationToken ct = default);
}
