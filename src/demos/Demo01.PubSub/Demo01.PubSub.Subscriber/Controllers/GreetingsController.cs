using Dapr;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Results;
using Demo01.PubSub.Subscriber.Greetings.HandleGreeting;
using Microsoft.AspNetCore.Mvc;
// Alias needed: the Demo01.PubSub.* namespace shadows the PubSub constants class.
using Messaging = DaprDemos.Contracts.Messaging;

namespace Demo01.PubSub.Subscriber.Controllers;

[ApiController]
public sealed class GreetingsController(
    ICommandHandler<HandleGreetingCommand, Unit> handler) : ControllerBase
{
    [Topic(Messaging.PubSub.Name, Topics.Greetings)]
    [HttpPost("/greetings-handler")]
    public async Task<IActionResult> HandleGreetingAsync(
        GreetingSubmittedEvent greetingSubmitted,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(greetingSubmitted.ToCommand(), cancellationToken);

        // A failure Result maps to non-2xx on purpose: non-2xx makes Dapr redeliver the message.
        return result.Match<IActionResult>(
            _ => Ok(),
            error => StatusCode(StatusCodes.Status500InternalServerError, error));
    }
}
