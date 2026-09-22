using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Auth.Logout;

internal sealed class LogoutEndpoint(
    AppDbContext db,
    IJwtTokenService jwtTokenService,
    IAuditLogger auditLogger,
    IConfiguration configuration,
    ILogger<LogoutEndpoint> logger) : Endpoint<RefreshTokenRequest>
{
    public override void Configure()
    {
        Post(ApiRoutes.Auth.Logout);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Logout:HitLimit", 30);
        var durationSeconds = configuration.GetValue("Throttling:Logout:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Revoke a refresh token session";
            s.Description = "Revokes every refresh token in the session that the supplied token belongs to. Always responds with 204.";
            s.Responses[204] = "Logout succeeded.";
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        // Logout is idempotent: an absent or already-revoked token still succeeds.
        if (string.IsNullOrWhiteSpace(req.RefreshToken))
        {
            await Send.NoContentAsync(ct);
            return;
        }

        var tokenHash = jwtTokenService.HashRefreshToken(req.RefreshToken);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var stored = await db.RefreshTokens
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (stored is not null)
        {
            var now = DateTimeOffset.UtcNow;

            var family = await db.RefreshTokens
                .Where(rt => rt.SessionId == stored.SessionId && rt.RevokedAt == null)
                .ToListAsync(ct);

            if (family.Count > 0)
            {
                foreach (var token in family)
                    token.RevokedAt = now;

                await db.SaveChangesAsync(ct);

                await auditLogger.LogAsync(
                    userId: stored.UserId,
                    action: "auth.logout.success",
                    entityType: "User",
                    entityId: stored.UserId,
                    ip: ip,
                    userAgent: userAgent,
                    metadata: null,
                    ct);

                LogoutEndpointLogs.UserLoggedOut(logger, stored.UserId);
            }
        }

        await Send.NoContentAsync(ct);
    }
}

internal static partial class LogoutEndpointLogs
{
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Information,
        Message = "User logged out: {UserId}")]
    public static partial void UserLoggedOut(ILogger logger, Guid userId);
}
