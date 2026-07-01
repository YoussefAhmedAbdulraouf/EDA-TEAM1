using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Hosting;
using NotificationService.Events;
using System.Text.Json;

namespace NotificationService.Consumers;

public class NotificationConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly ILogger<NotificationConsumer> _logger;

    // TODO: Paste your team's Notification SQS Queue URL here
    private readonly string _queueUrl = "https://sqs.us-east-1.amazonaws.com/944596879679/IDA-Team1-notification-queue";

    public NotificationConsumer(IAmazonSQS sqs, ILogger<NotificationConsumer> logger)
    {
        _sqs = sqs;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification consumer started. Listening for all events...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            }, stoppingToken);

            foreach (var msg in response?.Messages?? [])
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<SnsEnvelope>(msg.Body);

                    // Simulate SMS sending delay
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

                    // TODO: Route the event to the right handler based on its type.
                    //
                    // CHALLENGE: The SQS message doesn't directly tell you which
                    // event type it is. You need to figure it out. Here are some approaches:
                    //
                    // Approach 1 (Recommended): Read the EventType from the SNS
                    //   MessageAttributes in the envelope:
                    //   var eventType = envelope.MessageAttributes?["EventType"]?.Value;
                    //   Then switch on eventType to deserialize the right type.
                    //
                    // Approach 2: Try deserializing into each type and check for
                    //   distinguishing fields (works but is fragile).
                    //
                    // Once you know the event type, deserialize and log a
                    // customer-friendly message:
                    //
                    //   OrderPlaced    -> "Dear {Name}, your order has been received!"
                    //   OrderAccepted  -> "Great news! {Restaurant} is preparing your food. ETA: {X} min."
                    //   OrderRejected  -> "Sorry, your order was declined by the restaurant. Reason: {Reason}"
                    //   DriverAssigned -> "Your driver {Name} is on the way! ETA: {X} min. Contact: {Phone}"
                    //   OrderCancelled -> "Your order has been cancelled. Reason: {Reason}"
                    //
                    // Use _logger.LogInformation for the messages so they show in the console.

                    string? eventType = null;
                    if (envelope?.MessageAttributes != null && envelope.MessageAttributes.TryGetValue("EventType", out var attribute))
                    {
                        eventType = attribute.Value;
                    }

                    if (!string.IsNullOrEmpty(eventType) && !string.IsNullOrEmpty(envelope?.Message))
                    {
                        switch (eventType)
                        {
                            case nameof(OrderPlaced):
                                var placed = JsonSerializer.Deserialize<OrderPlaced>(envelope.Message);
                                if (placed != null) _logger.LogInformation("Dear {Name}, your order has been received!", placed.CustomerName);
                                break;
                            case nameof(OrderAccepted):
                                var accepted = JsonSerializer.Deserialize<OrderAccepted>(envelope.Message);
                                if (accepted != null) _logger.LogInformation("Great news! {Restaurant} is preparing your food. ETA: {X} min.", accepted.RestaurantName, accepted.EstimatedPrepTimeMinutes);
                                break;
                            case nameof(OrderRejected):
                                var rejected = JsonSerializer.Deserialize<OrderRejected>(envelope.Message);
                                if (rejected != null) _logger.LogInformation("Sorry, your order was declined by the restaurant. Reason: {Reason}", rejected.Reason);
                                break;
                            case nameof(DriverAssigned):
                                var driverAssigned = JsonSerializer.Deserialize<DriverAssigned>(envelope.Message);
                                if (driverAssigned != null) _logger.LogInformation("Your driver {Name} is on the way! ETA: {X} min. Contact: {Phone}", driverAssigned.DriverName, driverAssigned.EstimatedDeliveryMinutes, driverAssigned.DriverPhone);
                                break;
                            case nameof(OrderCancelled):
                                var cancelled = JsonSerializer.Deserialize<OrderCancelled>(envelope.Message);
                                if (cancelled != null) _logger.LogInformation("Your order has been cancelled. Reason: {Reason}", cancelled.Reason);
                                break;
                            default:
                                _logger.LogWarning("Unknown EventType: {EventType}", eventType);
                                break;
                        }
                    }

                    await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process notification");
                }
            }
        }
    }
}
