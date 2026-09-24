using DuongNhan.ApiService.Data;
using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
        // Two concurrent failures for the same email can both see "no row" and
        // both try to INSERT, and one loses on the unique index. Retry once
        // after clearing the tracker so the loser lands on the UPDATE path.
        for (var attempt = 0; attempt < 2; attempt++)
        {
            try
            {
                await RecordFailureCoreAsync(emailHash, ct);
                return;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex) && attempt == 0)
            {
                db.ChangeTracker.Clear();
            }
        }
    }

    private async Task RecordFailureCoreAsync(string emailHash, CancellationToken ct)
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

    private static bool IsUniqueViolation(DbUpdateException ex)
        => ex.InnerException is PostgresException { SqlState: "23505" };
}