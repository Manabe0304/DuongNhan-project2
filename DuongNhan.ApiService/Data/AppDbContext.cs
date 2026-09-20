using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<SkinImage> SkinImages => Set<SkinImage>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .HasMany(u => u.SkinImages)
            .WithOne(si => si.User)
            .HasForeignKey(si => si.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SkinImage>()
            .HasOne(si => si.Diagnosis)
            .WithOne(d => d.SkinImage)
            .HasForeignKey<Diagnosis>(d => d.SkinImageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}