namespace DuongNhan.Shared.Dtos.Admin;

/// <summary>
/// Dashboard metrics for the admin area. The first block of properties keeps the exact shape the
/// standalone DuongNhan.Admin MVC app already deserialises, so both admin UIs can share one endpoint.
/// </summary>
public sealed record AdminStatsDto(
    int TotalUsers,
    int ActiveUsers,
    int TotalScans,
    int TotalProducts,
    int ProductsWithAffiliate,
    int TotalDiagnoses,
    int TotalPlans,
    List<AdminRecentUserDto> RecentUsers,
    List<AdminTopProductDto> TopProducts,
    int SuspendedUsers = 0,
    int NewUsersLast7Days = 0,
    int ScansLast7Days = 0,
    List<AdminDailyCountDto>? ScansPerDay = null,
    List<AdminLabelCountDto>? TopConditions = null,
    List<AdminLabelCountDto>? ProductsPerStep = null,
    List<AdminLabelCountDto>? ProductsPerSkinType = null);

public sealed record AdminRecentUserDto(Guid Id, string Email, string? DisplayName, string Status, DateTimeOffset CreatedAt);

public sealed record AdminTopProductDto(Guid Id, string Name, string Brand, string Category, bool HasAffiliate);

public sealed record AdminDailyCountDto(DateOnly Day, int Count);

public sealed record AdminLabelCountDto(string Label, int Count);

public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    string Role,
    string Status,
    bool EmailVerified,
    int ScanCount,
    DateTimeOffset CreatedAt);

public sealed record AdminPagedResult<T>(List<T> Items, int Total, int Page, int PageSize);

public sealed record UpdateUserStatusRequest(string Status);

public sealed record AdminProductDto(
    Guid Id,
    string Name,
    string Brand,
    string Category,
    decimal Price,
    string? ImageUrl,
    string? AffiliateUrl,
    string SkinType,
    int? Step,
    bool IsActive,
    DateTimeOffset CreatedAt);

public sealed record UpdateAdminProductRequest(string? SkinType, int? Step, bool? IsActive);
