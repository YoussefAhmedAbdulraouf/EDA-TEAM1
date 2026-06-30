using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using System.Text.Json;

namespace FoodOrdering.Messaging;

/// <summary>
/// Publishes events to an SNS topic.
/// The event type name is attached as a MessageAttribute so that
/// SNS filter policies can route events to the right SQS queues.
/// </summary>
public class EventPublisher
{
    private readonly IAmazonSimpleNotificationService _sns;
    private readonly string _topicArn;

    public EventPublisher(IAmazonSimpleNotificationService sns, string topicArn)
    {
        _sns = sns;
        _topicArn = topicArn;
    }

    public async Task PublishAsync<T>(T @event) where T : class
    {
        var message = JsonSerializer.Serialize(@event);

        var request = new PublishRequest
        {
            TopicArn = _topicArn,
            Message = message,
            MessageAttributes = new Dictionary<string, MessageAttributeValue>
            {
                ["EventType"] = new MessageAttributeValue
                {
                    DataType = "String",
                    StringValue = typeof(T).Name
                }
            }
        };

        await _sns.PublishAsync(request);
    }
}
