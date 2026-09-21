using DuongNhan.ApiService.Data;

namespace DuongNhan.ApiService.Models;

internal sealed class User : IAuditable, ISoftDeletable
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public required string Email { get; set; }
    public required string PasswordHash { get; set; }
    public string? DisplayName { get; set; }
    public string? PhoneNumber { get; set; }
    public DateTimeOffset? EmailVerifiedAt { get; set; }
    public string Status { get; set; } = "active";
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public DateTimeOffset? TokensInvalidatedAt { get; set; }
    public ICollection<RefreshToken> RefreshTokens { get; init; } = [];
    public ICollection<SkinImage> SkinImages { get; init; } = [];
}