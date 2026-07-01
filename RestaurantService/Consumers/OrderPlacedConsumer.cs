using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using RestaurantService.Events;
using RestaurantService.Services;
using System.Text.Json;

namespace RestaurantService.Consumers;

public class OrderPlacedConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly EventPublisher _publisher;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    // TODO: Paste your team's Restaurant SQS Queue URL here
    private readonly string _queueUrl = "https://sqs.us-east-1.amazonaws.com/944596879679/IDA-Team1-restaurant-queue";

    public OrderPlacedConsumer(IAmazonSQS sqs, EventPublisher publisher, ILogger<OrderPlacedConsumer> logger)
    {
        _sqs = sqs;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Restaurant consumer started. Listening on queue...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            }, stoppingToken);

            if (response?.Messages == null || response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var msg in response.Messages)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(msg.Body))
                    {
                        _logger.LogWarning("Received empty message body for MessageId: {MessageId}", msg.MessageId);
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    // Step 1: Unwrap the SNS envelope to get the actual event JSON
                    var envelope = JsonSerializer.Deserialize<SnsEnvelope>(msg.Body);
                    if (envelope == null || string.IsNullOrEmpty(envelope.Message))
                    {
                        _logger.LogWarning("Received a message that couldn't be deserialized into an SnsEnvelope or is missing the message payload.");
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    // Extract the EventType attribute value
                    string? eventType = null;
                    if (envelope.MessageAttributes != null &&
                        envelope.MessageAttributes.TryGetValue("EventType", out var attribute))
                    {
                        eventType = attribute?.Value;
                    }

                    // If eventType is null, log a warning and delete/ignore the message
                    if (string.IsNullOrEmpty(eventType))
                    {
                        _logger.LogWarning("Received message {MessageId} missing 'EventType' attribute.", msg.MessageId);
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    // If it is NOT an OrderPlaced event, delete it and skip it (prevents processing OrderAccepted/OrderRejected in loop)
                    if (eventType != nameof(OrderPlaced))
                    {
                        _logger.LogDebug("[Restaurant] Ignoring non-OrderPlaced event type: {EventType}", eventType);
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    var orderPlaced = JsonSerializer.Deserialize<OrderPlaced>(envelope.Message);
                    if (orderPlaced == null)
                    {
                        _logger.LogWarning("Failed to deserialize OrderPlaced event.");
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    _logger.LogInformation(
                        "[Restaurant] Received order {OrderId} - {ItemCount} items, total: {Total:C}",
                        orderPlaced.OrderId,
                        orderPlaced.Items?.Count ?? 0,
                        orderPlaced.TotalAmount);

                    // Step 2: Simulate the kitchen reviewing the order
                    _logger.LogInformation("[Restaurant] Reviewing order {OrderId}...", orderPlaced.OrderId);
                    var reviewDelaySeconds = Random.Shared.Next(3, 6);
                    await Task.Delay(TimeSpan.FromSeconds(reviewDelaySeconds), stoppingToken);

                    // Hardcoded Restaurant details
                    var restaurantId = Guid.Parse("d3b07384-d113-4e4e-a342-a8c4c7003c21");
                    var restaurantName = "Pizza Palace";

                    // Step 3: Decide: accept or reject
                    if (orderPlaced.TotalAmount > 500)
                    {
                        _logger.LogInformation("[Restaurant] Order {OrderId} REJECTED - amount too high", orderPlaced.OrderId);
                        var orderRejectedEvent = new OrderRejected(
                            OrderId: orderPlaced.OrderId,
                            RestaurantId: restaurantId,
                            Reason: "amount too high",
                            OccurredAt: DateTime.UtcNow
                        );
                        await _publisher.PublishAsync(orderRejectedEvent);
                    }
                    else
                    {
                        var prepTime = Random.Shared.Next(15, 46);
                        _logger.LogInformation("[Restaurant] Order {OrderId} ACCEPTED - prep time: {prepTime} min", orderPlaced.OrderId, prepTime);
                        var orderAcceptedEvent = new OrderAccepted(
                            OrderId: orderPlaced.OrderId,
                            RestaurantId: restaurantId,
                            RestaurantName: restaurantName,
                            EstimatedPrepTimeMinutes: prepTime,
                            OccurredAt: DateTime.UtcNow
                        );
                        await _publisher.PublishAsync(orderAcceptedEvent);
                    }

                    // Step 4: Delete the message (only after successful processing!)
                    await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message {MessageId}", msg.MessageId);
                }
            }
        }
    }
}
