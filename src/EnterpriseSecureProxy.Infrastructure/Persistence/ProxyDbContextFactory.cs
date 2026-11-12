using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace EnterpriseSecureProxy.Infrastructure.Persistence;

/// <summary>Design-time factory for <c>dotnet ef</c> commands (migrations).</summary>
public sealed class ProxyDbContextFactory : IDesignTimeDbContextFactory<ProxyDbContext>
{
    public ProxyDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../EnterpriseSecureProxy.Api"))
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("ProxyConfiguration")
            ?? throw new InvalidOperationException("ConnectionStrings:ProxyConfiguration is not configured.");

        var optionsBuilder = new DbContextOptionsBuilder<ProxyDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new ProxyDbContext(optionsBuilder.Options);
    }
}
