using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Services;

internal sealed class LoginAttemptTracker(AppDbContext db, TimeProvider timeProvider) : ILoginAttemptTracker
{
    private const int MaxAttempts = 5;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public async Task<bool> IsLockedOutAsync(string emailHash, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var record = await db.LoginAttempts
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.EmailHash == emailHash, ct);

        if (record?.LockedUntil is null) return false;

        return record.LockedUntil > now;
    }

    public async Task RecordFailureAsync(string emailHash, CancellationToken ct)
    {
        var now = timeProvider.GetUtcNow();
        var record = await db.LoginAttempts
            .FirstOrDefaultAsync(a => a.EmailHash == emailHash, ct);

        if (record is null)
        {
            record = new LoginAttempt { EmailHash = emailHash };
            db.LoginAttempts.Add(record);
        }
        else if (record.LockedUntil is not null && record.LockedUntil <= now)
        {
            // The previous lockout has elapsed: start a fresh attempt window so the
            // next single failure does not immediately re-lock the account.
            record.FailedCount = 0;
            record.LockedUntil = null;
        }

        record.FailedCount++;
        record.LastFailedAt = now;

        if (record.FailedCount >= MaxAttempts)
            record.LockedUntil = now.Add(LockoutDuration);

        await db.SaveChangesAsync(ct);
    }

    public async Task ResetAsync(string emailHash, CancellationToken ct)
    {
        var record = await db.LoginAttempts
            .FirstOrDefaultAsync(a => a.EmailHash == emailHash, ct);

        if (record is null) return;

        record.FailedCount = 0;
        record.LockedUntil = null;
        await db.SaveChangesAsync(ct);
    }
}