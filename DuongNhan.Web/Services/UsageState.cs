using DuongNhan.Shared.Dtos.Subscriptions;

namespace DuongNhan.Web.Services;

/// <summary>
/// The signed-in user's scan allowance for the current package, shown in the top bar.
/// Pages call <see cref="RefreshAsync"/> after something changes it (e.g. a finished scan).
/// </summary>
public sealed partial class UsageState(DuongNhanApiService api, ILogger<UsageState> logger)
{
    public UsageCounterDto? Current { get; private set; }

    public event Action? Changed;

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "Could not load the scan usage counter for the top bar.")]
    private partial void LogCouldNotLoadScanUsageCounter(Exception ex);

    public async Task RefreshAsync()
    {
        try
        {
            Current = await api.GetUsageTypedAsync();
        }
        catch (Exception ex)
        {
            // Cosmetic: keep the previous value (or none) if the API is unreachable,
            // but log it so a missing counter is diagnosable instead of failing silently.
            LogCouldNotLoadScanUsageCounter(ex);
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        Current = null;
        Changed?.Invoke();
    }
}
