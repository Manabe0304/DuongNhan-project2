using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

/// <summary>
/// A paid period on a plan. A user is on a paid plan while they have an <see cref="Active"/> row with
/// StartsAt &lt;= now &lt; EndsAt; otherwise they fall back to the free (cheapest active) plan.
/// </summary>
internal sealed class Subscription : IAuditable
{
    public const string Active = "active";
    public const string Canceled = "canceled";

    public Guid Id { get; init; } = Guid.CreateVersion7();
    public Guid UserId { get; set; }
    public Guid PlanId { get; set; }
    public string Status { get; set; } = Active;
    public DateTimeOffset StartsAt { get; set; }
    public DateTimeOffset EndsAt { get; set; }
    public Guid? PaymentOrderId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}
