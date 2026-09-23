using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Skin;
using Refit;

namespace DuongNhan.Web.Api;

public interface ISkinApi
{
    [Multipart]
    [Post(ApiRoutes.Skin.Upload)]
    Task<UploadSkinResponse> UploadAsync(
        [AliasAs("file")] StreamPart file,
        CancellationToken ct = default);

    // Refit does not understand ASP.NET route constraints such as "{id:guid}",
    // so the client uses the constraint-free form of the same route.
    [Post("/api/skin/{id}/diagnose")]
    Task<DiagnosisDto> DiagnoseAsync([AliasAs("id")] Guid id, CancellationToken ct = default);

    [Get(ApiRoutes.Skin.History)]
    Task<List<DiagnosisDto>> GetHistoryAsync(CancellationToken ct = default);
}
