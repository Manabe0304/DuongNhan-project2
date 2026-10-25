using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Subscriptions;
using FastEndpoints;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Subscriptions.GetCurrent;

internal sealed class GetSubscriptionEndpoint(AppDbContext db, TimeProvider clock) : EndpointWithoutRequest<SubscriptionDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Subscriptions.Current);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "The plan the current user is on";
            s.Description = "The running paid plan with its dates, or the free plan when there is none.";
            s.Responses[200] = "Current plan.";
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

        var paid = await PlanAccess.GetPaidPlanAsync(db, clock, userId, ct);
        if (paid is { } running)
        {
            await Send.OkAsync(new SubscriptionDto(
                running.Plan.Code,
                running.Plan.Name,
                PlanAccess.TierOf(running.Plan.Code),
                running.Subscription.Status,
                IsPaid: true,
                running.Subscription.StartsAt,
                running.Subscription.EndsAt), ct);
            return;
        }

        var free = await PlanAccess.GetEffectivePlanAsync(db, clock, userId, ct);
        await Send.OkAsync(new SubscriptionDto(
            free?.Code ?? "plan_free",
            free?.Name ?? "Free",
            "Free",
            "active",
            IsPaid: false,
            StartsAt: null,
            EndsAt: null), ct);
    }
}
