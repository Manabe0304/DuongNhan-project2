using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Admin.Stats;

internal sealed class AdminStatsEndpoint(AppDbContext db, TimeProvider clock) : EndpointWithoutRequest<AdminStatsDto>
{
    private const int ChartDays = 14;

    public override void Configure()
    {
        Get(ApiRoutes.Admin.Stats);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Admin dashboard metrics";
            s.Description = "Aggregated user, scan, revenue and catalogue metrics for the admin dashboard.";
            s.Responses[200] = "Dashboard metrics.";
            s.Responses[403] = "Caller is not an admin.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var now = clock.GetUtcNow();
        var weekAgo = now.AddDays(-7);
        // Chart days are Vietnam days (GMT+7). Npgsql only accepts UTC offsets in queries, hence chartStartUtc.
        var vnOffset = TimeSpan.FromHours(7);
        var chartStart = new DateTimeOffset(now.ToOffset(vnOffset).Date.AddDays(-(ChartDays - 1)), vnOffset);
        var chartStartUtc = chartStart.ToUniversalTime();

        // A DbContext is not thread-safe, so these run one after another.
        var totalUsers = await db.Users.CountAsync(ct);
        var activeUsers = await db.Users.CountAsync(u => u.Status == "active", ct);
        var suspendedUsers = await db.Users.CountAsync(u => u.Status == "suspended", ct);
        var newUsers = await db.Users.CountAsync(u => u.CreatedAt >= weekAgo, ct);

        var totalScans = await db.SkinImages.CountAsync(ct);
        var scansLast7 = await db.SkinImages.CountAsync(s => s.CreatedAt >= weekAgo, ct);
        var totalDiagnoses = await db.Diagnoses.CountAsync(ct);

        var totalProducts = await db.Products.CountAsync(ct);
        var withAffiliate = await db.Products.CountAsync(p => p.AffiliateUrl != null && p.AffiliateUrl != "", ct);
        var totalPlans = await db.Plans.CountAsync(ct);

        var recentUsers = await db.Users
            .AsNoTracking()
            .OrderByDescending(u => u.CreatedAt)
            .Take(6)
            .Select(u => new AdminRecentUserDto(u.Id, u.Email, u.DisplayName, u.Status, u.CreatedAt))
            .ToListAsync(ct);

        var newestProducts = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .OrderByDescending(p => p.CreatedAt)
            .Take(6)
            .Select(p => new AdminTopProductDto(
                p.Id, p.Name, p.Brand, p.Category, p.AffiliateUrl != null && p.AffiliateUrl != ""))
            .ToListAsync(ct);

        // Scans per day (Vietnam time), zero-filled so the chart has a bar for every day.
        var scanTimes = await db.SkinImages
            .AsNoTracking()
            .Where(s => s.CreatedAt >= chartStartUtc)
            .Select(s => s.CreatedAt)
            .ToListAsync(ct);

        var byDay = scanTimes
            .GroupBy(t => DateOnly.FromDateTime(t.ToOffset(vnOffset).DateTime))
            .ToDictionary(g => g.Key, g => g.Count());

        var scansPerDay = Enumerable.Range(0, ChartDays)
            .Select(i => DateOnly.FromDateTime(chartStart.DateTime.AddDays(i)))
            .Select(d => new AdminDailyCountDto(d, byDay.GetValueOrDefault(d)))
            .ToList();

        // Revenue = money actually received for paid orders (PaidAmount, else the order amount), per Vietnam day.
        var paidOrders = db.PaymentOrders.AsNoTracking().Where(o => o.Status == PaymentOrder.Paid);
        var totalRevenue = await paidOrders.SumAsync(o => (decimal?)(o.PaidAmount ?? o.Amount), ct) ?? 0m;
        var revenueLast7 = await paidOrders
            .Where(o => o.PaidAt >= weekAgo)
            .SumAsync(o => (decimal?)(o.PaidAmount ?? o.Amount), ct) ?? 0m;
        var paidOrderCount = await paidOrders.CountAsync(ct);
        var activeSubscriptions = await db.Subscriptions
            .CountAsync(sub => sub.Status == Subscription.Active && sub.EndsAt > now, ct);

        var revenueRows = await paidOrders
            .Where(o => o.PaidAt >= chartStartUtc)
            .Select(o => new { PaidAt = o.PaidAt!.Value, Amount = o.PaidAmount ?? o.Amount })
            .ToListAsync(ct);

        var revenueByDay = revenueRows
            .GroupBy(r => DateOnly.FromDateTime(r.PaidAt.ToOffset(vnOffset).DateTime))
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Amount));

        var revenuePerDay = Enumerable.Range(0, ChartDays)
            .Select(i => DateOnly.FromDateTime(chartStart.DateTime.AddDays(i)))
            .Select(d => new AdminDailyRevenueDto(d, revenueByDay.GetValueOrDefault(d)))
            .ToList();

        var recentPayments = await (
            from o in paidOrders
            join u in db.Users.IgnoreQueryFilters([AppQueryFilters.SoftDelete]) on o.UserId equals u.Id
            orderby o.PaidAt descending
            select new AdminRecentPaymentDto(o.Id, u.Email, o.PlanName, o.PaidAmount ?? o.Amount, o.PaidAt!.Value))
            .Take(6)
            .ToListAsync(ct);

        var topConditions = await db.Diagnoses
            .AsNoTracking()
            .GroupBy(d => d.PrimaryCondition)
            .Select(g => new { Label = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync(ct);

        var catalogue = await db.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.Category, p.Step, p.SkinType })
            .ToListAsync(ct);

        var perStep = SkincareCatalog.Steps
            .Select(step => new AdminLabelCountDto(
                step.Label,
                catalogue.Count(p => SkincareCatalog.ResolveStep(p.Step, p.Category) == step.Number)))
            .ToList();

        var perSkinType = SkincareCatalog.SkinTypes
            .Select(type => new AdminLabelCountDto(
                type.Label,
                catalogue.Count(p => SkincareCatalog.SuitsSkinType(p.SkinType, type.Code))))
            .ToList();

        await Send.OkAsync(new AdminStatsDto(
            TotalUsers: totalUsers,
            ActiveUsers: activeUsers,
            TotalScans: totalScans,
            TotalProducts: totalProducts,
            ProductsWithAffiliate: withAffiliate,
            TotalDiagnoses: totalDiagnoses,
            TotalPlans: totalPlans,
            RecentUsers: recentUsers,
            TopProducts: newestProducts,
            SuspendedUsers: suspendedUsers,
            NewUsersLast7Days: newUsers,
            ScansLast7Days: scansLast7,
            ScansPerDay: scansPerDay,
            TopConditions: topConditions.Select(c => new AdminLabelCountDto(c.Label, c.Count)).ToList(),
            ProductsPerStep: perStep,
            ProductsPerSkinType: perSkinType,
            TotalRevenue: totalRevenue,
            RevenueLast7Days: revenueLast7,
            PaidOrders: paidOrderCount,
            ActiveSubscriptions: activeSubscriptions,
            RevenuePerDay: revenuePerDay,
            RecentPayments: recentPayments), ct);
    }
}
