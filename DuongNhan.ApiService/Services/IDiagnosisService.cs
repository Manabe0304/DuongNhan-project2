using DuongNhan.ApiService.Models;

namespace DuongNhan.ApiService.Services;

internal interface IDiagnosisService
{
    Task<Diagnosis> DiagnoseAsync(SkinImage image, CancellationToken ct = default);
}
