using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Cluely.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CluelyDbContext>
{
    public CluelyDbContext CreateDbContext(string[] args)
    {
        // Build config from appsettings.json in Cluely.Api
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), @"..\Cluely.Api"))
            .AddJsonFile("appsettings.json")
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CluelyDbContext>();
        optionsBuilder.UseSqlServer(configuration.GetConnectionString("CluelyDb"));

        return new CluelyDbContext(optionsBuilder.Options);
    }
}
