using Microsoft.EntityFrameworkCore;
using UrlShortener.Api.Models;

namespace UrlShortener.Api.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Link> Links => Set<Link>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Link>(entity =>
        {
            entity.ToTable("Links");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Code)
                .IsRequired()
                .HasMaxLength(20);

            entity.HasIndex(e => e.Code)
                .IsUnique();

            entity.Property(e => e.TargetUrl)
                .IsRequired()
                .HasMaxLength(2048);

            entity.Property(e => e.CreatedAtUtc)
                .IsRequired();

            entity.Property(e => e.ExpiresAtUtc)
                .IsRequired(false);

            entity.Property(e => e.AccessCount)
                .IsRequired()
                .HasDefaultValue(0);
        });
    }
}
