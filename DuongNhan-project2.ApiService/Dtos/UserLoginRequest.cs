namespace Dtos;

public sealed record UserLoginRequest
(
    string Username,
    string Password
);