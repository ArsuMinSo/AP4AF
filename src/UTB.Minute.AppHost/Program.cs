using Microsoft.Extensions.Hosting;

var builder = DistributedApplication.CreateBuilder(args);

IResourceBuilder<PostgresServerResource> postgres;

if (builder.Environment.IsEnvironment("Testing"))
{
    postgres = builder.AddPostgres("postgres-testing");
}
else
{
    postgres = builder.AddPostgres("postgres")
                      .WithPgAdmin(pgAdmin => pgAdmin.WithLifetime(ContainerLifetime.Persistent))
                      .WithDataVolume()
                      .WithLifetime(ContainerLifetime.Persistent);
}

var minuteDb = postgres.AddDatabase("minutedb");

builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
       .WithReference(minuteDb)
       .WithHttpCommand("reset-db", "Reset Database")
       .WaitFor(minuteDb);

builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
       .WithReference(minuteDb)
       .WaitFor(minuteDb);

builder.Build().Run();
