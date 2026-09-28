namespace DuongNhan.Shared.Dtos.Auth;

public sealed record VerifyOtpRequest(
    string Email,
    string Code
);
