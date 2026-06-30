// Copy of shared contracts — all team members must use the same definitions.
// See SharedContracts/Events.cs for the full documentation.

namespace OrderService.Events;

public record OrderPlaced(
    Guid OrderId,
    Guid CustomerId,
    string CustomerName,
    string CustomerPhone,
    string DeliveryAddress,
    List<OrderItemDto> Items,
    decimal TotalAmount,
    DateTime OccurredAt
);

public record OrderCancelled(
    Guid OrderId,
    string Reason,
    DateTime OccurredAt
);

public record OrderItemDto(
    string ItemName,
    int Quantity,
    decimal UnitPrice
);
