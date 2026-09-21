namespace DuongNhan.ApiService.Models;

internal sealed class AuditLog
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid? UserId { get; set; }
    public required string Action { get; set; }
    public required string EntityType { get; set; }
    public Guid? EntityId { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? Metadata { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}