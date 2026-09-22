using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class PlanConfiguration : IEntityTypeConfiguration<Plan>
{
    public void Configure(EntityTypeBuilder<Plan> builder)
    {
        builder.ToTable("plans");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();
        builder.Property(p => p.Name).HasMaxLength(100).IsRequired();
        builder.Property(p => p.Code).HasMaxLength(50).IsRequired();
        builder.Property(p => p.Description).HasMaxLength(1000);
        builder.Property(p => p.Price).HasPrecision(18, 2);
        builder.Property(p => p.BillingCycle).HasMaxLength(50);
        builder.Property(p => p.FeaturesJson).HasColumnType("text");

        builder.HasIndex(p => p.Code).IsUnique();
        builder.HasQueryFilter(p => p.DeletedAt == null);
    }
}
