using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Subscriptions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Subscriptions.Usage;

/// <summary>
/// Scans used vs. allowed for the current user's package this calendar month (UTC).
/// There is no subscription table yet, so every account is on the entry-level (cheapest active) plan;
/// when paid subscriptions exist, only the plan lookup below needs to change.
/// </summary>
internal sealed class GetUsageEndpoint(AppDbContext db, TimeProvider clock) : EndpointWithoutRequest<UsageCounterDto>
{
    private const string FallbackName = "Free";
    private const string FallbackCode = "plan_free";
    private const int FallbackLimit = 5;

    public override void Configure()
    {
        Get(ApiRoutes.Subscriptions.Usage);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Scan usage for the current package";
            s.Description = "Returns the plan name, the monthly scan limit and how many scans the user has completed this month.";
            s.Responses[200] = "Current usage.";
            s.Responses[401] = "Missing or invalid access token.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var plan = await db.Plans
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .FirstOrDefaultAsync(ct);

        var now = clock.GetUtcNow();
        var start = new DateTimeOffset(now.Year, now.Month, 1, 0, 0, 0, TimeSpan.Zero);
        var end = start.AddMonths(1);

        var used = await db.Diagnoses
            .AsNoTracking()
            .CountAsync(d => d.UserId == userId && d.DiagnosedAt >= start && d.DiagnosedAt < end, ct);

        var limit = plan?.MaxScansPerMonth ?? FallbackLimit;

        await Send.OkAsync(new UsageCounterDto(
            PlanName: plan?.Name ?? FallbackName,
            PlanCode: plan?.Code ?? FallbackCode,
            ScansUsed: used,
            ScansLimit: limit,
            ScansRemaining: Math.Max(0, limit - used),
            PeriodStart: start,
            PeriodEnd: end), ct);
    }
}
