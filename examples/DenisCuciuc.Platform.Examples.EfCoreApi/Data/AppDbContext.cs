using Microsoft.EntityFrameworkCore;

namespace DenisCuciuc.Platform.Examples.EfCoreApi.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Products.Product> Products => Set<Products.Product>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Products.Product>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(2000).IsRequired();
            entity.Property(e => e.Price).HasPrecision(18, 2);
            entity.HasIndex(e => e.Name).IsUnique();
            entity.Ignore(e => e.DomainEvents);
            entity.Ignore(e => e.HasDomainEvents);
        });
    }
}
