using AutoLineCRM.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.SharedKernel.Data;

public class CatalogDbContext : DbContext
{
    public CatalogDbContext(DbContextOptions<CatalogDbContext> options) : base(options)
    {
    }
    public DbSet<Car> Cars => Set<Car>();
    public DbSet<CarImage> CarImages => Set<CarImage>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Car>.HasIndex(c => c.VIN).IsUnique();
        b.Entity<Car>().HasIndex(c => new { c.Brand, c.Model , c.Status});

        b.Entity<Car>()
            .HasMany(c => c.Images)
            .WithOne(i => i.Car)
            .HasForeignKey(i => i.CarId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}