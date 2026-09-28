using System.Text.Json;
using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Subscriptions;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Subscriptions.GetPlans;

internal sealed class GetPlansEndpoint(AppDbContext db) : EndpointWithoutRequest<List<PlanDto>>
{
    public override void Configure()
    {
        Get(ApiRoutes.Subscriptions.Plans);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "List available subscription plans";
            s.Description = "Returns every active plan, cheapest first. Public so the pricing page works before sign-in.";
            s.Responses[200] = "List of plans.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var plans = await db.Plans
            .Where(p => p.IsActive)
            .OrderBy(p => p.Price)
            .AsNoTracking()
            .ToListAsync(ct);

        var dtos = plans
            .Select(p => new PlanDto(
                p.Id,
                p.Name,
                p.Code,
                p.Description,
                p.Price,
                p.BillingCycle,
                p.MaxScansPerMonth,
                ParseFeatures(p.FeaturesJson)))
            .ToList();

        await Send.OkAsync(dtos, ct);
    }

    private static List<string> ParseFeatures(string? featuresJson)
    {
        if (string.IsNullOrWhiteSpace(featuresJson)) return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(featuresJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
