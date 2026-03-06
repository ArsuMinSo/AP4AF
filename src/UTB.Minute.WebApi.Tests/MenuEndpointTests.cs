using System.Net;
using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.WebApi.Tests;

public class MenuEndpointTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public MenuEndpointTests()
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

    private async Task<FoodDto> CreateTestFood()
    {
        var dto = new CreateFoodDto("Test Food for Menu", "Description", 100m);
        var response = await _client.PostAsJsonAsync("/foods", dto);
        return (await response.Content.ReadFromJsonAsync<FoodDto>())!;
    }

    [Fact]
    public async Task CreateMenuItem_ReturnsCreated()
    {
        var food = await CreateTestFood();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var dto = new CreateMenuItemDto(today, food.Id, 10);

        var response = await _client.PostAsJsonAsync("/menu", dto);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var item = await response.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.NotNull(item);
        Assert.Equal(food.Id, item.Food.Id);
        Assert.Equal(10, item.AvailablePortions);
    }

    [Fact]
    public async Task GetAllMenuItems_ReturnsMenuItems()
    {
        var food = await CreateTestFood();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await _client.PostAsJsonAsync("/menu", new CreateMenuItemDto(today, food.Id, 5));

        var items = await _client.GetFromJsonAsync<List<MenuItemDto>>("/menu");
        Assert.NotNull(items);
        Assert.NotEmpty(items);
    }

    [Fact]
    public async Task GetTodayMenu_ReturnsTodayItems()
    {
        var food = await CreateTestFood();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        await _client.PostAsJsonAsync("/menu", new CreateMenuItemDto(today, food.Id, 8));

        var items = await _client.GetFromJsonAsync<List<MenuItemDto>>("/menu/today");
        Assert.NotNull(items);
        Assert.All(items, i => Assert.Equal(today, i.Date));
    }

    [Fact]
    public async Task UpdateMenuItem_UpdatesSuccessfully()
    {
        var food = await CreateTestFood();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createResponse = await _client.PostAsJsonAsync("/menu", new CreateMenuItemDto(today, food.Id, 10));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuItemDto>();

        var update = new UpdateMenuItemDto(today.AddDays(1), food.Id, 20);
        var updateResponse = await _client.PutAsJsonAsync($"/menu/{created!.Id}", update);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<MenuItemDto>();
        Assert.Equal(20, updated!.AvailablePortions);
    }

    [Fact]
    public async Task DeleteMenuItem_RemovesItem()
    {
        var food = await CreateTestFood();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var createResponse = await _client.PostAsJsonAsync("/menu", new CreateMenuItemDto(today, food.Id, 3));
        var created = await createResponse.Content.ReadFromJsonAsync<MenuItemDto>();

        var deleteResponse = await _client.DeleteAsync($"/menu/{created!.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/menu/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }
}
