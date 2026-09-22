using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

internal sealed class Plan : IAuditable, ISoftDeletable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Code { get; set; }
    public string? Description { get; set; }
    public decimal Price { get; set; }
    public string BillingCycle { get; set; } = "monthly";
    public int MaxScansPerMonth { get; set; } = 10;
    public string? FeaturesJson { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
