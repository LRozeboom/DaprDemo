using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Api.Alerts;
using Demo04.Bindings.Application;
using Demo04.Bindings.Application.Alerts.RaiseAlert;
using Demo04.Bindings.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.MapPost("/alerts", async (
    RaiseAlertRequest request,
    ICommandHandler<RaiseAlertCommand, Unit> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new RaiseAlertCommand(request.Title, request.Message), cancellationToken);

    return result.Match(
        _ => Results.Accepted(),
        error => Results.BadRequest(new { error.Code, error.Message }));
});

app.Run();
