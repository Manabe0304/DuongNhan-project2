using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Services;

/// <summary>Works out which plan a user is on right now (paid subscription, otherwise the free plan).</summary>
internal static class PlanAccess
{
    /// <summary>The user's active paid plan, or the cheapest active plan (free) when they have none.</summary>
    public static async Task<Plan?> GetEffectivePlanAsync(
        AppDbContext db, TimeProvider clock, Guid userId, CancellationToken ct)
    {
        var paid = await GetPaidPlanAsync(db, clock, userId, ct);
        if (paid is not null) return paid.Value.Plan;

        return await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .FirstOrDefaultAsync(ct);
    }

    /// <summary>The user's running paid subscription with its plan, or null on the free plan.</summary>
    public static async Task<(Subscription Subscription, Plan Plan)?> GetPaidPlanAsync(
        AppDbContext db, TimeProvider clock, Guid userId, CancellationToken ct)
    {
        var now = clock.GetUtcNow();

        var row = await (
            from s in db.Subscriptions.AsNoTracking()
            join p in db.Plans.AsNoTracking() on s.PlanId equals p.Id
            where s.UserId == userId
                  && s.Status == Subscription.Active
                  && s.StartsAt <= now
                  && s.EndsAt > now
                  && p.IsActive
            orderby p.Price descending, s.EndsAt descending
            select new { Subscription = s, Plan = p })
            .FirstOrDefaultAsync(ct);

        return row is null ? null : (row.Subscription, row.Plan);
    }

    /// <summary>Free | Pro | VIP, derived from the plan code (plan_free / plan_pro / plan_vip).</summary>
    public static string TierOf(string planCode)
        => planCode.Contains("vip", StringComparison.OrdinalIgnoreCase) ? "VIP"
         : planCode.Contains("pro", StringComparison.OrdinalIgnoreCase) ? "Pro"
         : "Free";
}
