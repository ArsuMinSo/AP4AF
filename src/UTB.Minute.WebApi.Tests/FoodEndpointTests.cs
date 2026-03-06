using System.Net;
using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.WebApi.Tests;

public class FoodEndpointTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public FoodEndpointTests()
    {
        _factory = new TestWebApplicationFactory();
        _client = _factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.InitializeDbAsync();

    public Task DisposeAsync()
    {
        _client.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CreateFood_ReturnsCreated()
    {
        var dto = new CreateFoodDto("Test Svíčková", "Beef with cream sauce", 115m);
        var response = await _client.PostAsJsonAsync("/foods", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var food = await response.Content.ReadFromJsonAsync<FoodDto>();
        Assert.NotNull(food);
        Assert.Equal("Test Svíčková", food.Name);
        Assert.True(food.IsActive);
    }

    [Fact]
    public async Task GetFoods_ReturnsListWithCreatedFoods()
    {
        var dto = new CreateFoodDto("GetAll Test Food", "Desc", 99m);
        await _client.PostAsJsonAsync("/foods", dto);

        var foods = await _client.GetFromJsonAsync<List<FoodDto>>("/foods");
        Assert.NotNull(foods);
        Assert.Contains(foods, f => f.Name == "GetAll Test Food");
    }

    [Fact]
    public async Task GetFoodById_ReturnsCorrectFood()
    {
        var dto = new CreateFoodDto("GetById Food", "Desc", 79m);
        var createResponse = await _client.PostAsJsonAsync("/foods", dto);
        var created = await createResponse.Content.ReadFromJsonAsync<FoodDto>();

        var response = await _client.GetAsync($"/foods/{created!.Id}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var food = await response.Content.ReadFromJsonAsync<FoodDto>();
        Assert.Equal(created.Id, food!.Id);
    }

    [Fact]
    public async Task GetFoodById_WhenNotFound_Returns404()
    {
        var response = await _client.GetAsync("/foods/99999");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateFood_UpdatesSuccessfully()
    {
        var create = new CreateFoodDto("Original Name", "Desc", 100m);
        var createResponse = await _client.PostAsJsonAsync("/foods", create);
        var created = await createResponse.Content.ReadFromJsonAsync<FoodDto>();

        var update = new UpdateFoodDto("Updated Name", "New Desc", 120m);
        var updateResponse = await _client.PutAsJsonAsync($"/foods/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<FoodDto>();
        Assert.Equal("Updated Name", updated!.Name);
        Assert.Equal(120m, updated.Price);
    }

    [Fact]
    public async Task DeactivateFood_SetsIsActiveToFalse()
    {
        var create = new CreateFoodDto("Active Food", "Desc", 89m);
        var createResponse = await _client.PostAsJsonAsync("/foods", create);
        var created = await createResponse.Content.ReadFromJsonAsync<FoodDto>();
        Assert.True(created!.IsActive);

        var deactivateResponse = await _client.PatchAsync($"/foods/{created.Id}/deactivate", null);
        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var deactivated = await deactivateResponse.Content.ReadFromJsonAsync<FoodDto>();
        Assert.False(deactivated!.IsActive);
    }
}
