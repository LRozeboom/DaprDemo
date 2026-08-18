using Dapr;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;
using Demo02.Retries.Subscriber.FlakyMessages.PublishFlakyMessage;
using Microsoft.AspNetCore.Mvc;

namespace Demo02.Retries.Subscriber.Controllers;

[ApiController]
public sealed class FlakyMessagesController(
    ICommandHandler<PublishFlakyMessageCommand, Guid> publishHandler,
    ICommandHandler<HandleFlakyMessageCommand, Unit> handler) : ControllerBase
{
    [HttpPost("/publish")]
    public async Task<IActionResult> PublishFlakyMessageAsync(CancellationToken cancellationToken)
    {
        var result = await publishHandler.HandleAsync(
            new PublishFlakyMessageCommand($"Hello from Demo 02 at {DateTimeOffset.UtcNow:HH:mm:ss}"),
            cancellationToken);

        return result.Match<IActionResult>(
            id => Ok(new { id }),
            error => BadRequest(new { error.Code, error.Message }));
    }

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
