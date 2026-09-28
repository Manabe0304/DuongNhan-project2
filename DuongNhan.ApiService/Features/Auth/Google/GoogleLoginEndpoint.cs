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

        GoogleJsonWebSignature.Payload payload;
        try
        {
            var settings = string.IsNullOrWhiteSpace(clientId)
                ? new GoogleJsonWebSignature.ValidationSettings()
                : new GoogleJsonWebSignature.ValidationSettings { Audience = [clientId] };

            payload = await GoogleJsonWebSignature.ValidateAsync(req.IdToken, settings);
        }
        catch (InvalidJwtException ex)
        {
            GoogleLoginEndpointLogs.InvalidToken(logger, ex);
            AddError(r => r.IdToken, "Google sign-in failed. Please try again.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        if (!payload.EmailVerified)
        {
            AddError(r => r.IdToken, "This Google account's email is not verified.");
            await Send.ErrorsAsync(StatusCodes.Status401Unauthorized, cancellation: ct);
            return;
        }

        var normalizedEmail = payload.Email.Trim().ToLowerInvariant();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

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

internal static partial class GoogleLoginEndpointLogs
{
    [LoggerMessage(
        EventId = 1301,
        Level = LogLevel.Information,
        Message = "User signed in via Google: {UserId}")]
    public static partial void UserSignedIn(ILogger logger, Guid userId);

    [LoggerMessage(
        EventId = 1302,
        Level = LogLevel.Warning,
        Message = "Google ID token failed validation")]
    public static partial void InvalidToken(ILogger logger, Exception exception);
}
