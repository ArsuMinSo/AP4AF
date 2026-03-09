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
}

app.MapDefaultEndpoints();

app.MapFoodEndpoints();
app.MapMenuEndpoints();
app.MapOrderEndpoints();

app.Run();
