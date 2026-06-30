using Amazon.SQS;
using Amazon.SimpleNotificationService;
using RestaurantService.Consumers;
using RestaurantService.Services;

var builder = Host.CreateApplicationBuilder(args);

// AWS SDK picks up credentials from ~/.aws/credentials automatically
builder.Services.AddSingleton<IAmazonSQS>(sp =>
    new AmazonSQSClient(Amazon.RegionEndpoint.USEast1));

builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
    new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.USEast1));

builder.Services.AddSingleton(sp =>
{
    var sns = sp.GetRequiredService<IAmazonSimpleNotificationService>();

    // TODO: Paste your team's SNS Topic ARN here
    var topicArn = "arn:aws:sns:us-east-1:944596879679:IDA-Team1-order-events";

    return new EventPublisher(sns, topicArn);
});

builder.Services.AddHostedService<OrderPlacedConsumer>();

Console.WriteLine("===========================================");
Console.WriteLine("  RESTAURANT SERVICE - Listening for orders");
Console.WriteLine("===========================================");

var host = builder.Build();
host.Run();
