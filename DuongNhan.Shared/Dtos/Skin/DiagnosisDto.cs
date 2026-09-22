namespace DuongNhan.Shared.Dtos.Skin;

public sealed record DiagnosisDto(
    Guid Id,
    Guid SkinImageId,
    string PrimaryCondition,
    string? Severity,
    decimal Confidence,
    string? Summary,
    string? SkinType,
    int? SkinHealthScore,
    DateTimeOffset DiagnosedAt,
    List<DiagnosisConditionDto> Conditions,
    string? ImageUrl = null
);