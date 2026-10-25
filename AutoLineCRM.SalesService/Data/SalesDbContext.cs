using AutoLineCRM.SharedKernel.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.SalesService.Data;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options) { }

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<Contract> Contracts => Set<Contract>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("sales");

        // Оскільки ми використовуємо Shared Database, ігноруємо навігаційні властивості, 
        // які належать іншим мікросервісам (Client, Car), щоб не створювати дублікати таблиць.
        b.Entity<Request>().Ignore(r => r.Client);
        b.Entity<Request>().Ignore(r => r.Car);
        b.Entity<Request>().Ignore(r => r.Manager);

        b.Entity<Contract>().Ignore(c => c.Client);
        b.Entity<Contract>().Ignore(c => c.Manager);
        b.Entity<Contract>().Ignore(c => c.Request); // Уникнення циклічної залежності при створенні
    }
}