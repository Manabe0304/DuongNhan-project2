using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DuongNhan.ApiService.Services;

internal sealed class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public string CreateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(configuration["Jwt:Key"]
                ?? throw new InvalidOperationException("Jwt:Key is not configured.")));

        var lifetimeMinutes = configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 15);

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            Expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256),
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.UserId, user.Id.ToString()),
                new Claim(AppClaimTypes.Email, user.Email),
                new Claim(AppClaimTypes.Role, "User")
            ])
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    public (string token, string hash, DateTimeOffset expiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var hash = HashRefreshToken(token);
        var days = configuration.GetValue("Jwt:RefreshTokenLifetimeDays", 30);
        return (token, hash, DateTimeOffset.UtcNow.AddDays(days));
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}