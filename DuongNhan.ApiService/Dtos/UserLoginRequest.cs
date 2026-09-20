namespace DuongNhan.ApiService.Dtos;

public sealed record UserLoginRequest
(
    string Username,
    string Password
);