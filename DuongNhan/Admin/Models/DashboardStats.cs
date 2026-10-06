namespace DuongNhan.Admin.Models;

public class DashboardStats
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int TotalScans { get; set; }
    public int TotalProducts { get; set; }
    public int ProductsWithAffiliate { get; set; }
    public int TotalDiagnoses { get; set; }
    public int TotalPlans { get; set; }
    public List<RecentUser> RecentUsers { get; set; } = [];
    public List<TopProduct> TopProducts { get; set; } = [];
}

public class RecentUser
{
    public Guid Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public class TopProduct
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public bool HasAffiliate { get; set; }
}
