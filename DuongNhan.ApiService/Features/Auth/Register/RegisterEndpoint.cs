using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Mappers;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Auth;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Features.Auth.Register;

internal sealed class RegisterEndpoint(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService,
    UserMapper mapper,
    IConfiguration configuration) : Endpoint<RegisterRequest, AuthResponse>
{
    public override void Configure()
    {
        Post(ApiRoutes.Auth.Register);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Register a new user";
            s.Description = "Creates a user account and returns JWT access and refresh tokens.";
        });
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var normalizedEmail = req.Email.Trim().ToLowerInvariant();

        var exists = await db.Users
            .AsNoTracking()
            .AnyAsync(u => u.Email == normalizedEmail, ct);

        if (exists)
        {
            AddError(r => r.Email, "Email is already registered.");
            await SendErrorsAsync(cancellation: ct);
            return;
        }

        var user = new User
        {
            Email = normalizedEmail,
            PasswordHash = string.Empty
        };

        user.PasswordHash = passwordHasher.HashPassword(user, req.Password);
        user.DisplayName = req.DisplayName?.Trim();
        user.PhoneNumber = req.PhoneNumber?.Trim();

        db.Users.Add(user);

        var (refreshToken, refreshHash, refreshExpiresAt) = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = refreshHash,
            ExpiresAt = refreshExpiresAt
        });

        await db.SaveChangesAsync(ct);

        var accessToken = jwtTokenService.CreateAccessToken(user);
        var lifetimeMinutes = configuration.GetValue("Jwt:AccessTokenLifetimeMinutes", 15);

        await SendOkAsync(new AuthResponse(
            AccessToken: accessToken,
            RefreshToken: refreshToken,
            ExpiresAt: DateTimeOffset.UtcNow.AddMinutes(lifetimeMinutes),
            User: mapper.ToDto(user)), ct);
    }
}