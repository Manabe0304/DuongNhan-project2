using System.Security.Claims;
using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Services;

/// <summary>
/// Enforces the monthly scan allowance on the server (same rules as the usage counter shown in the UI).
/// Admin accounts are never limited.
/// </summary>
internal static class ScanQuota
{
    private const int FallbackLimit = 15;

    /// <summary>Vietnam time (GMT+7, no daylight saving). The scan month starts at 00:00 Vietnam time.</summary>
    public static readonly TimeSpan VnOffset = TimeSpan.FromHours(7);

    /// <summary>
    /// Start (inclusive) and end (exclusive) of the current scan month in Vietnam time. Npgsql only accepts
    /// UTC offsets in queries, so callers compare with <c>.ToUniversalTime()</c> of these values.
    /// </summary>
    public static (DateTimeOffset Start, DateTimeOffset End) CurrentMonth(TimeProvider clock)
    {
        var now = clock.GetUtcNow().ToOffset(VnOffset);
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, VnOffset);
        return (start, start.AddMonths(1));
    }

    public static bool IsAdmin(ClaimsPrincipal user)
        => user.IsInRole("Admin")
           || string.Equals(user.FindFirst(AppClaimTypes.Role)?.Value, "Admin", StringComparison.OrdinalIgnoreCase);

    public static async Task<bool> IsExhaustedAsync(
        AppDbContext db, TimeProvider clock, ClaimsPrincipal user, Guid userId, CancellationToken ct)
    {
        if (IsAdmin(user)) return false;

        // Paid plan while a subscription is running, otherwise the free (cheapest active) plan.
        var plan = await PlanAccess.GetEffectivePlanAsync(db, clock, userId, ct);
        var limit = plan?.MaxScansPerMonth ?? FallbackLimit;

        var (start, end) = CurrentMonth(clock);
        var startUtc = start.ToUniversalTime();
        var endUtc = end.ToUniversalTime();

        var used = await db.Diagnoses
            .AsNoTracking()
            .CountAsync(d => d.UserId == userId && d.DiagnosedAt >= startUtc && d.DiagnosedAt < endUtc, ct);

        return used >= limit;
    }
}
