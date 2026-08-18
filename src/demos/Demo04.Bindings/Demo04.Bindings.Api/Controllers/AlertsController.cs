using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Api.Alerts;
using Demo04.Bindings.Application.Alerts.RaiseAlert;
using Microsoft.AspNetCore.Mvc;

namespace Demo04.Bindings.Api.Controllers;

[ApiController]
public sealed class AlertsController(
    ICommandHandler<RaiseAlertCommand, Unit> handler) : ControllerBase
{
    [HttpPost("/alerts")]
    public async Task<IActionResult> RaiseAlertAsync(
        RaiseAlertRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new RaiseAlertCommand(request.Title, request.Message), cancellationToken);

        return result.Match<IActionResult>(
            _ => Accepted(),
            error => BadRequest(new { error.Code, error.Message }));
    }
}
