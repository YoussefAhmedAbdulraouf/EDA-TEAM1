namespace RestaurantService.Events;

// --- Events this service CONSUMES ---

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

// --- Events this service PUBLISHES ---

public record OrderAccepted(
    Guid OrderId,
    Guid RestaurantId,
    string RestaurantName,
    int EstimatedPrepTimeMinutes,
    DateTime OccurredAt
);

public record OrderRejected(
    Guid OrderId,
    Guid RestaurantId,
    string Reason,
    DateTime OccurredAt
);

// --- Shared DTOs ---

public record OrderItemDto(
    string ItemName,
    int Quantity,
    decimal UnitPrice
);

public record SnsEnvelope
{
    public string Message { get; init; } = "";
    public string MessageId { get; init; } = "";
    public string Type { get; init; } = "";
}
