using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DuongNhan.ApiService.Data.Configurations;

internal sealed class PaymentOrderConfiguration : IEntityTypeConfiguration<PaymentOrder>
{
    public void Configure(EntityTypeBuilder<PaymentOrder> builder)
    {
        builder.ToTable("payment_orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();
        builder.Property(o => o.PlanCode).HasMaxLength(50).IsRequired();
        builder.Property(o => o.PlanName).HasMaxLength(100).IsRequired();
        builder.Property(o => o.Amount).HasPrecision(18, 2);
        builder.Property(o => o.PaidAmount).HasPrecision(18, 2);
        builder.Property(o => o.OrderCode).HasMaxLength(40).IsRequired();
        builder.Property(o => o.Status).HasMaxLength(20).IsRequired();
        builder.Property(o => o.ReferenceCode).HasMaxLength(100);

        builder.HasIndex(o => o.OrderCode).IsUnique();
        builder.HasIndex(o => o.SepayTransactionId).IsUnique();
        builder.HasIndex(o => new { o.UserId, o.Status });
        builder.HasIndex(o => o.PaidAt);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Plan>()
            .WithMany()
            .HasForeignKey(o => o.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
