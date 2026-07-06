using Dapr;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Results;
using Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;
using Microsoft.AspNetCore.Mvc;

namespace Demo02.Retries.Subscriber.Controllers;

[ApiController]
public sealed class FlakyMessagesController(
    ICommandHandler<HandleFlakyMessageCommand, Unit> handler) : ControllerBase
{
    [Topic(PubSub.Name, Topics.FlakyMessages)]
    [HttpPost("/flaky-messages-handler")]
    public async Task<IActionResult> HandleFlakyMessageAsync(
        FlakyMessageEvent flakyMessage,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(flakyMessage.ToCommand(), cancellationToken);

        // A failure Result maps to non-2xx on purpose: non-2xx makes Dapr redeliver the message.
        return result.Match<IActionResult>(
            _ => Ok(),
            error => StatusCode(StatusCodes.Status500InternalServerError, error));
    }
}
