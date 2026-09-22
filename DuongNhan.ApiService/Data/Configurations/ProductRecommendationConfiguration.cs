using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class ProductRecommendationConfiguration : IEntityTypeConfiguration<ProductRecommendation>
{
    public void Configure(EntityTypeBuilder<ProductRecommendation> builder)
    {
        builder.ToTable("product_recommendations");
        builder.HasKey(pr => pr.Id);

        builder.Property(pr => pr.Id).ValueGeneratedNever();
        builder.Property(pr => pr.Reason).HasMaxLength(1000);

        builder.HasOne(pr => pr.Diagnosis)
            .WithMany()
            .HasForeignKey(pr => pr.DiagnosisId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.Product)
            .WithMany()
            .HasForeignKey(pr => pr.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(pr => new { pr.DiagnosisId, pr.StepOrder });

        builder.HasQueryFilter(pr => pr.Diagnosis!.SkinImage!.DeletedAt == null);
    }
}