using DuongNhan.Shared.Dtos.Subscriptions;

namespace DuongNhan.Web.Services;

/// <summary>
/// The signed-in user's scan allowance for the current package, shown in the top bar.
/// Pages call <see cref="RefreshAsync"/> after something changes it (e.g. a finished scan).
/// </summary>
public sealed class UsageState(DuongNhanApiService api)
{
    public UsageCounterDto? Current { get; private set; }

    public event Action? Changed;

    public async Task RefreshAsync()
    {
        try
        {
            Current = await api.GetUsageTypedAsync();
        }
        catch
        {
            // Cosmetic: keep the previous value (or none) if the API is unreachable.
        }

        Changed?.Invoke();
    }

    public void Clear()
    {
        Current = null;
        Changed?.Invoke();
    }
}
