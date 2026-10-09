using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Admin;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Admin.Users;

public sealed record UpdateUserStatusCommand
{
    public Guid Id { get; init; }
    public string Status { get; init; } = string.Empty;
}

internal sealed class UpdateUserStatusEndpoint(
    AppDbContext db,
    ITokenInvalidationCache tokenCache,
    IAuditLogger auditLogger,
    TimeProvider clock) : Endpoint<UpdateUserStatusCommand, AdminUserDto>
{
    private static readonly string[] Allowed = ["active", "suspended"];

    public override void Configure()
    {
        Put(ApiRoutes.Admin.UserStatus);
        Policies(AuthPolicies.RequireAdmin);

        Summary(s =>
        {
            s.Summary = "Suspend or re-activate a user account";
            s.Description = "Suspending revokes the user's sessions immediately. Admins cannot change their own status or another admin's.";
            s.Responses[200] = "The updated user.";
            s.Responses[400] = "Invalid status, or the account cannot be changed.";
            s.Responses[404] = "No such user.";
        });
    }

    public override async Task HandleAsync(UpdateUserStatusCommand req, CancellationToken ct)
    {
        var status = req.Status.Trim().ToLowerInvariant();
        if (!Allowed.Contains(status))
        {
            AddError("Status must be 'active' or 'suspended'.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var adminIdClaim = User.FindFirst(AppClaimTypes.UserId)?.Value;
        var parsed = Guid.TryParse(adminIdClaim, out var adminId);
        if (!parsed)
        {
            // No valid admin id in the claims; treat as anonymous/system action
            adminId = Guid.Empty;
        }

        if (adminId == req.Id)
        {
            AddError("You cannot change your own account status.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var user = await db.Users
            .Include(u => u.UserRoles).ThenInclude(r => r.Role)
            .FirstOrDefaultAsync(u => u.Id == req.Id, ct);

        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var isAdmin = user.UserRoles.Any(r => string.Equals(r.Role.Name, "Admin", StringComparison.OrdinalIgnoreCase));
        if (isAdmin)
        {
            AddError("Admin accounts cannot be suspended from the dashboard.");
            await Send.ErrorsAsync(400, ct);
            return;
        }

        var previous = user.Status;
        if (!string.Equals(previous, status, StringComparison.OrdinalIgnoreCase))
        {
            var now = clock.GetUtcNow();
            user.Status = status;

            if (status == "suspended")
            {
                // Kill existing access tokens (checked on every request) and refresh tokens.
                user.TokensInvalidatedAt = now;
                var refreshTokens = await db.RefreshTokens
                    .Where(t => t.UserId == user.Id && t.RevokedAt == null)
                    .ToListAsync(ct);
                foreach (var token in refreshTokens) token.RevokedAt = now;
            }

            await db.SaveChangesAsync(ct);

            if (status == "suspended")
                tokenCache.Set(user.Id, new UserTokenState(true, now));

            await auditLogger.LogAsync(
                userId: adminId == Guid.Empty ? null : adminId,
                action: "admin.user.status_changed",
                entityType: "User",
                entityId: user.Id,
                ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
                userAgent: HttpContext.Request.Headers.UserAgent.ToString(),
                metadata: new { from = previous, to = status },
                ct);
        }

        var scanCount = await db.SkinImages.CountAsync(s => s.UserId == user.Id, ct);

        await Send.OkAsync(new AdminUserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            user.UserRoles.Select(r => r.Role.Name).FirstOrDefault() ?? "User",
            user.Status,
            user.EmailVerifiedAt is not null,
            scanCount,
            user.CreatedAt), ct);
    }
}
