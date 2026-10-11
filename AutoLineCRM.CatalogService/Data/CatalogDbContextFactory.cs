using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AutoLineCRM.CatalogService.Data;

public class CatalogDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5433;Database=autoline;Username=postgres;Password=postgres;SearchPath=catalog");

        return new CatalogDbContext(optionsBuilder.Options);
    }
}