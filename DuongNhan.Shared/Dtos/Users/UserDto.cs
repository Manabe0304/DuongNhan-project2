namespace DuongNhan.Shared.Dtos.Users;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string? PhoneNumber,
    DateTimeOffset CreatedAt
);