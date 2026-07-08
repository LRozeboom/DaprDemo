using DaprDemos.SharedKernel.Messaging;
using Demo01.PubSub.Publisher;
using Demo01.PubSub.Publisher.Greetings.PublishGreeting;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.MapPost("/greetings", async (
    PublishGreetingRequest request,
    ICommandHandler<PublishGreetingCommand, Guid> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new PublishGreetingCommand(request.Message), cancellationToken);

    return result.Match(
        id => Results.Ok(new { id }),
        error => Results.BadRequest(new { error.Code, error.Message }));
});

app.Run();
