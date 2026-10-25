namespace DuongNhan.Shared.Dtos.Subscriptions;

/// <summary>
/// The plan the signed-in user is on right now. Users without a paid subscription are on the free plan
/// (<see cref="IsPaid"/> false, no dates). <see cref="Tier"/> is Free | Pro | VIP.
/// </summary>
public sealed record SubscriptionDto(
    string Code,
    string Name,
    string Tier,
    string Status,
    bool IsPaid,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt);
