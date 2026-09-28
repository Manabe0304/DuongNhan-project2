using System.Net;
using System.Security.Cryptography;
using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Features.Auth.Shared;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DuongNhan.ApiService.Features.Auth.Otp;

/// <summary>
/// Emails a one-time verification code to the address the user registered with.
/// The code is never returned to the caller — it only ever travels by email.
/// </summary>
internal sealed class SendOtpEndpoint(
    AppDbContext db,
    IMemoryCache cache,
    IConfiguration configuration,
    IEmailSender emailSender,
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
            s.Summary = "Email an account-verification OTP";
            s.Description = "Generates a 6-digit code and sends it to the given email address.";
            s.Responses[200] = "Sent is true when the email was handed to the mail server.";
        });
    }

    public override async Task HandleAsync(SendOtpRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();
        var emailHash = AuthHashing.HashEmail(normalizedEmail);

        // Same response whether or not the account exists, so this can't be used to enumerate users.
        var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Email == normalizedEmail, ct);
        if (!exists)
        {
            SendOtpEndpointLogs.SkippedUnknownEmail(logger, emailHash);
            await Send.OkAsync(new OtpResponse(Sent: true), ct);
            return;
        }

        if (!emailSender.IsConfigured)
        {
            SendOtpEndpointLogs.SmtpNotConfigured(logger);
            await Send.OkAsync(new OtpResponse(Sent: false), ct);
            return;
        }

        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");
        var lifetimeMinutes = configuration.GetValue("Otp:LifetimeMinutes", 10);

        try
        {
            await emailSender.SendAsync(
                normalizedEmail,
                "Mã xác minh tài khoản Dưỡng Nhan",
                BuildBody(code, lifetimeMinutes),
                ct);
        }
        catch (Exception ex) when (ex is System.Net.Mail.SmtpException or InvalidOperationException)
        {
            SendOtpEndpointLogs.SendFailed(logger, emailHash, ex);
            await Send.OkAsync(new OtpResponse(Sent: false), ct);
            return;
        }

        // Only remember the code once the email has actually gone out.
        cache.Set(CacheKey(normalizedEmail), code, TimeSpan.FromMinutes(lifetimeMinutes));
        SendOtpEndpointLogs.CodeSent(logger, emailHash, lifetimeMinutes);

        await Send.OkAsync(new OtpResponse(Sent: true), ct);
    }

    internal static string CacheKey(string normalizedEmail) => $"otp:{normalizedEmail}";

    private static string BuildBody(string code, int lifetimeMinutes)
        => $"""
            <div style="font-family:Arial,sans-serif;max-width:420px;margin:auto">
              <h2 style="color:#14171f">Xác minh tài khoản Dưỡng Nhan</h2>
              <p>Mã xác minh của bạn là:</p>
              <p style="font-size:32px;font-weight:700;letter-spacing:8px;color:#f07c68">{WebUtility.HtmlEncode(code)}</p>
              <p>Mã có hiệu lực trong {lifetimeMinutes} phút. Nếu bạn không đăng ký tài khoản, hãy bỏ qua email này.</p>
            </div>
            """;
}

internal static partial class SendOtpEndpointLogs
{
    [LoggerMessage(
        EventId = 1401,
        Level = LogLevel.Information,
        Message = "OTP emailed to {EmailHash}, valid {LifetimeMinutes} minute(s)")]
    public static partial void CodeSent(ILogger logger, string emailHash, int lifetimeMinutes);

    [LoggerMessage(
        EventId = 1402,
        Level = LogLevel.Information,
        Message = "OTP request skipped for unregistered email {EmailHash}")]
    public static partial void SkippedUnknownEmail(ILogger logger, string emailHash);

    [LoggerMessage(
        EventId = 1405,
        Level = LogLevel.Warning,
        Message = "OTP not sent: SMTP is not configured. Set Smtp:Username and Smtp:Password (e.g. a Gmail App Password) in appsettings or user-secrets.")]
    public static partial void SmtpNotConfigured(ILogger logger);

    [LoggerMessage(
        EventId = 1406,
        Level = LogLevel.Error,
        Message = "Failed to email OTP to {EmailHash}")]
    public static partial void SendFailed(ILogger logger, string emailHash, Exception exception);
}
