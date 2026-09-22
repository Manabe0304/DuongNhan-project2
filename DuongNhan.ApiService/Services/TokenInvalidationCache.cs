using DuongNhan.ApiService.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DuongNhan.ApiService.Services;

/// <summary>
/// Snapshot of the account state needed to validate an access token.
/// <see cref="Found"/> is false when the account no longer exists (e.g. deleted).
/// </summary>
internal readonly record struct UserTokenState(bool Found, DateTimeOffset InvalidatedAt);

internal interface ITokenInvalidationCache
{
    Task<UserTokenState> GetAsync(Guid userId, CancellationToken ct);
    void Set(Guid userId, UserTokenState state);
}

/// <summary>
/// Caches the per-user token invalidation timestamp so access-token validation
/// does not hit the database on every authenticated request. Revocations that
/// happen in-process are written through immediately; other instances converge
/// within <see cref="Ttl"/>.
/// </summary>
internal sealed class TokenInvalidationCache(AppDbContext db, IMemoryCache cache) : ITokenInvalidationCache
{
    private static readonly TimeSpan Ttl = TimeSpan.FromSeconds(30);
    private static readonly UserTokenState Missing = new(false, DateTimeOffset.MinValue);

    public async Task<UserTokenState> GetAsync(Guid userId, CancellationToken ct)
    {
        if (cache.TryGetValue(userId, out UserTokenState cached))
            return cached;

        var row = await db.Users
            .AsNoTracking()
            .Where(u => u.Id == userId)
            .Select(u => new { u.TokensInvalidatedAt })
            .FirstOrDefaultAsync(ct);

        var state = row is null
            ? Missing
            : new UserTokenState(true, row.TokensInvalidatedAt ?? DateTimeOffset.MinValue);

        cache.Set(userId, state, Ttl);
        return state;
    }

    public void Set(Guid userId, UserTokenState state)
        => cache.Set(userId, state, Ttl);
}
