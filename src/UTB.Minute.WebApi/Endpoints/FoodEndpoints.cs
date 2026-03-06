using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.WebApi.Endpoints;

public static class FoodEndpoints
{
    public static IEndpointRouteBuilder MapFoodEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/foods").WithTags("Foods");

        group.MapGet("/", GetAllFoods);
        group.MapGet("/{id:int}", GetFoodById);
        group.MapPost("/", CreateFood);
        group.MapPut("/{id:int}", UpdateFood);
        group.MapPatch("/{id:int}/deactivate", DeactivateFood);

        return routes;
    }

    private static async Task<Ok<List<FoodDto>>> GetAllFoods(AppDbContext db)
    {
        var foods = await db.Foods
            .Select(f => new FoodDto(f.Id, f.Name, f.Description, f.Price, f.IsActive))
            .ToListAsync();
        return TypedResults.Ok(foods);
    }

    private static async Task<Results<Ok<FoodDto>, NotFound>> GetFoodById(int id, AppDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null) return TypedResults.NotFound();
        return TypedResults.Ok(new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive));
    }

    private static async Task<Created<FoodDto>> CreateFood(CreateFoodDto dto, AppDbContext db)
    {
        var food = new Food
        {
            Name = dto.Name,
            Description = dto.Description,
            Price = dto.Price,
            IsActive = true
        };
        db.Foods.Add(food);
        await db.SaveChangesAsync();
        var result = new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive);
        return TypedResults.Created($"/foods/{food.Id}", result);
    }

    private static async Task<Results<Ok<FoodDto>, NotFound>> UpdateFood(int id, UpdateFoodDto dto, AppDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null) return TypedResults.NotFound();

        food.Name = dto.Name;
        food.Description = dto.Description;
        food.Price = dto.Price;
        await db.SaveChangesAsync();
        return TypedResults.Ok(new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive));
    }

    private static async Task<Results<Ok<FoodDto>, NotFound>> DeactivateFood(int id, AppDbContext db)
    {
        var food = await db.Foods.FindAsync(id);
        if (food is null) return TypedResults.NotFound();

        food.IsActive = false;
        await db.SaveChangesAsync();
        return TypedResults.Ok(new FoodDto(food.Id, food.Name, food.Description, food.Price, food.IsActive));
    }
}
