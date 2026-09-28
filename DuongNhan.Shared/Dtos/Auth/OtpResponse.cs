namespace DuongNhan.Shared.Dtos.Auth;

/// <summary>Sent is false when the server could not deliver the email (e.g. SMTP not configured).</summary>
public sealed record OtpResponse(
    bool Sent
);
