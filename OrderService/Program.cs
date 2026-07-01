using Amazon.SimpleNotificationService;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------
// AWS SDK picks up credentials from ~/.aws/credentials automatically.
// You do NOT need to put any credentials in appsettings.json.
// ---------------------------------------------------------------
builder.Services.AddSingleton<IAmazonSimpleNotificationService>(sp =>
    new AmazonSimpleNotificationServiceClient(Amazon.RegionEndpoint.USEast1));

builder.Services.AddSingleton(sp =>
{
    var sns = sp.GetRequiredService<IAmazonSimpleNotificationService>();

    // TODO: Paste your team's SNS Topic ARN here
    var topicArn = "arn:aws:sns:us-east-1:944596879679:IDA-Team1-order-events";

    return new EventPublisher(sns, topicArn);
});

builder.Services.AddControllers();

var app = builder.Build();

app.MapControllers();
app.UseRouting();

Console.WriteLine("===========================================");
Console.WriteLine("  ORDER SERVICE — Listening on port 5000");
Console.WriteLine("===========================================");

app.Run();
