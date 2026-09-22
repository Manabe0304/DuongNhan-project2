using DuongNhan.ApiService.Models;

namespace DuongNhan.ApiService.Services;

internal interface IJwtTokenService
{
    int AccessTokenLifetimeMinutes { get; }

    string CreateAccessToken(User user, Guid sessionId);
    (string token, string hash, DateTimeOffset expiresAt) CreateRefreshToken();
    string HashRefreshToken(string token);
}
