using DuongNhan.Shared.Dtos.Admin;
using DuongNhan.Shared.Dtos.Products;
using DuongNhan.Web.Api;

namespace DuongNhan.Web.Services;

/// <summary>
/// UI-facing wrapper for the admin API. Attaches the admin's access token to every call;
/// the API still enforces the Admin policy, so this is a convenience, not a security boundary.
/// </summary>
public sealed class AdminApiService(IAdminApi admin, IProductApi products, TokenStorage tokens)
{
    public async Task<AdminStatsDto> GetStatsAsync(CancellationToken ct = default)
        => await admin.GetStatsAsync(await BearerAsync(), ct);

    public async Task<AdminPagedResult<AdminUserDto>> GetUsersAsync(
        string? search, string? status, int page, int pageSize, CancellationToken ct = default)
        => await admin.GetUsersAsync(await BearerAsync(), search, status, page, pageSize, ct);

    public async Task<AdminUserDto> SetUserStatusAsync(Guid id, string status, CancellationToken ct = default)
        => await admin.UpdateUserStatusAsync(id, new UpdateUserStatusRequest(status), await BearerAsync(), ct);

    public async Task<List<AdminProductDto>> GetProductsAsync(CancellationToken ct = default)
        => await admin.GetProductsAsync(await BearerAsync(), ct);

    public async Task<AdminProductDto> UpdateProductAsync(
        Guid id, string? skinType, int? step, bool? isActive, CancellationToken ct = default)
        => await admin.UpdateProductAsync(id, new UpdateAdminProductRequest(skinType, step, isActive), await BearerAsync(), ct);

    public async Task<ImportProductsResponse> ImportProductsAsync(List<ImportProductRow> rows, CancellationToken ct = default)
        => await products.ImportAsync(new ImportProductsRequest(rows), await BearerAsync(), ct);

    private async Task<string> BearerAsync()
    {
        var token = await tokens.GetAccessTokenAsync();
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Phiên đăng nhập đã hết hạn. Hãy đăng nhập lại bằng tài khoản Admin.");

        return $"Bearer {token}";
    }
}
