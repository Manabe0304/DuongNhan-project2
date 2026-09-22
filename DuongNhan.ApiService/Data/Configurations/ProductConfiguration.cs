using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(255).IsRequired();
        builder.Property(p => p.Brand).HasMaxLength(150).IsRequired();
        builder.Property(p => p.Category).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.ImageUrl).HasMaxLength(1000);
        builder.Property(p => p.Description).HasMaxLength(2000);
        builder.Property(p => p.TargetConditions).HasMaxLength(500);
        builder.Property(p => p.UsageInstructions).HasMaxLength(1000);

        builder.HasIndex(p => p.Category);
        builder.HasIndex(p => p.Brand);

        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
