using DuongNhan.ApiService.Models;
using Microsoft.EntityFrameworkCore;

namespace DuongNhan.ApiService.Data;

internal sealed class AppDbContext(
    DbContextOptions<AppDbContext> options,
    TimeProvider timeProvider) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<SkinImage> SkinImages => Set<SkinImage>();
    public DbSet<Diagnosis> Diagnoses => Set<Diagnosis>();
    public DbSet<DiagnosisCondition> DiagnosisConditions => Set<DiagnosisCondition>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Plan> Plans => Set<Plan>();
    public DbSet<ProductRecommendation> ProductRecommendations => Set<ProductRecommendation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is IAuditable auditable)
            {
                if (entry.State == EntityState.Added)
                    auditable.CreatedAt = now;
                else if (entry.State == EntityState.Modified)
                    auditable.UpdatedAt = now;
            }
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}