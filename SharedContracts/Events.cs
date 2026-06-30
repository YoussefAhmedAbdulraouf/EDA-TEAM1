// ============================================
// Shared Event Contracts — Food Ordering System
// ============================================
// Copy this file into your project.
// Every team member uses the EXACT same record definitions.
// These are your INTEGRATION EVENTS — the public contract
// between services. Do not change them without team agreement.

using System.Text.Json;

namespace FoodOrdering.Contracts;

// --- Published by Order Service (Student A) ---

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

// --- Published by Restaurant Service (Student B) ---

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

// --- Published by Delivery Service (Student D) ---

public record DriverAssigned(
    Guid OrderId,
    Guid DriverId,
    string DriverName,
    string DriverPhone,
    int EstimatedDeliveryMinutes,
    DateTime OccurredAt
);

// --- Shared DTO ---

public record OrderItemDto(
    string ItemName,
    int Quantity,
    decimal UnitPrice
);

// --- SNS Envelope ---
// When SNS delivers a message to SQS, it wraps your original
// JSON in an envelope. This record helps you unwrap it.

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
