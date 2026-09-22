using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Auth.Login;

internal sealed class LoginEndpoint(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    DummyPasswordVerifier dummyPasswordVerifier,
    IJwtTokenService jwtTokenService,
    ILoginAttemptTracker loginAttemptTracker,
    IAuditLogger auditLogger,
    UserMapper mapper,
    IConfiguration configuration,
    ILogger<LoginEndpoint> logger) : Endpoint<LoginRequest, AuthResponse>
{
    private const string ActiveStatus = "active";

    public override void Configure()
    {
        Post(ApiRoutes.Auth.Login);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Login:HitLimit", 10);
        var durationSeconds = configuration.GetValue("Throttling:Login:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Authenticate a user";
            s.Description = "Validates the supplied credentials and returns JWT access and refresh tokens.";
            s.Responses[200] = "Login succeeded.";
            s.Responses[400] = "Validation failed.";
            s.Responses[401] = "Invalid email or password.";
            s.Responses[429] = "Too many login attempts. Please wait and try again.";
        });
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailHash = AuthHashing.HashEmail(normalizedEmail);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        if (await loginAttemptTracker.IsLockedOutAsync(emailHash, ct))
        {
            LoginEndpointLogs.AccountLockedOut(logger, emailHash);

            await auditLogger.LogAsync(
                userId: null,
                action: "auth.login.locked_out",
                entityType: "User",
                entityId: null,
                ip: ip,
                userAgent: userAgent,
                metadata: new { email_hash = emailHash },
                ct);

            AddError(r => r.Email, "Too many failed attempts. Please try again later.");
            await Send.ErrorsAsync(StatusCodes.Status429TooManyRequests, cancellation: ct);
            return;
        }

        var user = await db.Users
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        PasswordVerificationResult verification;
        if (user is null)
        {
            // Spend comparable CPU time so a missing account is indistinguishable by timing.
            dummyPasswordVerifier.Verify(req.Password);
            verification = PasswordVerificationResult.Failed;
        }
        else
        {
            verification = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.Password);
        }

        if (user is null || verification == PasswordVerificationResult.Failed)
        {
            await loginAttemptTracker.RecordFailureAsync(emailHash, ct);

            await auditLogger.LogAsync(
                userId: null,
                action: "auth.login.failure",
                entityType: "User",
                entityId: null,
                ip: ip,
                userAgent: userAgent,
                metadata: new { email_hash = emailHash },
                ct);

            AddError(r => r.Email, "Invalid email or password.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            // Counted as a failure and reported identically to bad credentials
            // so a suspended account cannot be enumerated.
            await loginAttemptTracker.RecordFailureAsync(emailHash, ct);

            await auditLogger.LogAsync(
                userId: user.Id,
                action: "auth.login.account_inactive",
                entityType: "User",
                entityId: user.Id,
                ip: ip,
                userAgent: userAgent,
                metadata: null,
                ct);

            AddError(r => r.Email, "Invalid email or password.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        if (verification == PasswordVerificationResult.SuccessRehashNeeded)
            user.PasswordHash = passwordHasher.HashPassword(user, req.Password);

        await loginAttemptTracker.ResetAsync(emailHash, ct);

        // One session id per login; it stays stable across refresh-token rotation.
        var sessionId = Guid.CreateVersion7();
        var (refreshToken, refreshHash, refreshExpiresAt) = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            SessionId = sessionId,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt
        });

        await db.SaveChangesAsync(ct);

        await auditLogger.LogAsync(
            userId: user.Id,
            action: "auth.login.success",
            entityType: "User",
            entityId: user.Id,
            ip: ip,
            userAgent: userAgent,
            metadata: null,
            ct);

        LoginEndpointLogs.UserLoggedIn(logger, user.Id);

        var accessToken = jwtTokenService.CreateAccessToken(user, sessionId);

        await Send.OkAsync(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(jwtTokenService.AccessTokenLifetimeMinutes),
            User: mapper.ToDto(user)), ct);
    }
}

internal static partial class LoginEndpointLogs
{
    [LoggerMessage(
        EventId = 1201,
        Level = LogLevel.Information,
        Message = "User logged in: {UserId}")]
    public static partial void UserLoggedIn(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1202,
        Level = LogLevel.Warning,
        Message = "Login blocked for locked-out account {EmailHash}")]
    public static partial void AccountLockedOut(ILogger logger, string emailHash);
}
