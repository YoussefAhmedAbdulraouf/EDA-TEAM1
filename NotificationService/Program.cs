using Amazon.SQS;
using NotificationService.Consumers;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSingleton<IAmazonSQS>(sp =>
    new AmazonSQSClient(Amazon.RegionEndpoint.USEast1));

builder.Services.AddHostedService<NotificationConsumer>();

Console.WriteLine("===========================================");
Console.WriteLine("  NOTIFICATION SERVICE - Listening for all events");
Console.WriteLine("===========================================");

var host = builder.Build();
host.Run();
