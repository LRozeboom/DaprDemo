using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;
using Demo03.StateStore.Worker.Counter.RunIncrements;
using Microsoft.AspNetCore.Mvc;

namespace Demo03.StateStore.Worker.Controllers;

[ApiController]
public sealed class CounterController(
    ICommandHandler<RunIncrementsCommand, RunSummary> runHandler,
    IQueryHandler<GetCounterQuery, int> getHandler,
    ICommandHandler<ResetCounterCommand, Unit> resetHandler) : ControllerBase
{
    [HttpPost("/run")]
    public async Task<IActionResult> RunAsync(CancellationToken cancellationToken)
    {
        var result = await runHandler.HandleAsync(new RunIncrementsCommand(), cancellationToken);

        return result.Match<IActionResult>(
            Ok,
            error => BadRequest(new { error.Code, error.Message }));
    }

    [HttpGet("/counter")]
    public async Task<IActionResult> GetCounterAsync(CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetCounterQuery(), cancellationToken);

        return result.Match<IActionResult>(
            value => Ok(new { value }),
            error => BadRequest(new { error.Code, error.Message }));
    }

    [HttpPost("/reset")]
    public async Task<IActionResult> ResetCounterAsync(CancellationToken cancellationToken)
    {
        var result = await resetHandler.HandleAsync(new ResetCounterCommand(), cancellationToken);

        return result.Match<IActionResult>(
            _ => Ok(new { value = 0 }),
            error => BadRequest(new { error.Code, error.Message }));
    }
}
