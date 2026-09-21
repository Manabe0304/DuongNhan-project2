namespace DuongNhan.ApiService.Services;

internal interface ILoginAttemptTracker
{
    Task<bool> IsLockedOutAsync(string emailHash, CancellationToken ct);
    Task RecordFailureAsync(string emailHash, CancellationToken ct);
    Task ResetAsync(string emailHash, CancellationToken ct);
}