using System.Text.Json;
using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Services;

internal sealed class AuditLogger(AppDbContext db, TimeProvider timeProvider) : IAuditLogger
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task LogAsync(
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? ip,
        string? userAgent,
        object? metadata,
        CancellationToken ct)
    {
        db.AuditLogs.Add(new Models.AuditLog
        {
            UserId = userId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            IpAddress = ip,
            UserAgent = userAgent,
            Metadata = metadata is null ? null : JsonSerializer.Serialize(metadata, JsonOptions),
            CreatedAt = timeProvider.GetUtcNow()
        });

        await db.SaveChangesAsync(ct);
    }
}