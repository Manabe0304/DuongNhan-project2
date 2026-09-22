using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Auth.Refresh;

internal sealed class RefreshEndpoint(
    AppDbContext db,
    IJwtTokenService jwtTokenService,
    ITokenInvalidationCache tokenInvalidationCache,
    IAuditLogger auditLogger,
    UserMapper mapper,
    IConfiguration configuration,
    ILogger<RefreshEndpoint> logger) : Endpoint<RefreshTokenRequest, AuthResponse>
{
    private const string ActiveStatus = "active";

    public override void Configure()
    {
        Post(ApiRoutes.Auth.Refresh);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Refresh:HitLimit", 30);
        var durationSeconds = configuration.GetValue("Throttling:Refresh:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Refresh an access token";
            s.Description = "Exchanges a valid refresh token for a new access token and a rotated refresh token.";
            s.Responses[200] = "Token refreshed.";
            s.Responses[400] = "Validation failed.";
            s.Responses[401] = "Refresh token is invalid, expired, or revoked.";
        });
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(req.RefreshToken);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var stored = await db.RefreshTokens
            .Include(rt => rt.User)
            .FirstOrDefaultAsync(rt => rt.TokenHash == tokenHash, ct);

        if (stored is null || stored.User is null)
        {
            await auditLogger.LogAsync(
                userId: null,
                action: "auth.refresh.invalid",
                entityType: "RefreshToken",
                entityId: null,
                ip: ip,
                userAgent: userAgent,
                metadata: null,
                ct);

            AddError(r => r.RefreshToken, "Refresh token is invalid or has expired.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        var user = stored.User;

        if (!stored.IsActive)
        {
            // A rotated-out token being presented again is a replay and a strong
            // theft signal (MITRE ATT&CK T1550.001 - Use Alternate Authentication
            // Material). Tear down the whole session family and invalidate every
            // access token already minted for the account. A merely expired token
            // is not treated as theft.
            if (stored.RevokedAt is not null)
            {
                var replayAt = DateTimeOffset.UtcNow;

                var family = await db.RefreshTokens
                    .Where(rt => rt.SessionId == stored.SessionId && rt.RevokedAt == null)
                    .ToListAsync(ct);

                foreach (var familyToken in family)
                    familyToken.RevokedAt = replayAt;

                user.TokensInvalidatedAt = replayAt;

                await db.SaveChangesAsync(ct);
                tokenInvalidationCache.Set(user.Id, new UserTokenState(true, replayAt));

                RefreshEndpointLogs.RefreshReuseDetected(logger, user.Id);

                await auditLogger.LogAsync(
                    userId: user.Id,
                    action: "auth.refresh.reuse_detected",
                    entityType: "User",
                    entityId: user.Id,
                    ip: ip,
                    userAgent: userAgent,
                    metadata: null,
                    ct);
            }

            AddError(r => r.RefreshToken, "Refresh token is invalid or has expired.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            await auditLogger.LogAsync(
                userId: user.Id,
                action: "auth.refresh.account_inactive",
                entityType: "User",
                entityId: user.Id,
                ip: ip,
                userAgent: userAgent,
                metadata: null,
                ct);

            AddError(r => r.RefreshToken, "This account is not active.");
            await Send.ErrorsAsync(StatusCodes.Status403Forbidden, cancellation: ct);
            return;
        }

        var issuedAt = DateTimeOffset.UtcNow;
        stored.RevokedAt = issuedAt;

        var (newToken, newHash, newExpiresAt) = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            SessionId = stored.SessionId,
            TokenHash = newHash,
            ExpiresAt = newExpiresAt
        });

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            userId: user.Id,
            action: "auth.refresh.success",
            entityType: "User",
            entityId: user.Id,
            ip: ip,
            userAgent: userAgent,
            metadata: null,
            ct);

        RefreshEndpointLogs.TokenRefreshed(logger, user.Id);

        var accessToken = jwtTokenService.CreateAccessToken(user, stored.SessionId);

        await Send.OkAsync(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: newToken,
            ExpiresAt: issuedAt.AddMinutes(jwtTokenService.AccessTokenLifetimeMinutes),
            User: mapper.ToDto(user)), ct);
    }
}

internal static partial class RefreshEndpointLogs
{
    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Information,
        Message = "Tokens refreshed for user {UserId}")]
    public static partial void TokenRefreshed(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1302,
        Level = LogLevel.Warning,
        Message = "Refresh token reuse detected for user {UserId}; revoked session and invalidated access tokens")]
    public static partial void RefreshReuseDetected(ILogger logger, Guid userId);
}
