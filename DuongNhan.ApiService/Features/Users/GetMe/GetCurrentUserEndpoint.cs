using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Users;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Users.GetMe;

internal sealed class GetCurrentUserEndpoint(AppDbContext db, UserMapper mapper) : EndpointWithoutRequest<UserDto>
{
    public override void Configure()
    {
        Get(ApiRoutes.Users.Me);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Get the authenticated user's profile";
            s.Description = "Returns the profile of the user identified by the supplied access token.";
            s.Responses[200] = "The current user.";
            s.Responses[401] = "Missing or invalid access token.";
            s.Responses[404] = "The user no longer exists.";
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var claim = User.FindFirst(AppClaimTypes.UserId)?.Value;

        if (!Guid.TryParse(claim, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(mapper.ToDto(user), ct);
    }
}
