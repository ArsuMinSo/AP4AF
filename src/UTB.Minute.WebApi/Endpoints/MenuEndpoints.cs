using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.WebApi.Endpoints;

public static class MenuEndpoints
{
    public static IEndpointRouteBuilder MapMenuEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/menu").WithTags("Menu");

        group.MapGet("/", GetAllMenuItems);
        group.MapGet("/today", GetTodayMenu);
        group.MapGet("/{id:int}", GetMenuItemById);
        group.MapPost("/", CreateMenuItem);
        group.MapPut("/{id:int}", UpdateMenuItem);
        group.MapDelete("/{id:int}", DeleteMenuItem);

        return routes;
    }

    private static FoodDto ToFoodDto(Food f) =>
        new(f.Id, f.Name, f.Description, f.Price, f.IsActive);

    private static MenuItemDto ToMenuItemDto(MenuItem m) =>
        new(m.Id, m.Date, ToFoodDto(m.Food), m.AvailablePortions);

    private static async Task<Ok<List<MenuItemDto>>> GetAllMenuItems(AppDbContext db)
    {
        var items = await db.MenuItems
            .Include(m => m.Food)
            .Select(m => new MenuItemDto(m.Id, m.Date, new FoodDto(m.Food.Id, m.Food.Name, m.Food.Description, m.Food.Price, m.Food.IsActive), m.AvailablePortions))
            .ToListAsync();
        return TypedResults.Ok(items);
    }

    private static async Task<Ok<List<MenuItemDto>>> GetTodayMenu(AppDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var items = await db.MenuItems
            .Include(m => m.Food)
            .Where(m => m.Date == today)
            .Select(m => new MenuItemDto(m.Id, m.Date, new FoodDto(m.Food.Id, m.Food.Name, m.Food.Description, m.Food.Price, m.Food.IsActive), m.AvailablePortions))
            .ToListAsync();
        return TypedResults.Ok(items);
    }

    private static async Task<Results<Ok<MenuItemDto>, NotFound>> GetMenuItemById(int id, AppDbContext db)
    {
        var item = await db.MenuItems.Include(m => m.Food).FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return TypedResults.NotFound();
        return TypedResults.Ok(ToMenuItemDto(item));
    }

    private static async Task<Results<Created<MenuItemDto>, NotFound>> CreateMenuItem(CreateMenuItemDto dto, AppDbContext db)
    {
        var food = await db.Foods.FindAsync(dto.FoodId);
        if (food is null) return TypedResults.NotFound();

        var item = new MenuItem
        {
            Date = dto.Date,
            FoodId = dto.FoodId,
            Food = food,
            AvailablePortions = dto.AvailablePortions
        };
        db.MenuItems.Add(item);
        await db.SaveChangesAsync();
        return TypedResults.Created($"/menu/{item.Id}", ToMenuItemDto(item));
    }

    private static async Task<Results<Ok<MenuItemDto>, NotFound>> UpdateMenuItem(int id, UpdateMenuItemDto dto, AppDbContext db)
    {
        var item = await db.MenuItems.Include(m => m.Food).FirstOrDefaultAsync(m => m.Id == id);
        if (item is null) return TypedResults.NotFound();

        var food = await db.Foods.FindAsync(dto.FoodId);
        if (food is null) return TypedResults.NotFound();

        item.Date = dto.Date;
        item.FoodId = dto.FoodId;
        item.Food = food;
        item.AvailablePortions = dto.AvailablePortions;
        await db.SaveChangesAsync();
        return TypedResults.Ok(ToMenuItemDto(item));
    }

    private static async Task<Results<NoContent, NotFound>> DeleteMenuItem(int id, AppDbContext db)
    {
        var item = await db.MenuItems.FindAsync(id);
        if (item is null) return TypedResults.NotFound();

        db.MenuItems.Remove(item);
        await db.SaveChangesAsync();
        return TypedResults.NoContent();
    }
}
