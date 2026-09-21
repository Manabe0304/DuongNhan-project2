using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class DiagnosisConfiguration : IEntityTypeConfiguration<Diagnosis>
{
    public void Configure(EntityTypeBuilder<Diagnosis> builder)
    {
        builder.ToTable("diagnoses");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Id).ValueGeneratedNever();
        builder.Property(d => d.PrimaryCondition).HasMaxLength(100).IsRequired();
        builder.Property(d => d.Severity).HasMaxLength(20);
        builder.Property(d => d.Confidence).HasPrecision(4, 3);
        builder.Property(d => d.Summary).HasMaxLength(2000);
        builder.Property(d => d.ModelName).HasMaxLength(100).IsRequired();
        builder.Property(d => d.ModelVersion).HasMaxLength(50).IsRequired();

        builder.HasIndex(d => new { d.UserId, d.DiagnosedAt });
        builder.HasIndex(d => d.SkinImageId).IsUnique();

        builder.HasMany(d => d.Conditions)
            .WithOne(c => c.Diagnosis)
            .HasForeignKey(c => c.DiagnosisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasQueryFilter(d => d.SkinImage!.DeletedAt == null);
    }
}