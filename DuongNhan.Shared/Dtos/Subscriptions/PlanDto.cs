namespace DuongNhan.Shared.Dtos.Subscriptions;

public sealed record PlanDto(
    Guid Id,
    string Name,
    string Code,
    string? Description,
    decimal Price,
    string BillingCycle,
    int MaxScansPerMonth,
    IReadOnlyList<string> Features
);
