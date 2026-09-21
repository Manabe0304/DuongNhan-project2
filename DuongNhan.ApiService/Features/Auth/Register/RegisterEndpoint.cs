using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DuongNhan.ApiService.Features.Auth.Register;

internal sealed class RegisterEndpoint(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    IAuditLogger auditLogger,
    UserMapper mapper,
    IConfiguration configuration,
    ILogger<RegisterEndpoint> logger) : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.Auth.Register);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:Register:HitLimit", 20);
        var durationSeconds = configuration.GetValue("Throttling:Register:DurationSeconds", 600);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Creates a user account and returns JWT access and refresh tokens.";
            s.Responses[200] = "Registration succeeded.";
            s.Responses[400] = "Validation failed.";
            s.Responses[409] = "Email already registered.";
            s.Responses[429] = "Too many registration attempts. Please wait and try again.";
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailHash = HashEmail(normalizedEmail);
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = HttpContext.Request.Headers.UserAgent.ToString();

        var existing = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = string.Empty
        };

        user.PasswordHash = passwordHasher.HashPassword(user, req.Password);

        if (existing is not null)
        {
            await auditLogger.LogAsync(
                userId: null,
                action: "auth.register.duplicate_email",
                entityType: "User",
                entityId: null,
                ip: ip,
                userAgent: userAgent,
                metadata: new { email_hash = emailHash },
                ct);

            AddError(r => r.Email, "Unable to create the account with the provided details.");
            await Send.ErrorsAsync(cancellation: ct);
            return;
        }

        await using var tx = await db.Database.BeginTransactionAsync(ct);

        try
        {
            user.DisplayName = Sanitize(req.DisplayName, 100);
            user.PhoneNumber = Sanitize(req.PhoneNumber, 30);

            db.Users.Add(user);

            var (refreshToken, refreshHash, refreshExpiresAt) = jwtTokenService.CreateRefreshToken();

            db.RefreshTokens.Add(new RefreshToken
            {
                UserId = user.Id,
                TokenHash = refreshHash,
                ExpiresAt = refreshExpiresAt
            });

            await db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            await auditLogger.LogAsync(
                userId: user.Id,
                action: "auth.register.success",
                entityType: "User",
                entityId: user.Id,
                ip: ip,
                userAgent: userAgent,
                metadata: null,
                ct);

            RegisterEndpointLogs.UserRegistered(logger, user.Id);

            var accessToken = jwtTokenService.CreateAccessToken(user);
            var lifetimeMinutes = configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 15);

            await Send.OkAsync(new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: refreshToken,
                ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(lifetimeMinutes),
                User: mapper.ToDto(user)), ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            await tx.RollbackAsync(ct);

            RegisterEndpointLogs.ConcurrentRegistrationConflict(logger, emailHash, ex);

            await auditLogger.LogAsync(
                userId: null,
                action: "auth.register.concurrent_conflict",
                entityType: "User",
                entityId: null,
                ip: ip,
                userAgent: userAgent,
                metadata: new { email_hash = emailHash },
                ct);

            AddError(r => r.Email, "Unable to create the account with the provided details.");
            await Send.ErrorsAsync(cancellation: ct);
        }
    }

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: "23505" };

    private static string? Sanitize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var cleaned = new string(value.Where(c => !char.IsControl(c)).ToArray()).Trim();
        return cleaned.Length > maxLength ? cleaned[..maxLength] : cleaned;
    }

    private static string HashEmail(string email)
        => Convert.ToHexString(
            System.Security.Cryptography.SHA256.HashData(
                System.Text.Encoding.UTF8.GetBytes(email))).ToLowerInvariant();
}

internal static partial class RegisterEndpointLogs
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Warning,
        Message = "Concurrent registration conflict for {EmailHash}")]
    public static partial void ConcurrentRegistrationConflict(
        ILogger logger, string emailHash, Exception exception);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "User registered: {UserId}")]
    public static partial void UserRegistered(
        ILogger logger, Guid userId);
}