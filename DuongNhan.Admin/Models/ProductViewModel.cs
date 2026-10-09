namespace DuongNhan.Admin.Models;

public class ProductViewModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? AffiliateUrl { get; set; }
    /// <summary>Comma-separated skin types ("Oily,Combination") or "All".</summary>
    public string? SkinType { get; set; }
    /// <summary>Skincare routine step 1-6 (cleanse, treat, serum, moisturize, protect, extra).</summary>
    public int? Step { get; set; }
    public bool IsActive { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

public class ProductEditModel
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? AffiliateUrl { get; set; }
    public bool IsActive { get; set; } = true;
}
