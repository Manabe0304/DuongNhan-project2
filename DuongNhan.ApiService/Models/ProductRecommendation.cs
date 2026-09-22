namespace DuongNhan.ApiService.Models;

internal sealed class ProductRecommendation
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid DiagnosisId { get; init; }
    public Guid ProductId { get; init; }
    public string Reason { get; set; } = string.Empty;
    public int MatchPercentage { get; set; } = 95;
    public int StepOrder { get; set; } = 1;

    public Diagnosis? Diagnosis { get; init; }
    public Product? Product { get; init; }
}
