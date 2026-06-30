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

        // TODO: Create the OrderPlaced event with the data from the request
        // TODO: Publish the event using _publisher.PublishAsync(...)
        // TODO: Log to the console so you can see it working
        // TODO: Return Accepted (HTTP 202) with the OrderId

        throw new NotImplementedException("Implement the PlaceOrder method!");
    }

    /// <summary>
    /// DELETE /api/orders/{orderId}
    /// Cancels an existing order and publishes an OrderCancelled event.
    /// </summary>
    [HttpDelete("{orderId}")]
    public async Task<IActionResult> CancelOrder(Guid orderId, [FromBody] CancelOrderRequest request)
    {
        // TODO: Create the OrderCancelled event
        // TODO: Publish the event
        // TODO: Log to the console
        // TODO: Return Ok with the OrderId and Status

        throw new NotImplementedException("Implement the CancelOrder method!");
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
