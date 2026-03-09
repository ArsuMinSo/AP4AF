using System.Net.Http.Json;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.Tests
{
    [Collection("Database collection")]
    public class FoodTests(DatabaseFixture fixture)
    {
        private readonly DatabaseFixture fixture = fixture;

        [Fact]
        public async Task GetFoods_ReturnsAllSeededFoods()
        {
            // Arrange
            Food svickova = new() { Name = "Svíčková", Description = "Beef with cream sauce", Price = 115m, IsActive = true };
            Food rizek = new() { Name = "Vídeňský řízek", Description = "Viennese schnitzel", Price = 129m, IsActive = true };

            using (var context = fixture.CreateContext())
            {
                context.Foods.AddRange(svickova, rizek);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync("foods", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var foods = await response.Content.ReadFromJsonAsync<FoodDto[]>(TestContext.Current.CancellationToken);
            Assert.NotNull(foods);
            Assert.Contains(foods, f => f.Id == svickova.Id && f.Name == svickova.Name);
            Assert.Contains(foods, f => f.Id == rizek.Id && f.Name == rizek.Name);
        }

        [Fact]
        public async Task CreateFood_WhenValid_ReturnsCreatedAndPersistsToDatabase()
        {
            // Arrange
            var dto = new CreateFoodDto("Guláš", "Beef goulash with dumplings", 109m);

            // Act
            var createResponse = await fixture.HttpClient.PostAsJsonAsync("foods", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            FoodDto? created = await createResponse.Content.ReadFromJsonAsync<FoodDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(created);
            Assert.Equal(dto.Name, created.Name);
            Assert.Equal(dto.Description, created.Description);
            Assert.Equal(dto.Price, created.Price);
            Assert.True(created.IsActive);
            Assert.NotNull(createResponse.Headers.Location);
            Assert.Contains($"foods/{created.Id}", createResponse.Headers.Location.ToString());

            using var context = fixture.CreateContext();
            Food? foodInDb = await context.Foods.FindAsync([created.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(foodInDb);
            Assert.Equal(dto.Name, foodInDb.Name);
        }

        [Fact]
        public async Task GetFoodById_WhenExists_ReturnsOkAndExpectedFood()
        {
            // Arrange
            Food polevka = new() { Name = "Polévka dne", Description = "Soup of the day", Price = 35m, IsActive = true };

            using (var context = fixture.CreateContext())
            {
                context.Foods.Add(polevka);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync($"foods/{polevka.Id}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            FoodDto? food = await response.Content.ReadFromJsonAsync<FoodDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(food);
            Assert.Equal(polevka.Id, food.Id);
            Assert.Equal(polevka.Name, food.Name);
        }

        [Fact]
        public async Task GetFoodById_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            var response = await fixture.HttpClient.GetAsync($"foods/{nonExistentId}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UpdateFood_WhenExists_UpdatesFoodInDatabase()
        {
            // Arrange
            Food original = new() { Name = "Původní název", Description = "Původní popis", Price = 100m, IsActive = true };

            using (var context = fixture.CreateContext())
            {
                context.Foods.Add(original);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var updateDto = new UpdateFoodDto("Nový název", "Nový popis", 120m);

            // Act
            var updateResponse = await fixture.HttpClient.PutAsJsonAsync($"foods/{original.Id}", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            using (var context = fixture.CreateContext())
            {
                Food? foodInDb = await context.Foods.FindAsync([original.Id], TestContext.Current.CancellationToken);
                Assert.NotNull(foodInDb);
                Assert.Equal(updateDto.Name, foodInDb.Name);
                Assert.Equal(updateDto.Price, foodInDb.Price);
            }
        }

        [Fact]
        public async Task UpdateFood_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;
            var updateDto = new UpdateFoodDto("Nikdo", "Popis", 100m);

            // Act
            var response = await fixture.HttpClient.PutAsJsonAsync($"foods/{nonExistentId}", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeactivateFood_WhenExists_SetsIsActiveToFalse()
        {
            // Arrange
            Food active = new() { Name = "Aktivní jídlo", Description = "Popis", Price = 89m, IsActive = true };

            using (var context = fixture.CreateContext())
            {
                context.Foods.Add(active);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var deactivateResponse = await fixture.HttpClient.PatchAsync($"foods/{active.Id}/deactivate", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

            using (var context = fixture.CreateContext())
            {
                Food? foodInDb = await context.Foods.FindAsync([active.Id], TestContext.Current.CancellationToken);
                Assert.NotNull(foodInDb);
                Assert.False(foodInDb.IsActive);
            }
        }

        [Fact]
        public async Task DeactivateFood_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            var response = await fixture.HttpClient.PatchAsync($"foods/{nonExistentId}/deactivate", null, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
