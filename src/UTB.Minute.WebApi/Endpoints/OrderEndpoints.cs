using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using UTB.Minute.Contracts;
using UTB.Minute.Db;
using UTB.Minute.Db.Entities;
using UTB.Minute.WebApi.Services;

namespace UTB.Minute.WebApi.Endpoints;

public static class OrderEndpoints
{
    private static readonly Dictionary<OrderStatus, HashSet<OrderStatus>> ValidTransitions = new()
    {
        [OrderStatus.Preparing] = [OrderStatus.Ready, OrderStatus.Cancelled],
        [OrderStatus.Ready] = [OrderStatus.Completed],
        [OrderStatus.Cancelled] = [OrderStatus.Completed],
        [OrderStatus.Completed] = []
    };

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/orders").WithTags("Orders");

        group.MapGet("/", GetActiveOrders);
        group.MapGet("/{id:int}", GetOrderById);
        group.MapPost("/", CreateOrder);
        group.MapPatch("/{id:int}/status", UpdateOrderStatus);
        group.MapGet("/events", StreamOrderEvents);

        return routes;
    }

    private static OrderDto ToOrderDto(Order o) =>
        new(o.Id, o.MenuItemId, o.MenuItem.Food.Name, o.CreatedAt, (OrderStatusDto)o.Status);

    private static async Task<Ok<List<OrderDto>>> GetActiveOrders(AppDbContext db)
    {
        var orders = await db.Orders
            .Include(o => o.MenuItem)
            .ThenInclude(m => m.Food)
            .Where(o => o.Status != OrderStatus.Completed)
            .ToListAsync();
        return TypedResults.Ok(orders.Select(ToOrderDto).ToList());
    }

    private static async Task<Results<Ok<OrderDto>, NotFound>> GetOrderById(int id, AppDbContext db)
    {
        var order = await db.Orders
            .Include(o => o.MenuItem)
            .ThenInclude(m => m.Food)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return TypedResults.NotFound();
        return TypedResults.Ok(ToOrderDto(order));
    }

    private static async Task<Results<Created<OrderDto>, NotFound, Conflict<string>>> CreateOrder(
        CreateOrderDto dto, AppDbContext db, SseService sse)
    {
        var menuItem = await db.MenuItems
            .Include(m => m.Food)
            .FirstOrDefaultAsync(m => m.Id == dto.MenuItemId);
        if (menuItem is null) return TypedResults.NotFound();

        if (menuItem.AvailablePortions <= 0)
            return TypedResults.Conflict("No portions available.");

        menuItem.AvailablePortions--;

        var order = new Order
        {
            MenuItemId = dto.MenuItemId,
            MenuItem = menuItem,
            CreatedAt = DateTime.UtcNow,
            Status = OrderStatus.Preparing
        };
        db.Orders.Add(order);

        try
        {
            await db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return TypedResults.Conflict("Order could not be placed due to concurrent modification. Please try again.");
        }

        var result = ToOrderDto(order);
        await sse.BroadcastAsync("order-created", result);
        return TypedResults.Created($"/orders/{order.Id}", result);
    }

    private static async Task<Results<Ok<OrderDto>, NotFound, BadRequest<string>>> UpdateOrderStatus(
        int id, UpdateOrderStatusDto dto, AppDbContext db, SseService sse)
    {
        var order = await db.Orders
            .Include(o => o.MenuItem)
            .ThenInclude(m => m.Food)
            .FirstOrDefaultAsync(o => o.Id == id);
        if (order is null) return TypedResults.NotFound();

        var newStatus = (OrderStatus)dto.NewStatus;
        if (!ValidTransitions[order.Status].Contains(newStatus))
        {
            return TypedResults.BadRequest(
                $"Invalid status transition from '{order.Status}' to '{newStatus}'.");
        }

        order.Status = newStatus;
        await db.SaveChangesAsync();

        var result = ToOrderDto(order);
        await sse.BroadcastAsync("order-updated", result);
        return TypedResults.Ok(result);
    }

    private static async Task StreamOrderEvents(
        HttpContext context, SseService sse, CancellationToken cancellationToken)
    {
        context.Response.Headers.Append("Content-Type", "text/event-stream");
        context.Response.Headers.Append("Cache-Control", "no-cache");
        context.Response.Headers.Append("Connection", "keep-alive");

        var channel = System.Threading.Channels.Channel.CreateUnbounded<string>();

        async Task WriteToChannel(string message, CancellationToken ct)
        {
            await channel.Writer.WriteAsync(message, ct);
        }

        using var subscription = sse.Subscribe(WriteToChannel);

        // Send initial ping
        await context.Response.WriteAsync("event: ping\ndata: connected\n\n", cancellationToken);
        await context.Response.Body.FlushAsync(cancellationToken);

        await foreach (var message in channel.Reader.ReadAllAsync(cancellationToken))
        {
            await context.Response.WriteAsync(message, cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);
        }
    }
}
