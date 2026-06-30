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
    private readonly string _queueUrl = "https://sqs.us-east-1.amazonaws.com/944596879679/IDA-Team1-delivery-queue";

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

            foreach (var msg in response.Messages)
            {
                try
                {
                    // Step 1: Unwrap the SNS envelope to get the actual event JSON
                    var envelope = JsonSerializer.Deserialize<SnsEnvelope>(msg.Body);
                    var orderPlaced = JsonSerializer.Deserialize<OrderPlaced>(envelope!.Message);

                    _logger.LogInformation(
                        "[Restaurant] Received order {OrderId} - {ItemCount} items, total: {Total:C}",
                        orderPlaced!.OrderId,
                        orderPlaced.Items.Count,
                        orderPlaced.TotalAmount);

                    // TODO: Step 2 - Simulate the kitchen reviewing the order
                    //   Use Task.Delay to simulate a 3-5 second review time.
                    //   Log something like "[Restaurant] Reviewing order..."

                    // TODO: Step 3 - Decide: accept or reject
                    //   Simple rule: reject if TotalAmount > 500, accept otherwise.
                    //
                    //   If ACCEPTING:
                    //     - Pick a random prep time between 15-45 min
                    //       e.g. var prepTime = Random.Shared.Next(15, 45);
                    //     - Log: "[Restaurant] Order {OrderId} ACCEPTED - prep time: {prepTime} min"
                    //     - Publish an OrderAccepted event using _publisher.PublishAsync(...)
                    //
                    //   If REJECTING:
                    //     - Log: "[Restaurant] Order {OrderId} REJECTED - amount too high"
                    //     - Publish an OrderRejected event
                    //
                    //   Use hardcoded values for RestaurantId and RestaurantName:
                    //     RestaurantId: Guid.NewGuid() or a fixed Guid
                    //     RestaurantName: "Pizza Palace"

                    // Step 4: Delete the message (only after successful processing!)
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
