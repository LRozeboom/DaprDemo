using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker;
using Demo03.StateStore.Worker.Counter;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddApplication(builder.Configuration);

var app = builder.Build();

app.MapDefaultEndpoints();

// HTTPS redirection is deliberately absent: it breaks Dapr sidecar communication.

app.MapPost("/run", (RunSignal runSignal) =>
{
    runSignal.Trigger();
    return Results.Accepted(value: new { iterations = CounterRunner.Iterations });
});

app.MapGet("/counter", async (
    IQueryHandler<GetCounterQuery, int> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new GetCounterQuery(), cancellationToken);

    return result.Match(
        value => Results.Ok(new { value }),
        error => Results.BadRequest(new { error.Code, error.Message }));
});

app.MapPost("/reset", async (
    ICommandHandler<ResetCounterCommand, Unit> handler,
    CancellationToken cancellationToken) =>
{
    var result = await handler.HandleAsync(new ResetCounterCommand(), cancellationToken);

    return result.Match(
        _ => Results.Ok(new { value = 0 }),
        error => Results.BadRequest(new { error.Code, error.Message }));
});

app.Run();
