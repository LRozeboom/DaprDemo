using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Outbox.Worker.Orders;
using Demo04.Outbox.Worker.Orders.GetOrder;
using Demo04.Outbox.Worker.Orders.HandleOrderPlaced;
using Demo04.Outbox.Worker.Orders.PlaceOrder;

namespace Demo04.Outbox.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddDaprClient();

        services.AddSingleton<OrderStore>();
        services.AddSingleton<OrderDeliveryPlan>();

        services.AddScoped<ICommandHandler<PlaceOrderCommand, Guid>, PlaceOrderCommandHandler>();
        services.AddScoped<IQueryHandler<GetOrderQuery, OrderRecord>, GetOrderQueryHandler>();
        services.AddScoped<ICommandHandler<HandleOrderPlacedCommand, Unit>, HandleOrderPlacedCommandHandler>();

        return services;
    }
}
