using System.Text.Json;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Enums;

namespace DuongNhan.ApiService.Services;

internal sealed class OpenAiDiagnosisService(TimeProvider timeProvider, ILogger<OpenAiDiagnosisService> logger) : IDiagnosisService
{
    public async Task<Diagnosis> DiagnoseAsync(SkinImage image, CancellationToken ct = default)
    {
        OpenAiDiagnosisServiceLogs.StartingDiagnosis(logger, image.Id);

        // Simulate AI computation latency
        await Task.Delay(900, ct);

        var now = timeProvider.GetUtcNow();
        var hashByte = image.Id.ToByteArray()[0];
        var profileVariant = hashByte % 3;

        string primaryCondition;
        string severity;
        decimal confidence;
        string skinType;
        int skinHealthScore;
        string summary;
        List<(string Code, string Name, decimal Conf, string Sev)> conditions;

        switch (profileVariant)
        {
            case 0:
                primaryCondition = "Mụn trứng cá & Bã nhờn";
                severity = "Moderate";
                confidence = 0.92m;
                skinType = "Da hỗn hợp thiên dầu";
                skinHealthScore = 68;
                summary = "Làn da có dấu hiệu tăng tiết bã nhờn mạnh tại vùng chữ T, kèm theo mụn ẩn li ti và mụn viêm rải rác ở hai bên cánh mũi và trán. Lỗ chân lông có xu hướng nở rộng và có một số vết thâm sau mụn mới xuất hiện.";
                conditions =
                [
                    ("Acne", "Mụn trứng cá (Acne vulgaris)", 0.92m, "Moderate"),
                    ("EnlargedPores", "Lỗ chân lông to vùng chữ T", 0.87m, "Moderate"),
                    ("Hyperpigmentation", "Thâm sau mụn (PIH)", 0.78m, "Mild")
                ];
                break;

            case 1:
                primaryCondition = "Ửng đỏ & Giãn mao mạch";
                severity = "Moderate";
                confidence = 0.88m;
                skinType = "Da nhạy cảm kích ứng";
                skinHealthScore = 72;
                summary = "Hàng rào bảo vệ da bị suy giảm nhẹ, xuất hiện hiện tượng ửng đỏ và mao mạch nổi rõ ở hai bên gò má. Da có phản ứng nhạy cảm với thời tiết và thiếu độ ẩm cần thiết, cần chu trình phục hồi làm dịu.";
                conditions =
                [
                    ("Rosacea", "Ửng đỏ & Giãn mao mạch", 0.88m, "Moderate"),
                    ("Eczema", "Da khô rát thiếu ẩm", 0.82m, "Mild"),
                    ("Hyperpigmentation", "Sắc tố da không đều màu", 0.74m, "Mild")
                ];
                break;

            default:
                primaryCondition = "Thâm nám & Tăng sắc tố";
                severity = "Moderate";
                confidence = 0.89m;
                skinType = "Da thường thiên khô";
                skinHealthScore = 76;
                summary = "Bề mặt da xuất hiện các đốm nâu và sạm nám nhẹ ở vùng gò má do tác động tích lũy từ tia UV. Da có độ đàn hồi khá tốt nhưng cần tăng cường hoạt chất chống oxy hóa và bảo vệ chống nắng tối đa.";
                conditions =
                [
                    ("Melasma", "Sạm nám da (Melasma)", 0.89m, "Moderate"),
                    ("Hyperpigmentation", "Tăng sắc tố do ánh nắng", 0.84m, "Moderate"),
                    ("EnlargedPores", "Lỗ chân lông vùng cánh mũi", 0.71m, "Mild")
                ];
                break;
        }

        var diagnosis = new Diagnosis
        {
            SkinImageId = image.Id,
            UserId = image.UserId,
            PrimaryCondition = primaryCondition,
            Severity = severity,
            Confidence = confidence,
            Summary = summary,
            ModelName = "openai-gpt-4o-skin-mock",
            ModelVersion = "2024-11-20",
            ProcessingTimeMs = 950,
            DiagnosedAt = now,
            CreatedAt = now,
            RawResponse = JsonSerializer.Serialize(new
            {
                skinType,
                skinHealthScore,
                variant = profileVariant
            })
        };

        var rank = 1;
        foreach (var (code, name, conf, sev) in conditions)
        {
            diagnosis.Conditions.Add(new DiagnosisCondition
            {
                DiagnosisId = diagnosis.Id,
                ConditionCode = code,
                Confidence = conf,
                Rank = rank++
            });
        }

        return diagnosis;
    }
}

internal static partial class OpenAiDiagnosisServiceLogs
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Information,
        Message = "Simulating intelligent AI diagnosis for SkinImage {ImageId}")]
    public static partial void StartingDiagnosis(ILogger logger, Guid imageId);
}
