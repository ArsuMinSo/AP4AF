using System.Net.Http.Json;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;

namespace UTB.Minute.Tests
{
    [Collection("Database collection")]
    public class OrderTests(DatabaseFixture fixture)
    {
        private readonly DatabaseFixture fixture = fixture;

        private async Task<(Food food, MenuItem menuItem)> CreateFoodAndMenuItemInDb(int portions = 5)
        {
            Food food = new() { Name = "Order Test Food", Description = "Desc", Price = 100m, IsActive = true };
            var today = DateOnly.FromDateTime(DateTime.UtcNow);

            using var context = fixture.CreateContext();
            context.Foods.Add(food);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            MenuItem menuItem = new() { Date = today, FoodId = food.Id, Food = food, AvailablePortions = portions };
            context.MenuItems.Add(menuItem);
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);

            return (food, menuItem);
        }

        [Fact]
        public async Task CreateOrder_WhenValid_ReturnsCreatedAndDecreasesPortions()
        {
            // Arrange
            var (_, menuItem) = await CreateFoodAndMenuItemInDb(5);
            var dto = new CreateOrderDto(menuItem.Id);

            // Act
            var createResponse = await fixture.HttpClient.PostAsJsonAsync("orders", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

            OrderDto? created = await createResponse.Content.ReadFromJsonAsync<OrderDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(created);
            Assert.Equal(menuItem.Id, created.MenuItemId);
            Assert.Equal(OrderStatusDto.Preparing, created.Status);
            Assert.NotNull(createResponse.Headers.Location);
            Assert.Contains($"orders/{created.Id}", createResponse.Headers.Location.ToString());

            // Verify portions decreased
            using var context = fixture.CreateContext();
            MenuItem? itemInDb = await context.MenuItems.FindAsync([menuItem.Id], TestContext.Current.CancellationToken);
            Assert.NotNull(itemInDb);
            Assert.Equal(4, itemInDb.AvailablePortions);
        }

        [Fact]
        public async Task CreateOrder_WhenNoPortionsAvailable_ReturnsConflict()
        {
            // Arrange
            var (_, menuItem) = await CreateFoodAndMenuItemInDb(0);
            var dto = new CreateOrderDto(menuItem.Id);

            // Act
            var response = await fixture.HttpClient.PostAsJsonAsync("orders", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task CreateOrder_WhenMenuItemNotFound_ReturnsNotFound()
        {
            // Arrange
            var dto = new CreateOrderDto(999999);

            // Act
            var response = await fixture.HttpClient.PostAsJsonAsync("orders", dto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetOrderById_WhenExists_ReturnsOkAndExpectedOrder()
        {
            // Arrange
            var (food, menuItem) = await CreateFoodAndMenuItemInDb(5);
            Order order = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Preparing };

            using (var context = fixture.CreateContext())
            {
                context.Orders.Add(order);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync($"orders/{order.Id}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            OrderDto? orderDto = await response.Content.ReadFromJsonAsync<OrderDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(orderDto);
            Assert.Equal(order.Id, orderDto.Id);
            Assert.Equal(menuItem.Id, orderDto.MenuItemId);
            Assert.Equal(OrderStatusDto.Preparing, orderDto.Status);
        }

        [Fact]
        public async Task GetOrderById_WhenNotExists_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;

            // Act
            var response = await fixture.HttpClient.GetAsync($"orders/{nonExistentId}", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetActiveOrders_DoesNotIncludeCompletedOrders()
        {
            // Arrange
            var (food, menuItem) = await CreateFoodAndMenuItemInDb(5);
            Order preparingOrder = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Preparing };
            Order completedOrder = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Completed };

            using (var context = fixture.CreateContext())
            {
                context.Orders.AddRange(preparingOrder, completedOrder);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            // Act
            var response = await fixture.HttpClient.GetAsync("orders", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            OrderDto[]? orders = await response.Content.ReadFromJsonAsync<OrderDto[]>(TestContext.Current.CancellationToken);
            Assert.NotNull(orders);
            Assert.Contains(orders, o => o.Id == preparingOrder.Id);
            Assert.DoesNotContain(orders, o => o.Id == completedOrder.Id);
        }

        [Fact]
        public async Task UpdateOrderStatus_PreparingToReady_UpdatesStatusInDatabase()
        {
            // Arrange
            var (food, menuItem) = await CreateFoodAndMenuItemInDb(5);
            Order order = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Preparing };

            using (var context = fixture.CreateContext())
            {
                context.Orders.Add(order);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var updateDto = new UpdateOrderStatusDto(OrderStatusDto.Ready);

            // Act
            var updateResponse = await fixture.HttpClient.PatchAsJsonAsync($"orders/{order.Id}/status", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            OrderDto? updated = await updateResponse.Content.ReadFromJsonAsync<OrderDto>(TestContext.Current.CancellationToken);
            Assert.NotNull(updated);
            Assert.Equal(OrderStatusDto.Ready, updated.Status);

            using (var context = fixture.CreateContext())
            {
                Order? orderInDb = await context.Orders.FindAsync([order.Id], TestContext.Current.CancellationToken);
                Assert.NotNull(orderInDb);
                Assert.Equal(OrderStatus.Ready, orderInDb.Status);
            }
        }

        [Fact]
        public async Task UpdateOrderStatus_InvalidTransition_ReturnsBadRequest()
        {
            // Arrange - Completed orders cannot be transitioned
            var (food, menuItem) = await CreateFoodAndMenuItemInDb(5);
            Order order = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Completed };

            using (var context = fixture.CreateContext())
            {
                context.Orders.Add(order);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var updateDto = new UpdateOrderStatusDto(OrderStatusDto.Preparing);

            // Act
            var response = await fixture.HttpClient.PatchAsJsonAsync($"orders/{order.Id}/status", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UpdateOrderStatus_PreparingToCancelled_UpdatesStatusInDatabase()
        {
            // Arrange
            var (food, menuItem) = await CreateFoodAndMenuItemInDb(5);
            Order order = new() { MenuItemId = menuItem.Id, MenuItem = menuItem, CreatedAt = DateTime.UtcNow, Status = OrderStatus.Preparing };

            using (var context = fixture.CreateContext())
            {
                context.Orders.Add(order);
                await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            }

            var updateDto = new UpdateOrderStatusDto(OrderStatusDto.Cancelled);

            // Act
            var updateResponse = await fixture.HttpClient.PatchAsJsonAsync($"orders/{order.Id}/status", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

            using (var context = fixture.CreateContext())
            {
                Order? orderInDb = await context.Orders.FindAsync([order.Id], TestContext.Current.CancellationToken);
                Assert.NotNull(orderInDb);
                Assert.Equal(OrderStatus.Cancelled, orderInDb.Status);
            }
        }

        [Fact]
        public async Task UpdateOrderStatus_WhenOrderNotFound_ReturnsNotFound()
        {
            // Arrange
            int nonExistentId = 999999;
            var updateDto = new UpdateOrderStatusDto(OrderStatusDto.Ready);

            // Act
            var response = await fixture.HttpClient.PatchAsJsonAsync($"orders/{nonExistentId}/status", updateDto, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
