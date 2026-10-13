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
);
