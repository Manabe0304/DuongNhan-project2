using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Services;

internal static class SubscriptionActivator
{
    /// <summary>
    /// Gives the user the plan they paid for. Buying the plan they already have extends it by one billing
    /// period; buying a different plan switches to it immediately and ends the previous one.
    /// Only stages changes on the context: the caller saves, so the order and the subscription change together.
    /// </summary>
    public static async Task ActivateAsync(
        AppDbContext db, Guid userId, Plan plan, Guid orderId, DateTimeOffset now, CancellationToken ct)
    {
        var months = string.Equals(plan.BillingCycle, "yearly", StringComparison.OrdinalIgnoreCase) ? 12 : 1;

        var running = await db.Subscriptions
            .Where(s => s.UserId == userId && s.Status == Subscription.Active && s.EndsAt > now)
            .ToListAsync(ct);

        var samePlan = running.Find(s => s.PlanId == plan.Id);
        if (samePlan is not null)
        {
            samePlan.EndsAt = samePlan.EndsAt.AddMonths(months);
            samePlan.PaymentOrderId = orderId;
            return;
        }

        foreach (var other in running)
        {
            other.Status = Subscription.Canceled;
            other.EndsAt = now;
        }

        db.Subscriptions.Add(new Subscription
        {
            UserId = userId,
            PlanId = plan.Id,
            Status = Subscription.Active,
            StartsAt = now,
            EndsAt = now.AddMonths(months),
            PaymentOrderId = orderId
        });
    }
}
