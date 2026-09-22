using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DuongNhan.ApiService.Models;
using DuongNhan.Shared.Contracts;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace DuongNhan.ApiService.Services;

internal sealed class JwtTokenService : IJwtTokenService
{
    // HS256 requires a 256-bit key; reject anything weaker at startup.
    private const int MinimumKeyBytes = 32;

    // The handler caches internal crypto providers, so a single instance is reused.
    private static readonly JsonWebTokenHandler TokenHandler = new();

    private readonly SigningCredentials _signingCredentials;
    private readonly string? _issuer;
    private readonly string? _audience;
    private readonly int _refreshTokenLifetimeDays;

    public JwtTokenService(IConfiguration configuration)
    {
        var key = configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("Jwt:Key is not configured.");

        var keyBytes = Encoding.UTF8.GetBytes(key);
        if (keyBytes.Length < MinimumKeyBytes)
        {
            throw new InvalidOperationException(
                $"Jwt:Key must be at least {MinimumKeyBytes} bytes ({MinimumKeyBytes * 8} bits) to sign HS256 tokens.");
        }

        _signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(keyBytes),
            SecurityAlgorithms.HmacSha256);

        _issuer = configuration["Jwt:Issuer"];
        _audience = configuration["Jwt:Audience"];
        AccessTokenLifetimeMinutes = configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 15);
        _refreshTokenLifetimeDays = configuration.GetValue("Jwt:RefreshTokenLifetimeDays", 30);
    }

    public int AccessTokenLifetimeMinutes { get; }

    public string CreateAccessToken(User user, Guid sessionId)
    {
        var now = DateTime.UtcNow;

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _issuer,
            Audience = _audience,
            IssuedAt = now,
            NotBefore = now,
            Expires = now.AddMinutes(AccessTokenLifetimeMinutes),
            SigningCredentials = _signingCredentials,
            Subject = new ClaimsIdentity(
            [
                new Claim(AppClaimTypes.UserId, user.Id.ToString()),
                new Claim(AppClaimTypes.Email, user.Email),
                new Claim(AppClaimTypes.Role, "User"),
                new Claim(AppClaimTypes.SessionId, sessionId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ])
        };

        return TokenHandler.CreateToken(descriptor);
    }

    public (string token, string hash, DateTimeOffset expiresAt) CreateRefreshToken()
    {
        var bytes = RandomNumberGenerator.GetBytes(64);
        var token = Convert.ToBase64String(bytes);
        var hash = HashRefreshToken(token);
        return (token, hash, DateTimeOffset.UtcNow.AddDays(_refreshTokenLifetimeDays));
    }

    public string HashRefreshToken(string token)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
