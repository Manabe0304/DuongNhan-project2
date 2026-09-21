using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class SkinImageConfiguration : IEntityTypeConfiguration<SkinImage>
{
    public void Configure(EntityTypeBuilder<SkinImage> builder)
    {
        builder.ToTable("skin_images");
        builder.HasKey(si => si.Id);

        builder.Property(si => si.Id).ValueGeneratedNever();
        builder.Property(si => si.StorageKey).HasMaxLength(500).IsRequired();
        builder.Property(si => si.OriginalFileName).HasMaxLength(255).IsRequired();
        builder.Property(si => si.ContentType).HasMaxLength(100).IsRequired();
        builder.Property(si => si.Status).HasMaxLength(20).IsRequired();
        builder.Property(si => si.ChecksumSha256).HasMaxLength(64);

        builder.HasIndex(si => new { si.UserId, si.CreatedAt });
        builder.HasIndex(si => si.Status);

        builder.HasQueryFilter(si => si.DeletedAt == null);

        builder.HasOne(si => si.Diagnosis)
            .WithOne(d => d.SkinImage)
            .HasForeignKey<Diagnosis>(d => d.SkinImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}