using Dapr;
using DaprDemos.Contracts.Messaging;
using DaprDemos.Contracts.Messaging.Events;
using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Outbox.Worker.Orders;
using Demo04.Outbox.Worker.Orders.GetOrder;
using Demo04.Outbox.Worker.Orders.HandleOrderPlaced;
using Demo04.Outbox.Worker.Orders.PlaceOrder;
using Microsoft.AspNetCore.Mvc;

namespace Demo04.Outbox.Worker.Controllers;

[ApiController]
public sealed class OrdersController(
    ICommandHandler<PlaceOrderCommand, Guid> placeHandler,
    IQueryHandler<GetOrderQuery, OrderRecord> getHandler,
    ICommandHandler<HandleOrderPlacedCommand, Unit> handleHandler) : ControllerBase
{
    [HttpPost("/orders")]
    public async Task<IActionResult> PlaceOrderAsync(PlaceOrderRequest request, CancellationToken cancellationToken)
    {
        var result = await placeHandler.HandleAsync(
            new PlaceOrderCommand(request.Customer, request.Amount, request.FailDeliveries, request.ForceConflict),
            cancellationToken);

        // 202, not 200: the row is committed, but the event is still on its way to subscribers.
        return result.Match<IActionResult>(
            id => Accepted(new { id }),
            error => error.Code == OrderErrors.TransactionRejectedCode
                ? Conflict(new { error.Code, error.Message })
                : BadRequest(new { error.Code, error.Message }));
    }

    [HttpGet("/orders/{id:guid}")]
    public async Task<IActionResult> GetOrderAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await getHandler.HandleAsync(new GetOrderQuery(id), cancellationToken);

        return result.Match<IActionResult>(
            Ok,
            error => NotFound(new { error.Code, error.Message }));
    }

    [Topic(PubSub.Name, Topics.Orders)]
    [HttpPost("/orders-handler")]
    public async Task<IActionResult> HandleOrderPlacedAsync(
        OrderPlacedEvent orderPlaced,
        CancellationToken cancellationToken)
    {
        var result = await handleHandler.HandleAsync(orderPlaced.ToCommand(), cancellationToken);

        // Same contract as demo 02: non-2xx tells Dapr to redeliver.
        return result.Match<IActionResult>(
            _ => Ok(),
            error => StatusCode(StatusCodes.Status500InternalServerError, error));
    }
}
