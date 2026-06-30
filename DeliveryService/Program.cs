using Amazon.SQS;
using Amazon.SimpleNotificationService;
using DeliveryService.Consumers;
using DeliveryService.Services;

var builder = Host.CreateApplicationBuilder(args);

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

builder.Services.AddHostedService<DeliveryConsumer>();

Console.WriteLine("===========================================");
Console.WriteLine("  DELIVERY SERVICE - Listening for accepted orders");
Console.WriteLine("===========================================");

var host = builder.Build();
host.Run();
