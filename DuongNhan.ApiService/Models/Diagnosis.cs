using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

internal sealed class Diagnosis : IAuditable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid SkinImageId { get; init; }
    public Guid UserId { get; init; }
    public required string PrimaryCondition { get; set; }
    public string? Severity { get; set; }
    public decimal Confidence { get; set; }
    public string? Summary { get; set; }
    public string? RawResponse { get; set; }
    public required string ModelName { get; set; }
    public required string ModelVersion { get; set; }
    public int? ProcessingTimeMs { get; set; }
    public DateTimeOffset DiagnosedAt { get; init; } = DateTimeOffset.UtcNow;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public SkinImage? SkinImage { get; init; }
    public ICollection<DiagnosisCondition> Conditions { get; init; } = [];
}