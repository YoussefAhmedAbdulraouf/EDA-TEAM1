namespace NotificationService.Events;

// This service CONSUMES all events but publishes none.
// It is the customer's window into the system.

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

public record DriverAssigned(
    Guid OrderId,
    Guid DriverId,
    string DriverName,
    string DriverPhone,
    int EstimatedDeliveryMinutes,
    DateTime OccurredAt
);

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
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; init; }
}

public record SnsMessageAttribute
{
    public string Type { get; init; } = "";
    public string Value { get; init; } = "";
}
