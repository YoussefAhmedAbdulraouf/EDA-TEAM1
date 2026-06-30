using Amazon.SQS;
using Amazon.SQS.Model;
using DeliveryService.Events;
using DeliveryService.Services;
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

            foreach (var msg in response.Messages)
            {
                try
                {
                    var envelope = JsonSerializer.Deserialize<SnsEnvelope>(msg.Body);

                    // TODO: Determine the event type and handle accordingly.
                    //
                    // Use the MessageAttributes from the SNS envelope:
                    //   var eventType = envelope.MessageAttributes?["EventType"]?.Value;
                    //
                    // Then switch on eventType:
                    //
                    // CASE "OrderPlaced":
                    //   - Deserialize as OrderPlaced
                    //   - Store the delivery address in _orderAddresses dictionary:
                    //     _orderAddresses[orderPlaced.OrderId] = orderPlaced.DeliveryAddress;
                    //   - Log: "[Delivery] Saved address for order {OrderId}"
                    //   - Do NOT publish anything — just save for later.
                    //
                    // CASE "OrderAccepted":
                    //   - Deserialize as OrderAccepted
                    //   - Look up the address: _orderAddresses.TryGetValue(...)
                    //   - Log: "[Delivery] Looking for a driver near {address}..."
                    //   - Simulate search: await Task.Delay(TimeSpan.FromSeconds(6))
                    //   - Pick a random driver:
                    //     var driver = _drivers[Random.Shared.Next(_drivers.Count)];
                    //   - Log: "[Delivery] Driver {Name} assigned to order {OrderId}"
                    //   - Publish a DriverAssigned event with:
                    //     DriverId: Guid.NewGuid()
                    //     DriverName: driver.Name
                    //     DriverPhone: driver.Phone
                    //     EstimatedDeliveryMinutes: Random.Shared.Next(15, 35)

                    await _sqs.DeleteMessageAsync(_queueUrl, msg.ReceiptHandle, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process message");
                }
            }
        }
    }
}
