namespace DuongNhan.Shared.Dtos.Auth;

/// <summary>
/// DevCode is only populated when the API is running in the Development environment,
/// since this project has no real email/SMS provider wired up yet — see SendOtpEndpoint.
/// </summary>
public sealed record OtpResponse(
    bool Sent,
    string? DevCode
);
