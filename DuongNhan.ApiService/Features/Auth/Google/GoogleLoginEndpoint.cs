using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Auth.Google;

/// <summary>
/// Signs a user in (or silently creates an account) from a Google Identity Services ID token.
/// The token is verified against Google's public keys via GoogleJsonWebSignature — nothing about
/// the caller-supplied token is trusted until that validation succeeds.
/// </summary>
internal sealed class GoogleLoginEndpoint(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    UserMapper mapper,
    IConfiguration configuration,
    ILogger<GoogleLoginEndpoint> logger) : Endpoint<GoogleLoginRequest, AuthResponse>
{
    private const string ActiveStatus = "active";

    public override void Configure()
    {
        Post(ApiRoutes.Auth.Google);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Google:HitLimit", 20);
        var durationSeconds = configuration.GetValue("Throttling:Google:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Sign in (or register) with a Google ID token";
            s.Description = "Validates the ID token issued by Google Identity Services, then finds or creates the matching account.";
            s.Responses[200] = "Login succeeded.";
            s.Responses[401] = "The Google ID token failed validation.";
        });
    }

    public override async Task HandleAsync(GoogleLoginRequest req, CancellationToken ct)
    {
        var clientId = configuration["Google:ClientId"];

        // Without our own client id we cannot check the token was issued for THIS app,
        // so refuse rather than accept a token minted for any other Google client.
        if (string.IsNullOrWhiteSpace(clientId) || clientId.StartsWith("REPLACE_", StringComparison.Ordinal))
        {
            GoogleLoginEndpointLogs.NotConfigured(logger);
            AddError(r => r.IdToken, "Google sign-in is not configured on the server (Google:ClientId).");
            await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, cancellation: ct);
            return;
        }

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] };

            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            GoogleLoginEndpointLogs.InvalidToken(logger, ex);
            AddError(r => r.IdToken, "Google sign-in failed. Please try again.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or TimeoutException)
        {
            // Google's signing certificates could not be downloaded (offline, firewall, slow network).
            // Report a clean 503 instead of letting the exception escape the endpoint.
            GoogleLoginEndpointLogs.CertificateFetchFailed(logger, ex);
            AddError(r => r.IdToken, "Could not reach Google to verify the sign-in. Please try again.");
            await Send.ErrorsAsync(StatusCodes.Status503ServiceUnavailable, cancellation: ct);
            return;
        }

        if (!payload.EmailVerified)
        {
            AddError(r => r.IdToken, "This Google account's email is not verified.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        // Load roles like the password login does, otherwise an Admin signing in with Google
        // would be issued a token with the default "User" role.
        var user = await db.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        if (user is null)
        {
            user = new User
            {
                Email = normalizedEmail,
                DisplayName = string.IsNullOrWhiteSpace(payload.Name) ? normalizedEmail.Split('@')[0] : payload.Name,
                EmailVerifiedAt = DateTimeOffset.UtcNow,
                // Google-only accounts have no password of their own; store an unusable random
                // hash so PasswordHash's not-null constraint is satisfied without a schema change.
                PasswordHash = string.Empty
            };
            user.PasswordHash = passwordHasher.HashPassword(user, Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N"));

            db.Users.Add(user);
        }
        else if (user.EmailVerifiedAt is null)
        {
            // Google has already verified this address; no reason to make the user do it again.
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
        }

        if (!string.Equals(user.Status, ActiveStatus, StringComparison.OrdinalIgnoreCase))
        {
            AddError(r => r.IdToken, "This account is not active.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

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

        GoogleLoginEndpointLogs.UserSignedIn(logger, user.Id);

        var accessToken = jwtTokenService.CreateAccessToken(user, sessionId);

        await Send.OkAsync(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(jwtTokenService.AccessTokenLifetimeMinutes),
            User: mapper.ToDto(user)), ct);
    }
}

// Hand-written LoggerMessage delegates (no source generator involved), same call shape as before.
internal static class GoogleLoginEndpointLogs
{
    private static readonly Action<ILogger, Guid, Exception?> UserSignedInMessage =
        LoggerMessage.Define<Guid>(
            LogLevel.Information,
            new EventId(1301, nameof(UserSignedIn)),
            "User signed in via Google: {UserId}");

    private static readonly Action<ILogger, Exception?> InvalidTokenMessage =
        LoggerMessage.Define(
            LogLevel.Warning,
            new EventId(1302, nameof(InvalidToken)),
            "Google ID token failed validation");

    private static readonly Action<ILogger, Exception?> NotConfiguredMessage =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1303, nameof(NotConfigured)),
            "Google sign-in attempted but Google:ClientId is not set in DuongNhan.ApiService appsettings");

    private static readonly Action<ILogger, Exception?> CertificateFetchFailedMessage =
        LoggerMessage.Define(
            LogLevel.Error,
            new EventId(1304, nameof(CertificateFetchFailed)),
            "Could not download Google's signing certificates to validate the ID token");

    public static void CertificateFetchFailed(ILogger logger, Exception exception)
        => CertificateFetchFailedMessage(logger, exception);

    public static void UserSignedIn(ILogger logger, Guid userId)
        => UserSignedInMessage(logger, userId, null);

    public static void InvalidToken(ILogger logger, Exception exception)
        => InvalidTokenMessage(logger, exception);

    public static void NotConfigured(ILogger logger)
        => NotConfiguredMessage(logger, null);
}
