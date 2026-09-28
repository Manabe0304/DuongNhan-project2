using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DuongNhan.ApiService.Features.Auth.Otp;

/// <summary>
/// Issues a one-time email-verification code.
/// <para>
/// This project has no real email/SMS provider wired up yet, so the code is only logged
/// server-side (and, out of convenience during development, echoed back in the response —
/// never in Production). Wire an actual sender here (SendGrid, SMTP, etc.) before shipping.
/// </para>
/// </summary>
internal sealed class SendOtpEndpoint(
    AppDbContext db,
    IMemoryCache cache,
    IConfiguration configuration,
    IHostEnvironment environment,
    ILogger<SendOtpEndpoint> logger) : Endpoint<SendOtpRequest, OtpResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.Auth.SendOtp);
        AllowAnonymous();

        var hitLimit = configuration.GetValue("Throttling:SendOtp:HitLimit", 5);
        var durationSeconds = configuration.GetValue("Throttling:SendOtp:DurationSeconds", 300);
        Throttle(hitLimit: hitLimit, durationSeconds: durationSeconds);

        Summary(s =>
        {
            s.Summary = "Send an email-verification OTP";
            s.Description = "Generates a 6-digit code for the given email and (in Development) returns it directly since no email provider is configured.";
            s.Responses[200] = "OTP generated.";
        });
    }

    public override async Task HandleAsync(SendOtpRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailHash = AuthHashing.HashEmail(normalizedEmail);

        // Always respond 200 regardless of whether the email is registered, so this
        // endpoint cannot be used to enumerate accounts.
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail, ct);
        if (!exists)
        {
            SendOtpEndpointLogs.SkippedUnknownEmail(logger, emailHash);
            await Send.OkAsync(new OtpResponse(Sent: true, DevCode: null), ct);
            return;
        }

        var code = Random.Shared.Next(0, 1_000_000).ToString("D6");
        var lifetimeMinutes = configuration.GetValue("Otp:LifetimeMinutes", 10);

        cache.Set(CacheKey(normalizedEmail), code, TimeSpan.FromMinutes(lifetimeMinutes));

        SendOtpEndpointLogs.CodeIssued(logger, emailHash, lifetimeMinutes);

        await Send.OkAsync(new OtpResponse(
            Sent: true,
            DevCode: environment.IsDevelopment() ? code : null), ct);
    }

    internal static string CacheKey(string normalizedEmail) => $"otp:{normalizedEmail}";
}

internal static partial class SendOtpEndpointLogs
{
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Information,
        Message = "OTP issued for {EmailHash}, valid {LifetimeMinutes} minute(s). TODO: wire a real email/SMS provider — this is currently only logged.")]
    public static partial void CodeIssued(ILogger logger, string emailHash, int lifetimeMinutes);

    [LoggerMessage(
        EventId = 1402,
        Level = LogLevel.Information,
        Message = "OTP request skipped for unregistered email {EmailHash}")]
    public static partial void SkippedUnknownEmail(ILogger logger, string emailHash);
}
