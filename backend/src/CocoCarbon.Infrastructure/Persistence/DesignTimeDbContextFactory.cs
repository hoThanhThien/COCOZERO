using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CocoCarbon.Infrastructure.Persistence;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CocoCarbonDbContext>
{
    public CocoCarbonDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CocoCarbonDbContext>();
        optionsBuilder.UseNpgsql(
            "Host=localhost;Port=5432;Database=cococarbon;Username=cococarbon_app;Password=design_time_only",
            npgsql => npgsql.UseNetTopologySuite());
        return new CocoCarbonDbContext(optionsBuilder.Options);
    }
}
