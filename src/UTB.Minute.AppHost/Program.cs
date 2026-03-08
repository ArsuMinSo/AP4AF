var builder = DistributedApplication.CreateBuilder(args);

var postgres = builder.AddPostgres("postgres")
    .WithDataVolume()
    .WithPgAdmin();

var minuteDb = postgres.AddDatabase("minutedb");

var keycloak = builder.AddKeycloak("keycloak", 8180)
    .WithDataVolume();

var dbManager = builder.AddProject<Projects.UTB_Minute_DbManager>("dbmanager")
    .WithReference(minuteDb)
    .WaitFor(minuteDb);

var webApi = builder.AddProject<Projects.UTB_Minute_WebApi>("webapi")
    .WithHttpEndpoint(port: 8080, isProxied: false)
    .WithReference(minuteDb)
    .WithReference(keycloak)
    .WaitFor(minuteDb)
    .WaitFor(keycloak);

builder.AddProject<Projects.UTB_Minute_AdminClient>("adminclient")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(webApi);

builder.AddProject<Projects.UTB_Minute_CanteenClient>("canteenclient")
    .WithReference(webApi)
    .WithReference(keycloak)
    .WaitFor(webApi);

builder.Build().Run();
