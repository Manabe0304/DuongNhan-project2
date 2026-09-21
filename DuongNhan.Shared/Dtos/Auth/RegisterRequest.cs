namespace DuongNhan.Shared.Dtos.Auth;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string? DisplayName,
    string? PhoneNumber
);