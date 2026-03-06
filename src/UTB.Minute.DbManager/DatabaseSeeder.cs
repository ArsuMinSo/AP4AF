using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.DbManager;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext db)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var svickova = new Food { Name = "Svíčková na smetaně", Description = "Beef sirloin with cream sauce and bread dumplings", Price = 115m, IsActive = true };
        var rizek = new Food { Name = "Vídeňský řízek", Description = "Viennese schnitzel with potato salad", Price = 129m, IsActive = true };
        var gulasch = new Food { Name = "Guláš", Description = "Beef goulash with bread dumplings", Price = 109m, IsActive = true };
        var polevka = new Food { Name = "Polévka dne", Description = "Soup of the day", Price = 35m, IsActive = true };
        var vegetarian = new Food { Name = "Smažený sýr", Description = "Fried cheese with tartare sauce and fries", Price = 99m, IsActive = true };
        var inactive = new Food { Name = "Staré jídlo", Description = "Inactive food item", Price = 89m, IsActive = false };

        db.Foods.AddRange(svickova, rizek, gulasch, polevka, vegetarian, inactive);
        await db.SaveChangesAsync();

        var menuItems = new List<MenuItem>
        {
            new() { Date = today, Food = svickova, FoodId = svickova.Id, AvailablePortions = 10 },
            new() { Date = today, Food = rizek, FoodId = rizek.Id, AvailablePortions = 5 },
            new() { Date = today, Food = gulasch, FoodId = gulasch.Id, AvailablePortions = 0 },
            new() { Date = today, Food = polevka, FoodId = polevka.Id, AvailablePortions = 20 },
            new() { Date = today.AddDays(1), Food = svickova, FoodId = svickova.Id, AvailablePortions = 15 },
            new() { Date = today.AddDays(1), Food = vegetarian, FoodId = vegetarian.Id, AvailablePortions = 8 },
            new() { Date = today.AddDays(-1), Food = rizek, FoodId = rizek.Id, AvailablePortions = 0 },
        };
        db.MenuItems.AddRange(menuItems);
        await db.SaveChangesAsync();

        var todayItems = menuItems.Where(m => m.Date == today).ToList();

        var orders = new List<Order>
        {
            new() { MenuItem = todayItems[0], MenuItemId = todayItems[0].Id, CreatedAt = DateTime.UtcNow.AddMinutes(-30), Status = OrderStatus.Preparing },
            new() { MenuItem = todayItems[0], MenuItemId = todayItems[0].Id, CreatedAt = DateTime.UtcNow.AddMinutes(-25), Status = OrderStatus.Ready },
            new() { MenuItem = todayItems[1], MenuItemId = todayItems[1].Id, CreatedAt = DateTime.UtcNow.AddMinutes(-20), Status = OrderStatus.Cancelled },
            new() { MenuItem = todayItems[3], MenuItemId = todayItems[3].Id, CreatedAt = DateTime.UtcNow.AddMinutes(-10), Status = OrderStatus.Completed },
        };
        db.Orders.AddRange(orders);
        await db.SaveChangesAsync();
    }
}
