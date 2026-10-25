using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoLineCRM.SalesService.Data;

public class SalesDbContextFactory : IDesignTimeDbContextFactory<SalesDbContext>
{
    public SalesDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<SalesDbContext>();

        
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=autoline;Username=postgres;Password=postgres;SearchPath=sales");

        return new SalesDbContext(optionsBuilder.Options);
    }
}