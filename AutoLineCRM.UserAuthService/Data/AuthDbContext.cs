using AutoLineCRM.SharedKernel.Entities;
using AutoLineCRM.SharedKernel.Enums;
using Microsoft.EntityFrameworkCore;

namespace AutoLineCRM.UserAuthService.Data;

public class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.HasDefaultSchema("auth");
        b.Entity<User>()
            .HasDiscriminator<UserRole>("Role")
            .HasValue<Client>(UserRole.Client)
            .HasValue<Manager>(UserRole.Manager)
            .HasValue<Administrator>(UserRole.Administrator);
        b.Entity<User>().HasIndex(u => u.Login).IsUnique();
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
