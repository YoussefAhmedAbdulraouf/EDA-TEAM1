using Microsoft.AspNetCore.Mvc;
using OrderService.Events;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly EventPublisher _publisher;

    public OrdersController(EventPublisher publisher)
    {
        _publisher = publisher;
    }

    /// <summary>
    /// POST /api/orders
    /// Creates a new order and publishes an OrderPlaced event.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> PlaceOrder([FromBody] PlaceOrderRequest request)
    {
        var orderId = Guid.NewGuid();

        // In a real system, you'd save to a database here.
        // For this lab, we just publish the event.

        // Create the OrderPlaced event with the data from the request
        var orderPlaced = new OrderPlaced(
            OrderId: orderId,
            CustomerId: request.CustomerId,
            CustomerName: request.CustomerName,
            CustomerPhone: request.CustomerPhone,
            DeliveryAddress: request.DeliveryAddress,
            Items: request.Items,
            TotalAmount: request.Items.Sum(i => i.Quantity * i.UnitPrice),
            OccurredAt: DateTime.UtcNow
        );

        // Publish the event using _publisher.PublishAsync(...)
        await _publisher.PublishAsync(orderPlaced);

        // Log to the console so you can see it working
        Console.WriteLine($"[OrderService] OrderPlaced event published — OrderId: {orderId}, Customer: {request.CustomerName}, Total: {orderPlaced.TotalAmount:C}");

        // Return Accepted (HTTP 202) with the OrderId
        return Accepted(new { OrderId = orderId });
    }

    /// <summary>
    /// DELETE /api/orders/{orderId}
    /// Cancels an existing order and publishes an OrderCancelled event.
    /// </summary>
    [HttpDelete("{orderId}")]
    public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest request)
    {
        // Create the OrderCancelled event
        var orderCancelled = new OrderCancelled(
            OrderId: orderId,
            Reason: request.Reason,
            OccurredAt: DateTime.UtcNow
        );

        // Publish the event
        await _publisher.PublishAsync(orderCancelled);

        // Log to the console
        Console.WriteLine($"[OrderService] OrderCancelled event published — OrderId: {orderId}, Reason: {request.Reason}");

        // Return Ok with the OrderId and Status
        return Ok(new { OrderId = orderId, Status = "Cancelled" });
    }
}

// --- Request models (what the API receives from the client) ---

public record PlaceOrderRequest(
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string DeliveryAddress,
    List<OrderItemDto> Items
);

public record CancelOrderRequest(string Reason);
