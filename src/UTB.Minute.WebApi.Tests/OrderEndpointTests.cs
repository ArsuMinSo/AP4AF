using System.Net;
using System.Net.Http.Json;
using UTB.Minute.Contracts;

namespace UTB.Minute.WebApi.Tests;

public class OrderEndpointTests : IAsyncLifetime
{
    private readonly TestWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public OrderEndpointTests()
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

    private async Task<(FoodDto food, MenuItemDto menuItem)> CreateFoodAndMenuItem(int portions = 5)
    {
        var food = (await (await _client.PostAsJsonAsync("/foods",
            new CreateFoodDto("Order Test Food", "Desc", 100m))).Content.ReadFromJsonAsync<FoodDto>())!;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var menuItem = (await (await _client.PostAsJsonAsync("/menu",
            new CreateMenuItemDto(today, food.Id, portions))).Content.ReadFromJsonAsync<MenuItemDto>())!;

        return (food, menuItem);
    }

    [Fact]
    public async Task CreateOrder_DecreasesAvailablePortions()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(5);

        var response = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var order = await response.Content.ReadFromJsonAsync<OrderDto>();
        Assert.NotNull(order);
        Assert.Equal(OrderStatusDto.Preparing, order.Status);

        var updatedMenu = await _client.GetFromJsonAsync<MenuItemDto>($"/menu/{menuItem.Id}");
        Assert.Equal(4, updatedMenu!.AvailablePortions);
    }

    [Fact]
    public async Task CreateOrder_WhenNoPortionsLeft_ReturnsConflict()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(1);
        await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));

        var response = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetActiveOrders_DoesNotIncludeCompleted()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(5);
        var orderResponse = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Complete the order: Preparing -> Ready -> Completed
        await _client.PatchAsJsonAsync($"/orders/{order!.Id}/status", new UpdateOrderStatusDto(OrderStatusDto.Ready));
        await _client.PatchAsJsonAsync($"/orders/{order.Id}/status", new UpdateOrderStatusDto(OrderStatusDto.Completed));

        var active = await _client.GetFromJsonAsync<List<OrderDto>>("/orders");
        Assert.DoesNotContain(active!, o => o.Id == order.Id);
    }

    [Fact]
    public async Task UpdateOrderStatus_ValidTransition_Succeeds()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(3);
        var orderResponse = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(OrderStatusDto.Preparing, order!.Status);

        var updateResponse = await _client.PatchAsJsonAsync(
            $"/orders/{order.Id}/status",
            new UpdateOrderStatusDto(OrderStatusDto.Ready));
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<OrderDto>();
        Assert.Equal(OrderStatusDto.Ready, updated!.Status);
    }

    [Fact]
    public async Task UpdateOrderStatus_InvalidTransition_ReturnsBadRequest()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(3);
        var orderResponse = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();

        // Cancel the order first
        await _client.PatchAsJsonAsync($"/orders/{order!.Id}/status", new UpdateOrderStatusDto(OrderStatusDto.Cancelled));

        // Try to set it to Ready (invalid)
        var invalidResponse = await _client.PatchAsJsonAsync(
            $"/orders/{order.Id}/status",
            new UpdateOrderStatusDto(OrderStatusDto.Ready));
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
    }

    [Fact]
    public async Task CancelledOrder_DoesNotRestorePortions()
    {
        var (_, menuItem) = await CreateFoodAndMenuItem(3);
        var orderResponse = await _client.PostAsJsonAsync("/orders", new CreateOrderDto(menuItem.Id));
        var order = await orderResponse.Content.ReadFromJsonAsync<OrderDto>();

        var afterOrder = await _client.GetFromJsonAsync<MenuItemDto>($"/menu/{menuItem.Id}");
        Assert.Equal(2, afterOrder!.AvailablePortions);

        await _client.PatchAsJsonAsync($"/orders/{order!.Id}/status", new UpdateOrderStatusDto(OrderStatusDto.Cancelled));

        var afterCancel = await _client.GetFromJsonAsync<MenuItemDto>($"/menu/{menuItem.Id}");
        Assert.Equal(2, afterCancel!.AvailablePortions);
    }
}
