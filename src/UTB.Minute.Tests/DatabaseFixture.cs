using Aspire.Hosting;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Json;
using UTB.Minute.Contracts;
using UTB.Minute.Db;

namespace UTB.Minute.Tests
{
    public class DatabaseFixture : IAsyncLifetime
    {
        private DistributedApplication app = null!;
        private HttpClient client = null!;
        private string? connectionString;

        public HttpClient HttpClient => client;

        public async ValueTask InitializeAsync()
        {
            var builder = await DistributedApplicationTestingBuilder.CreateAsync<Projects.UTB_Minute_AppHost>(["--environment=Testing"], TestContext.Current.CancellationToken);

            app = await builder.BuildAsync();

            await app.StartAsync(TestContext.Current.CancellationToken);

            client = app.CreateHttpClient("webapi");

            connectionString = await app.GetConnectionStringAsync("minutedb", TestContext.Current.CancellationToken);

            using (AppDbContext context = CreateContext())
            {
                await context.Database.EnsureDeletedAsync();
                await context.Database.EnsureCreatedAsync();
            }

            await app.ResourceNotifications.WaitForResourceHealthyAsync("webapi", TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            client.Dispose();
            await app.DisposeAsync();

            GC.SuppressFinalize(this);
        }

        public AppDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                    .UseNpgsql(connectionString)
                    .Options;

            return new AppDbContext(options);
        }
    }

    [CollectionDefinition("Database collection")]
    public class DatabaseCollection : ICollectionFixture<DatabaseFixture>
    {
    }
}
