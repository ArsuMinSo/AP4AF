using Microsoft.EntityFrameworkCore;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.DbManager;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddNpgsqlDbContext<AppDbContext>("minutedb");

var app = builder.Build();

app.MapDefaultEndpoints();

app.MapPost("/db/reset", async (AppDbContext db) =>
{
    await db.Database.EnsureDeletedAsync();
    await db.Database.MigrateAsync();
    await DatabaseSeeder.SeedAsync(db);
    return Results.Ok("Database reset and seeded successfully.");
});

app.Run();
