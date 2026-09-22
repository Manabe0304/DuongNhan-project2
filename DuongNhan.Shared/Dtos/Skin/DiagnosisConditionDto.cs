namespace DuongNhan.Shared.Dtos.Skin;

public sealed record DiagnosisConditionDto(
    Guid Id,
    string ConditionCode,
    string ConditionName,
    decimal Confidence,
    string? Severity,
    int Rank,
    string? Description = null
);