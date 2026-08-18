using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;
using Demo03.StateStore.Worker.Counter.RunIncrements;
using Microsoft.AspNetCore.Mvc;

namespace Demo03.StateStore.Worker.Controllers;

[ApiController]
public sealed class CounterController(
    RunSignal runSignal,
    IQueryHandler<GetCounterQuery, int> getHandler,
    ICommandHandler<ResetCounterCommand, Unit> resetHandler) : ControllerBase
{
    // Returns as soon as the run is queued: two curls issued back to back leave both workers
    // running at the same time, which is what puts the two processes in contention.
    [HttpPost("/run")]
    public IActionResult Run()
    {
        runSignal.Trigger();

        return Accepted(new
        {
            iterations = RunIncrementsCommandHandler.Iterations,
            concurrency = RunIncrementsCommandHandler.Concurrency
        });
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
