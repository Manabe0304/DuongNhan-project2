using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

internal sealed class Product : IAuditable, ISoftDeletable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Name { get; set; }
    public required string Brand { get; set; }
    public required string Category { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public string? TargetConditions { get; set; }
    public string? UsageInstructions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
}
