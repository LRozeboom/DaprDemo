using DaprDemos.SharedKernel.Messaging;
using Demo02.Retries.Subscriber;
using Demo02.Retries.Subscriber.FlakyMessages;
using Demo02.Retries.Subscriber.FlakyMessages.PublishFlakyMessage;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddControllers().AddDapr();

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.UseCloudEvents();
app.MapSubscribeHandler();
app.MapControllers();

app.MapPost("/publish", async (
    ICommandHandler<PublishFlakyMessageCommand, Guid> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(
        new PublishFlakyMessageCommand($"Hello from Demo 02 at {DateTimeOffset.UtcNow:HH:mm:ss}"),
        cancellationToken);

    return result.Match(
        id => Results.Ok(new { id }),
        error => Results.BadRequest(new { error.Code, error.Message }));
});

app.MapPost("/fail-next/{count:int}", (int count, FailureToggle failureToggle) =>
{
    failureToggle.FailNext(count);
    return Results.Ok(new { armedFailures = count });
});

app.Run();
