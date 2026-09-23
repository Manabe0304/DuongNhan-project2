using System.Text.Json;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Dtos.Skin;

namespace DuongNhan.ApiService.Features.Skin;

/// <summary>
/// Maps a persisted <see cref="Diagnosis"/> to the API contract. The extra skin
/// metrics (skin type, health score) are stored inside
/// <see cref="Diagnosis.RawResponse"/> and surfaced here.
/// </summary>
internal static class DiagnosisMapping
{
    public static DiagnosisDto ToDto(Diagnosis diagnosis)
    {
        string? skinType = null;
        int? skinHealthScore = null;

        if (!string.IsNullOrWhiteSpace(diagnosis.RawResponse))
        {
            try
            {
                using var document = JsonDocument.Parse(diagnosis.RawResponse);

                if (document.RootElement.TryGetProperty("skinType", out var typeProperty))
                    skinType = typeProperty.GetString();

                if (document.RootElement.TryGetProperty("skinHealthScore", out var scoreProperty))
                    skinHealthScore = scoreProperty.GetInt32();
            }
            catch (JsonException)
            {
                // Older rows may not carry the extra metrics; fall back to defaults.
            }
        }

        var conditions = diagnosis.Conditions
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

        return new DiagnosisDto(
            Id: diagnosis.Id,
            SkinImageId: diagnosis.SkinImageId,
            PrimaryCondition: diagnosis.PrimaryCondition,
            Severity: diagnosis.Severity,
            Confidence: diagnosis.Confidence,
            Summary: diagnosis.Summary,
            SkinType: skinType ?? "Da hỗn hợp",
            SkinHealthScore: skinHealthScore ?? 75,
            DiagnosedAt: diagnosis.DiagnosedAt,
            Conditions: conditions,
            ImageUrl: $"/api/skin/{diagnosis.SkinImageId}");
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
