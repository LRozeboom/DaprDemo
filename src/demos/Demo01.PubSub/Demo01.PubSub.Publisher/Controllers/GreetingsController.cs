using DaprDemos.SharedKernel.Messaging;
using Demo01.PubSub.Publisher.Greetings.PublishGreeting;
using Microsoft.AspNetCore.Mvc;

namespace Demo01.PubSub.Publisher.Controllers;

[ApiController]
public sealed class GreetingsController(
    ICommandHandler<PublishGreetingCommand, Guid> handler) : ControllerBase
{
    [HttpPost("/greetings")]
    public async Task<IActionResult> PublishGreetingAsync(
        PublishGreetingRequest request,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(new PublishGreetingCommand(request.Message), cancellationToken);

        return result.Match<IActionResult>(
            id => Ok(new { id }),
            error => BadRequest(new { error.Code, error.Message }));
    }
}
