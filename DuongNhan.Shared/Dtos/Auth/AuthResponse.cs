using DuongNhan.Shared.Dtos.Users;

namespace DuongNhan.Shared.Dtos.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    UserDto User
);