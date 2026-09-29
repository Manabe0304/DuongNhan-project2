using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DuongNhan.ApiService.Features.Auth.Otp;

internal sealed class VerifyOtpEndpoint(
    AppDbContext db,
    IMemoryCache cache,
    IConfiguration configuration,
    ILogger<VerifyOtpEndpoint> logger) : Endpoint<VerifyOtpRequest, VerifyOtpResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.Auth.VerifyOtp);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:VerifyOtp:HitLimit", 10);
        var durationSeconds = configuration.GetValue("Throttling:VerifyOtp:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Verify an email-verification OTP";
            s.Description = "Checks the 6-digit code issued by /api/auth/send-otp and, if it matches, marks the account's email as verified.";
            s.Responses[200] = "Returns whether the code was valid.";
        });
    }

    public override async Task HandleAsync(VerifyOtpRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailHash = AuthHashing.HashEmail(normalizedEmail);

        if (string.IsNullOrWhiteSpace(req.Code)
            || !cache.TryGetValue(SendOtpEndpoint.CacheKey(normalizedEmail), out string? expectedCode)
            || string.IsNullOrWhiteSpace(expectedCode)
            || !string.Equals(expectedCode, req.Code.Trim(), StringComparison.Ordinal))
        {
            VerifyOtpEndpointLogs.Rejected(logger, emailHash);
            await Send.OkAsync(new VerifyOtpResponse(Verified: false), ct);
            return;
        }

        cache.Remove(SendOtpEndpoint.CacheKey(normalizedEmail));

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail, ct);
        if (user is not null && user.EmailVerifiedAt is null)
        {
            user.EmailVerifiedAt = DateTimeOffset.UtcNow;
            await db.SaveChangesAsync(ct);
        }

        VerifyOtpEndpointLogs.Verified(logger, emailHash);
        await Send.OkAsync(new VerifyOtpResponse(Verified: true), ct);
    }
}

internal static class VerifyOtpEndpointLogs
{
    private static readonly Action<ILogger, string, Exception?> VerifiedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Information,
            new EventId(1403, nameof(Verified)),
            "OTP verified for {EmailHash}");

    private static readonly Action<ILogger, string, Exception?> RejectedMessage =
        LoggerMessage.Define<string>(
            LogLevel.Warning,
            new EventId(1404, nameof(Rejected)),
            "OTP rejected for {EmailHash}");

    public static void Verified(ILogger logger, string emailHash)
        => VerifiedMessage(logger, emailHash, null);

    public static void Rejected(ILogger logger, string emailHash)
        => RejectedMessage(logger, emailHash, null);
}
