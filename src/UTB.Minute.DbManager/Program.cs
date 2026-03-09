using UTB.Minute.Db;
using UTB.Minute.DbManager;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("minutedb");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/reset-db", async (AppDbContext db) =>
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.EnsureCreatedAsync();
    await DatabaseSeeder.SeedAsync(db);
    return Results.Ok("Database reset and seeded successfully.");
});

app.Run();
