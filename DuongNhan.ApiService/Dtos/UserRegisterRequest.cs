namespace Dtos;

public sealed record UserRegisterRequest
(
    string Username,
    string Email,
    string Password,
    string? PhoneNumber
);