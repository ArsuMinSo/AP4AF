using System.Net.Http.Json;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.Tests
{
    [Collection("Database collection")]
    public class MenuTests(DatabaseFixture fixture)
    {
        private readonly DatabaseFixture fixture = fixture;

        private async Task<Food> CreateFoodInDb(string name = "Test Food")
        {
            Food food = new() { Name = name, Description = "Test description", Price = 99m, IsActive = true };
            using var context = fixture.CreateContext();
            context.Foods.Add(food);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            return food;
        }

        [Fact]
        public async Task CreateMenuItem_WhenValid_ReturnsCreatedAndPersistsToDatabase()
        {
            // Arrange
            var food = await CreateFoodInDb("Svíčková pro menu");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var dto = new CreateMenuItemDto(today, food.Id, 10);

            // Act
            var createResponse = await fixture.HttpClient.PostAsJsonAsync("menu", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            MenuItemDto? created = await createResponse.Content.ReadFromJsonAsync<MenuItemDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(created);
            Assert.Equal(today, created.Date);
            Assert.Equal(food.Id, created.Food.Id);
            Assert.Equal(10, created.AvailablePortions);
            Assert.NotNull(createResponse.Headers.Location);
            Assert.Contains($"menu/{created.Id}", createResponse.Headers.Location.ToString());

            using var context = fixture.CreateContext();
            MenuItem? itemInDb = await context.MenuItems.FindAsync([created.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(itemInDb);
            Assert.Equal(today, itemInDb.Date);
        }

        [Fact]
        public async Task CreateMenuItem_WhenFoodNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new CreateMenuItemDto(DateOnly.FromDateTime(DateTime.UtcNow), 999999, 5);

            // Act
            var response = await fixture.HttpClient.PostAsJsonAsync("menu", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetMenuItems_ReturnsAllMenuItems()
        {
            // Arrange
            var food = await CreateFoodInDb("GetAll Menu Food");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            using (var context = fixture.CreateContext())
            {
                context.MenuItems.Add(new MenuItem { Date = today, FoodId = food.Id, Food = food, AvailablePortions = 5 });
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync("menu", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            MenuItemDto[]? items = await response.Content.ReadFromJsonAsync<MenuItemDto[]>(TestContext.Current.CancellationToken);
            Assert.NotNull(items);
            Assert.True(items.Length >= 1);
        }

        [Fact]
        public async Task GetMenuItemById_WhenExists_ReturnsOkAndExpectedItem()
        {
            // Arrange
            var food = await CreateFoodInDb("GetById Menu Food");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            MenuItem menuItem = new() { Date = today, FoodId = food.Id, Food = food, AvailablePortions = 8 };

            using (var context = fixture.CreateContext())
            {
                context.MenuItems.Add(menuItem);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync($"menu/{menuItem.Id}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            MenuItemDto? item = await response.Content.ReadFromJsonAsync<MenuItemDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(item);
            Assert.Equal(menuItem.Id, item.Id);
            Assert.Equal(today, item.Date);
            Assert.Equal(8, item.AvailablePortions);
        }

        [Fact]
        public async Task GetMenuItemById_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            var response = await fixture.HttpClient.GetAsync($"menu/{nonExistentId}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetTodayMenu_ReturnsOnlyTodaysItems()
        {
            // Arrange
            var food = await CreateFoodInDb("Today Menu Food");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            var yesterday = today.AddDays(-1);

            MenuItem todayItem = new() { Date = today, FoodId = food.Id, Food = food, AvailablePortions = 5 };
            MenuItem yesterdayItem = new() { Date = yesterday, FoodId = food.Id, Food = food, AvailablePortions = 3 };

            using (var context = fixture.CreateContext())
            {
                context.MenuItems.AddRange(todayItem, yesterdayItem);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync("menu/today", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            MenuItemDto[]? items = await response.Content.ReadFromJsonAsync<MenuItemDto[]>(TestContext.Current.CancellationToken);
            Assert.NotNull(items);
            Assert.Contains(items, i => i.Id == todayItem.Id);
            Assert.DoesNotContain(items, i => i.Id == yesterdayItem.Id);
        }

        [Fact]
        public async Task UpdateMenuItem_WhenExists_UpdatesItemInDatabase()
        {
            // Arrange
            var food = await CreateFoodInDb("Update Menu Food");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            MenuItem menuItem = new() { Date = today, FoodId = food.Id, Food = food, AvailablePortions = 5 };

            using (var context = fixture.CreateContext())
            {
                context.MenuItems.Add(menuItem);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var updateDto = new UpdateMenuItemDto(today.AddDays(1), food.Id, 15);

            // Act
            var updateResponse = await fixture.HttpClient.PutAsJsonAsync($"menu/{menuItem.Id}", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            using (var context = fixture.CreateContext())
            {
                MenuItem? itemInDb = await context.MenuItems.FindAsync([menuItem.Id], TestContext.Current.CancellationToken);
                Assert.NotNull(itemInDb);
                Assert.Equal(today.AddDays(1), itemInDb.Date);
                Assert.Equal(15, itemInDb.AvailablePortions);
            }
        }

        [Fact]
        public async Task DeleteMenuItem_WhenExists_RemovesFromDatabase()
        {
            // Arrange
            var food = await CreateFoodInDb("Delete Menu Food");
            var today = DateOnly.FromDateTime(DateTime.UtcNow);
            MenuItem menuItem = new() { Date = today, FoodId = food.Id, Food = food, AvailablePortions = 5 };

            using (var context = fixture.CreateContext())
            {
                context.MenuItems.Add(menuItem);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var deleteResponse = await fixture.HttpClient.DeleteAsync($"menu/{menuItem.Id}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

            using (var context = fixture.CreateContext())
            {
                MenuItem? itemInDb = await context.MenuItems.FindAsync([menuItem.Id], TestContext.Current.CancellationToken);
                Assert.Null(itemInDb);
            }
        }

        [Fact]
        public async Task DeleteMenuItem_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            var response = await fixture.HttpClient.DeleteAsync($"menu/{nonExistentId}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
