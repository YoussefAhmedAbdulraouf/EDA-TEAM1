namespace DeliveryService.Events;

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

public record OrderAccepted(
    Guid OrderId,
    Guid RestaurantId,
    string RestaurantName,
    int EstimatedPrepTimeMinutes,
    DateTime OccurredAt
);

// --- Events this service PUBLISHES ---

public record DriverAssigned(
    Guid OrderId,
    Guid DriverId,
    string DriverName,
    string DriverPhone,
    int EstimatedDeliveryMinutes,
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
    public Dictionary<string, SnsMessageAttribute>? MessageAttributes { get; init; }
}

public record SnsMessageAttribute
{
    public string Type { get; init; } = "";
    public string Value { get; init; } = "";
}
