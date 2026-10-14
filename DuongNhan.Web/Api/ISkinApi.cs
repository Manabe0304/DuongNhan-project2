using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Skin;
using Refit;

namespace DuongNhan.Web.Api;

/// <summary>
/// The bearer token is passed explicitly (null = anonymous guest). The HttpClientFactory handler runs
/// outside the Blazor circuit and cannot read the token from browser storage, so without this header
/// every scan/history call reaches the API as the shared guest account.
/// </summary>
public interface ISkinApi
{
    [Multipart]
    [Post(ApiRoutes.Skin.Upload)]
    Task<UploadSkinResponse> UploadAsync(
        [AliasAs("file")] StreamPart file,
        [Header("Authorization")] string? authorization = null,
        CancellationToken ct = default);

    // Refit does not understand ASP.NET route constraints such as "{id:guid}",
    // so the client uses the constraint-free form of the same route.
    [Post("/api/skin/{id}/diagnose")]
    Task<DiagnosisDto> DiagnoseAsync(
        [AliasAs("id")] Guid id,
        [Header("Authorization")] string? authorization = null,
        CancellationToken ct = default);

    [Get(ApiRoutes.Skin.History)]
    Task<List<DiagnosisDto>> GetHistoryAsync(
        [Header("Authorization")] string? authorization = null,
        CancellationToken ct = default);
}
