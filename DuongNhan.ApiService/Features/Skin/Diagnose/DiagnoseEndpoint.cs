using System.Text.Json;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
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
    ILogger<DiagnoseEndpoint> logger) : Endpoint<DiagnoseRequest, DiagnosisDto>
{
    public override void Configure()
    {
        Post(ApiRoutes.Skin.Diagnose);
        AllowAnonymous();

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

        // Check if diagnosis already exists for this image
        var existing = await db.Diagnoses
            .Include(d => d.Conditions)
            .FirstOrDefaultAsync(d => d.SkinImageId == skinImage.Id, ct);

        Diagnosis diagnosis;

        if (existing is not null)
        {
            diagnosis = existing;
        }
        else
        {
            diagnosis = await diagnosisService.DiagnoseAsync(skinImage, ct);
            db.Diagnoses.Add(diagnosis);
            skinImage.Status = "diagnosed";
            await db.SaveChangesAsync(ct);
            DiagnoseEndpointLogs.DiagnosisCreated(logger, diagnosis.Id, skinImage.Id);
        }

        string? skinType = null;
        int? skinHealthScore = null;

        if (!string.IsNullOrWhiteSpace(diagnosis.RawResponse))
        {
            try
            {
                using var doc = JsonDocument.Parse(diagnosis.RawResponse);
                if (doc.RootElement.TryGetProperty("skinType", out var typeProp))
                    skinType = typeProp.GetString();
                if (doc.RootElement.TryGetProperty("skinHealthScore", out var scoreProp))
                    skinHealthScore = scoreProp.GetInt32();
            }
            catch
            {
                // Fallback gracefully if raw response cannot be parsed
            }
        }

        var conditionDtos = diagnosis.Conditions
            .OrderBy(c => c.Rank)
            .Select(c => new DiagnosisConditionDto(
                Id: c.Id,
                ConditionCode: c.ConditionCode,
                ConditionName: GetConditionVietnameseName(c.ConditionCode),
                Confidence: c.Confidence,
                Severity: diagnosis.Severity,
                Rank: c.Rank,
                Description: GetConditionDescription(c.ConditionCode)))
            .ToList();

        var response = new DiagnosisDto(
            Id: diagnosis.Id,
            SkinImageId: skinImage.Id,
            PrimaryCondition: diagnosis.PrimaryCondition,
            Severity: diagnosis.Severity,
            Confidence: diagnosis.Confidence,
            Summary: diagnosis.Summary,
            SkinType: skinType ?? "Da hỗn hợp",
            SkinHealthScore: skinHealthScore ?? 75,
            DiagnosedAt: diagnosis.DiagnosedAt,
            Conditions: conditionDtos,
            ImageUrl: $"/api/skin/{skinImage.Id}");

        await Send.OkAsync(response, ct);
    }

    private static string GetConditionVietnameseName(string code) => code switch
    {
        "Acne" => "Mụn trứng cá (Acne)",
        "EnlargedPores" => "Lỗ chân lông to",
        "Hyperpigmentation" => "Thâm mụn & Tăng sắc tố",
        "Rosacea" => "Chứng đỏ mặt & Giãn mao mạch",
        "Eczema" => "Da khô nứt nẻ / Chàm da",
        "Melasma" => "Sạm nám da mặt",
        "SeborrheicDermatitis" => "Viêm da tiết bã nhờn",
        "Healthy" => "Làn da khỏe mạnh",
        _ => code
    };

    private static string GetConditionDescription(string code) => code switch
    {
        "Acne" => "Sự tích tụ dầu thừa và tế bào chết gây bít tắc lỗ chân lông, tạo môi trường cho vi khuẩn C. acnes phát triển.",
        "EnlargedPores" => "Tuyến bã nhờn hoạt động quá mức kết hợp với giảm độ đàn hồi xung quanh nang lông.",
        "Hyperpigmentation" => "Sự tăng sinh sắc tố melanin sau tổn thương mụn hoặc do tiếp xúc với tia cực tím.",
        "Rosacea" => "Tình trạng viêm da mạn tính gây giãn mạch máu, đỏ da và cảm giác nóng rát.",
        "Eczema" => "Hàng rào biểu bì suy yếu khiến da mất nước nghiêm trọng và dễ bị kích ứng bởi môi trường.",
        "Melasma" => "Các mảng sắc tố màu nâu sẫm xuất hiện đối xứng trên trán, gò má và sống mũi do ánh nắng và nội tiết.",
        _ => "Tình trạng biểu hiện trên bề mặt da cần được chăm sóc theo phác đồ phù hợp."
    };
}

internal static partial class DiagnoseEndpointLogs
{
    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Information,
        Message = "Diagnosis {DiagnosisId} created for SkinImage {ImageId}")]
    public static partial void DiagnosisCreated(ILogger logger, Guid diagnosisId, Guid imageId);
}
