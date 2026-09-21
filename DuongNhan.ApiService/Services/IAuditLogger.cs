namespace DuongNhan.ApiService.Services;

internal interface IAuditLogger
{
    Task LogAsync(
        Guid? userId,
        string action,
        string entityType,
        Guid? entityId,
        string? ip,
        string? userAgent,
        object? metadata,
        CancellationToken ct);
}