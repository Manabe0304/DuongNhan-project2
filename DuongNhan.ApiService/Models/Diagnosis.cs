namespace DuongNhan.ApiService.Models;

public sealed class Diagnosis
{
    public int Id { get; set; }
    public required string Condition { get; set; }
    public double Confidence { get; set; }
    public string? RawResponse { get; set; }
    public DateTime DiagnosedAt { get; set; } = DateTime.UtcNow;
    public int SkinImageId { get; set; }
    public SkinImage? SkinImage { get; set; }
}