namespace DuongNhan.ApiService.Models;

internal sealed class LoginAttempt
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string EmailHash { get; set; }
    public int FailedCount { get; set; }
    public DateTimeOffset? LastFailedAt { get; set; }
    public DateTimeOffset? LockedUntil { get; set; }
}