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

    [Post(ApiRoutes.Skin.Diagnose)]
    Task<DiagnosisDto> DiagnoseAsync(Guid id, CancellationToken ct = default);

    [Get(ApiRoutes.Skin.History)]
    Task<List<DiagnosisDto>> GetHistoryAsync(CancellationToken ct = default);
}