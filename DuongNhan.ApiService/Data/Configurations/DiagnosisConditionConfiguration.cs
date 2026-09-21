using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class DiagnosisConditionConfiguration : IEntityTypeConfiguration<DiagnosisCondition>
{
    public void Configure(EntityTypeBuilder<DiagnosisCondition> builder)
    {
        builder.ToTable("diagnosis_conditions");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id).ValueGeneratedNever();
        builder.Property(c => c.ConditionCode).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Confidence).HasPrecision(4, 3);

        builder.HasIndex(c => new { c.DiagnosisId, c.Rank });

        // Match the Diagnosis filter so the graph is filtered consistently.
        builder.HasQueryFilter(c => c.Diagnosis!.SkinImage!.DeletedAt == null);
    }
}