namespace DuongNhan.ApiService.Models;

internal sealed class DiagnosisCondition
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid DiagnosisId { get; init; }
    public required string ConditionCode { get; set; }
    public decimal Confidence { get; set; }
    public int Rank { get; set; }

    public Diagnosis? Diagnosis { get; init; }
}