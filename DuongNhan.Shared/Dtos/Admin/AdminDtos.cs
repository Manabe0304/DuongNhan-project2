namespace DuongNhan.Shared.Dtos.Admin;

/// <summary>Dashboard metrics for the admin area (/admin in the Web app).</summary>
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
    List<AdminLabelCountDto>? ProductsPerSkinType = null,
    decimal TotalRevenue = 0,
    decimal RevenueLast7Days = 0,
    int PaidOrders = 0,
    int ActiveSubscriptions = 0,
    List<AdminDailyRevenueDto>? RevenuePerDay = null,
    List<AdminRecentPaymentDto>? RecentPayments = null);

public sealed record AdminRecentUserDto(Guid Id, string Email, string? DisplayName, string Status, DateTimeOffset CreatedAt);

public sealed record AdminTopProductDto(Guid Id, string Name, string Brand, string Category, bool HasAffiliate);

public sealed record AdminDailyCountDto(DateOnly Day, int Count);

public sealed record AdminLabelCountDto(string Label, int Count);

/// <summary>Revenue (VND) collected on one Vietnam-time day.</summary>
public sealed record AdminDailyRevenueDto(DateOnly Day, decimal Amount);

public sealed record AdminRecentPaymentDto(Guid Id, string Email, string PlanName, decimal Amount, DateTimeOffset PaidAt);

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
    DateTimeOffset CreatedAt,
    string? Description = null,
    string? TargetConditions = null,
    string? UsageInstructions = null);

/// <summary>Full product payload for create. Update uses the same shape with every field optional.</summary>
public sealed record SaveAdminProductRequest(
    string? Name,
    string? Brand,
    string? Category,
    decimal? Price,
    string? ImageUrl,
    string? Description,
    string? TargetConditions,
    string? UsageInstructions,
    string? AffiliateUrl,
    string? SkinType,
    int? Step,
    bool? IsActive);
