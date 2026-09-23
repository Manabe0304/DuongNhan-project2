using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Skin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Skin.Diagnose;

public sealed record DiagnoseRequest(Guid Id);

internal sealed class DiagnoseEndpoint(
    AppDbContext db,
    IDiagnosisService diagnosisService,
    IConfiguration configuration,
    ILogger<DiagnoseEndpoint> logger) : Endpoint<DiagnoseRequest, DiagnosisDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Skin.Diagnose);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Diagnose:HitLimit", 20);
        var durationSeconds = configuration.GetValue("Throttling:Diagnose:DurationSeconds", 60);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Analyze skin image and generate diagnosis";
            s.Description = "Runs the AI skin diagnosis engine on the specified SkinImage, generates conditions, and persists the Diagnosis record.";
            s.Responses[200] = "Diagnosis generated successfully.";
            s.Responses[404] = "Skin image not found.";
        });
    }

    public override async Task HandleAsync(DiagnoseRequest req, CancellationToken ct)
    {
        var skinImage = await db.SkinImages
            .FirstOrDefaultAsync(si => si.Id == req.Id, ct);

        if (skinImage is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        // Re-use an existing diagnosis so repeated visits do not burn AI calls.
        var diagnosis = await db.Diagnoses
            .Include(d => d.Conditions)
            .FirstOrDefaultAsync(d => d.SkinImageId == skinImage.Id, ct);

        if (diagnosis is null)
        {
            diagnosis = await diagnosisService.DiagnoseAsync(skinImage, ct);
            db.Diagnoses.Add(diagnosis);
            skinImage.Status = "diagnosed";
            await db.SaveChangesAsync(ct);

            DiagnoseEndpointLogs.DiagnosisCreated(logger, diagnosis.Id, skinImage.Id);
        }

        await Send.OkAsync(DiagnosisMapping.ToDto(diagnosis), ct);
    }
}

internal static partial class DiagnoseEndpointLogs
{
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Diagnosis {DiagnosisId} created for SkinImage {ImageId}")]
    public static partial void DiagnosisCreated(ILogger logger, Guid diagnosisId, Guid imageId);
}
