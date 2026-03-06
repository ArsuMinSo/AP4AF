namespace UTB.Minute.Contracts;

public enum OrderStatusDto
{
    Preparing,
    Ready,
    Cancelled,
    Completed
}

public record OrderDto(int Id, int MenuItemId, string FoodName, DateTime CreatedAt, OrderStatusDto Status);

public record CreateOrderDto(int MenuItemId);

public record UpdateOrderStatusDto(OrderStatusDto NewStatus);
