namespace DuongNhan.ApiService.Models;

public sealed class SkinImage
{
    public int Id { get; set; }
    public required string FilePath { get; set; }
    public required string OriginalFileName { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int UserId { get; set; }
    public User? User { get; set; }
    public Diagnosis? Diagnosis { get; set; }
}