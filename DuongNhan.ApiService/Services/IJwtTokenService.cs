using DuongNhan.ApiService.Models;

namespace DuongNhan.ApiService.Services;

internal interface IJwtTokenService
{
    string CreateAccessToken(User user);
    (string token, string hash, DateTimeOffset expiresAt) CreateRefreshToken();
    string HashRefreshToken(string token);
}