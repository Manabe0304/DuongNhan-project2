using DuongNhan.ApiService.Data;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Skin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Skin.History;

internal sealed class GetHistoryEndpoint(AppDbContext db) : EndpointWithoutRequest<List<DiagnosisDto>>
{
    private const int MaxItems = 50;

    public override void Configure()
    {
        Get(ApiRoutes.Skin.History);
        AllowAnonymous();

        Summary(s =>
        {
            s.Summary = "List previous skin diagnoses";
            s.Description = "Returns the most recent diagnoses for the authenticated user, or an empty list when anonymous.";
            s.Responses[200] = "Diagnoses ordered newest first.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        // History is private to the signed-in account. Anonymous scans are stored under the shared guest
        // user, so returning them here would leak every visitor's scans to everyone else.
        var claim = User.FindFirst(AppClaimTypes.UserId)?.Value;
        if (!Guid.TryParse(claim, out var userId))
        {
            await Send.OkAsync([], ct);
            return;
        }

        var diagnoses = await db.Diagnoses
            .AsNoTracking()
            .Include(d => d.Conditions)
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.DiagnosedAt)
            .Take(MaxItems)
            .ToListAsync(ct);

        var response = diagnoses.Select(DiagnosisMapping.ToDto).ToList();

        await Send.OkAsync(response, ct);
    }
}
