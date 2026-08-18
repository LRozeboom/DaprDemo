using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.IncrementCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;
using Microsoft.AspNetCore.Mvc;

namespace Demo03.StateStore.Worker.Controllers;

[ApiController]
public sealed class CounterController(
    IQueryHandler<GetCounterQuery, CounterState> getHandler,
    ICommandHandler<IncrementCounterCommand, int> incrementHandler,
    ICommandHandler<ResetCounterCommand, Unit> resetHandler) : ControllerBase
{
    [HttpGet("/counter")]
    public async Task<IActionResult> GetCounterAsync(CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetCounterQuery(), cancellationToken);

        return result.Match<IActionResult>(
            state => Ok(new { value = state.Value, etag = state.ETag }),
            error => BadRequest(new { error.Code, error.Message }));
    }

    [HttpPost("/counter/increment")]
    public async Task<IActionResult> IncrementCounterAsync(CancellationToken cancellationToken)
    {
        var result = await incrementHandler.HandleAsync(new IncrementCounterCommand(), cancellationToken);

        return result.Match<IActionResult>(
            value => Ok(new { value }),
            error => BadRequest(new { error.Code, error.Message }));
    }

    [HttpPost("/counter/reset")]
    public async Task<IActionResult> ResetCounterAsync(CancellationToken cancellationToken)
    {
        var result = await resetHandler.HandleAsync(new ResetCounterCommand(), cancellationToken);

        return result.Match<IActionResult>(
            _ => Ok(new { value = 0 }),
            error => BadRequest(new { error.Code, error.Message }));
    }
}
