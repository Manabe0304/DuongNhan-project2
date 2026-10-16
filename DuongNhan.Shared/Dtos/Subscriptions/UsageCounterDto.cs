namespace DuongNhan.Shared.Dtos.Subscriptions;

/// <summary>Scan allowance of the current package for the current billing month.</summary>
public sealed record UsageCounterDto(
    string PlanName,
    string PlanCode,
    int ScansUsed,
    int ScansLimit,
    int ScansRemaining,
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd
)
{
    /// <summary>A negative limit means unlimited (used for the Admin account).</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsUnlimited => ScansLimit < 0;

    [System.Text.Json.Serialization.JsonIgnore]
    public string LimitLabel => IsUnlimited ? "∞" : ScansLimit.ToString();

    /// <summary>"34/15" for normal plans, just "∞" for unlimited accounts.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public string UsageLabel => IsUnlimited ? "∞" : $"{ScansUsed}/{ScansLimit}";
}
