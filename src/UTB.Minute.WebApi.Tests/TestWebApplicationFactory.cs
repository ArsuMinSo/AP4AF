using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using UTB.Minute.Db;

namespace UTB.Minute.WebApi.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<WebApiMarker>
{
    private readonly string _dbName = "TestDb_" + Guid.NewGuid();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove all EF Core and Npgsql-related services to replace with InMemory
            var toRemove = services
                .Where(d => d.ServiceType.FullName != null &&
                    (d.ServiceType.FullName.Contains("EntityFramework") ||
                     d.ServiceType.FullName.Contains("DbContext") ||
                     d.ServiceType.FullName.Contains("Npgsql")))
                .ToList();
            foreach (var desc in toRemove) services.Remove(desc);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));
        });
    }

    public async Task InitializeDbAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
