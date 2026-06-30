using Amazon.SQS;
using Amazon.SQS.Model;
using DeliveryService.Events;
using DeliveryService.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace DeliveryService.Consumers;

public class DeliveryConsumer : BackgroundService
{
    private readonly IAmazonSQS _sqs;
    private readonly EventPublisher _publisher;
    private readonly ILogger<DeliveryConsumer> _logger;

    // TODO: Paste your team's Delivery SQS Queue URL here
    private readonly string _queueUrl = "https://sqs.us-east-1.amazonaws.com/944596879679/IDA-Team1-delivery-queue";

    // In-memory store: save delivery addresses from OrderPlaced events
    // so we have them when OrderAccepted arrives later.
    // This is the EVENT-DRIVEN approach to getting data you need!
    private readonly Dictionary<Guid, string> _orderAddresses = new();

    // Fake driver pool
    private readonly List<(string Name, string Phone)> _drivers = new()
    {
        ("Ahmed Hassan", "+20-100-123-4567"),
        ("Sara Mohamed", "+20-100-765-4321"),
        ("Omar Ali", "+20-100-555-8888"),
        ("Nour Ibrahim", "+20-100-999-1234"),
    };

    public DeliveryConsumer(IAmazonSQS sqs, EventPublisher publisher, ILogger<DeliveryConsumer> logger)
    {
        _sqs = sqs;
        _publisher = publisher;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Delivery consumer started. Listening for events...");

        while (!stoppingToken.IsCancellationRequested)
        {
            var response = await _sqs.ReceiveMessageAsync(new ReceiveMessageRequest
            {
                QueueUrl = _queueUrl,
                MaxNumberOfMessages = 10,
                WaitTimeSeconds = 20
            }, stoppingToken);

            // RECOMMENDATION: Guard against null responses or empty message pools safely
            if (response?.Messages == null || response.Messages.Count == 0)
            {
                continue;
            }

            foreach (var msg in response.Messages)
            {
                try
                {
                    // Ensure the individual message body has content
                    if (string.IsNullOrWhiteSpace(msg.Body))
                    {
                        _logger.LogWarning("Received empty message body for MessageId: {MessageId}", msg.MessageId);
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    var envelope = JsonSerializer.Deserialize<SnsEnvelope>(msg.Body);

                    // 1. Ensure the envelope itself isn't null
                    if (envelope == null)
                    {
                        _logger.LogWarning("Received a message that couldn't be deserialized into an SnsEnvelope.");
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue;
                    }

                    // 2. Safely check if the EventType key exists in the dictionary
                    string? eventType = null;
                    if (envelope.MessageAttributes != null && 
                        envelope.MessageAttributes.TryGetValue("EventType", out var attribute))
                    {
                        eventType = attribute?.Value;
                    }

                    // 3. If eventType is null, log it and move on instead of crashing
                    if (string.IsNullOrEmpty(eventType))
                    {
                        _logger.LogWarning("Received message {MessageId} missing 'EventType' attribute. Body snippet: {Body}", 
                            msg.MessageId, msg.Body.Length > 100 ? msg.Body[..100] : msg.Body);
                        
                        await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                        continue; 
                    }

                    switch (eventType)
                    {
                        case "OrderPlaced":
                            if (envelope.Message != null)
                            {
                                var orderPlaced = JsonSerializer.Deserialize<OrderPlaced>(envelope.Message);
                                if (orderPlaced != null)
                                {
                                    _orderAddresses[orderPlaced.OrderId] = orderPlaced.DeliveryAddress;
                                    _logger.LogInformation("[Delivery] Saved address for order {OrderId}", orderPlaced.OrderId);
                                }
                            }
                            break;

                        case "OrderAccepted":
                            if (envelope.Message != null)
                            {
                                var orderAccepted = JsonSerializer.Deserialize<OrderAccepted>(envelope.Message);
                                if (orderAccepted != null)
                                {
                                    if (_orderAddresses.TryGetValue(orderAccepted.OrderId, out var address))
                                    {
                                        _logger.LogInformation("[Delivery] Looking for a driver near {address}...", address);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("[Delivery] OrderPlaced missing! Address unknown for order {OrderId}. Proceeding anyway...", orderAccepted.OrderId);
                                        address = "Unknown Address";
                                    }

                                    // Simulate searching for a driver
                                    await Task.Delay(TimeSpan.FromSeconds(6), stoppingToken);

                                    // Pick a random driver
                                    var driver = _drivers[Random.Shared.Next(_drivers.Count)];
                                    _logger.LogInformation("[Delivery] Driver {Name} assigned to order {OrderId}", driver.Name, orderAccepted.OrderId);

                                    // Publish DriverAssigned event
                                    var driverAssignedEvent = new DriverAssigned(
                                        OrderId: orderAccepted.OrderId,
                                        DriverId: Guid.NewGuid(),
                                        DriverName: driver.Name,
                                        DriverPhone: driver.Phone,
                                        EstimatedDeliveryMinutes: Random.Shared.Next(15, 35),
                                        OccurredAt: DateTime.UtcNow
                                    );

                                    await _publisher.PublishAsync(driverAssignedEvent);

                                    // PRODUCTION TIP: Clean up storage so memory doesn't leak over days of operation
                                    _orderAddresses.Remove(orderAccepted.OrderId);
                                }
                            }
                            break;

                        default:
                            _logger.LogWarning("Unknown or unhandled event type: {EventType}", eventType);
                            break;
                    }

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