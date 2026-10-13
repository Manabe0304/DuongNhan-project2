using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using DuongNhan.ApiService.Services;
using DuongNhan.Shared.Contracts;
using DuongNhan.Shared.Dtos.Users;
using FastEndpoints;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using AuthPolicies = DuongNhan.Shared.Contracts.Policies;

namespace DuongNhan.ApiService.Features.Users.ChangePassword;

internal sealed class ChangePasswordEndpoint(
    AppDbContext db,
    IPasswordHasher<User> passwordHasher,
    ITokenInvalidationCache tokenCache,
    IAuditLogger auditLogger,
    TimeProvider clock) : Endpoint<ChangePasswordRequest>
{
    private const int MinLength = 12;

    public override void Configure()
    {
        Put(ApiRoutes.Users.ChangePassword);
        Policies(AuthPolicies.RequireUser);

        Summary(s =>
        {
            s.Summary = "Change the authenticated user's password";
            s.Description = "Requires the current password. Every other session is signed out; the caller must sign in again.";
            s.Responses[204] = "Password changed.";
            s.Responses[400] = "Wrong current password or the new password does not meet the policy.";
        });
    }

    public override async Task HandleAsync(ChangePasswordRequest req, CancellationToken ct)
    {
        if (!Guid.TryParse(User.FindFirst(AppClaimTypes.UserId)?.Value, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var next = req.NewPassword ?? string.Empty;
        if (next.Length < MinLength || !next.Any(char.IsUpper) || !next.Any(char.IsLower) || !next.Any(char.IsDigit))
            AddError($"Mật khẩu mới cần tối thiểu {MinLength} ký tự, gồm chữ hoa, chữ thường và số.");
        if (next == req.CurrentPassword)
            AddError("Mật khẩu mới phải khác mật khẩu hiện tại.");
        ThrowIfAnyErrors();

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        var verified = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, req.CurrentPassword ?? string.Empty);
        if (verified == PasswordVerificationResult.Failed)
        {
            AddError("Mật khẩu hiện tại không đúng.");
            ThrowIfAnyErrors();
        }

        var now = clock.GetUtcNow();
        user.PasswordHash = passwordHasher.HashPassword(user, next);
        user.TokensInvalidatedAt = now;

        var refreshTokens = await db.RefreshTokens
            .Where(t => t.UserId == user.Id && t.RevokedAt == null)
            .ToListAsync(ct);
        foreach (var token in refreshTokens) token.RevokedAt = now;

        await db.SaveChangesAsync(ct);
        tokenCache.Set(user.Id, new UserTokenState(true, now));

        await auditLogger.LogAsync(
            userId: user.Id,
            action: "user.password_changed",
            entityType: "User",
            entityId: user.Id,
            ip: HttpContext.Connection.RemoteIpAddress?.ToString(),
            userAgent: HttpContext.Request.Headers.UserAgent.ToString(),
            metadata: null,
            ct);

        await Send.NoContentAsync(ct);
    }
}
