using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db;
using UTB.Minute.WebApi.Endpoints;
using UTB.Minute.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("minutedb");

builder.Services.AddSingleton<SseService>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    if (db.Database.IsRelational())
    {
        await db.Database.MigrateAsync();
    }
    else
    {
        await db.Database.EnsureCreatedAsync();
    }
}

app.MapDefaultEndpoints();

app.MapFoodEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

app.Run();

public partial class Program { }

// WebApplicationFactory entry point
namespace UTB.Minute.WebApi
{
    public sealed class WebApiMarker { }
}
