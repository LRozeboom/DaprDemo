using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;

namespace Demo04.Outbox.Worker.Orders.GetOrder;

public sealed class GetOrderQueryHandler(OrderStore orderStore) : IQueryHandler<GetOrderQuery, OrderRecord>
{
    public async Task<Result<OrderRecord>> HandleAsync(GetOrderQuery query, CancellationToken cancellationToken)
    {
        var order = await orderStore.GetAsync(query.Id, cancellationToken);

        // The proof for the rollback run: a rejected transaction leaves nothing behind here either.
        return order is null
            ? OrderErrors.NotFound(query.Id)
            : order;
    }
}
