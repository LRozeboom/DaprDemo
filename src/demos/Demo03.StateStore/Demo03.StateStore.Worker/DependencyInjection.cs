using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.IncrementCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;

namespace Demo03.StateStore.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddDaprClient();

        services.AddSingleton<CounterStore>();

        services.AddScoped<IQueryHandler<GetCounterQuery, CounterState>, GetCounterQueryHandler>();
        services.AddScoped<ICommandHandler<IncrementCounterCommand, int>, IncrementCounterCommandHandler>();
        services.AddScoped<ICommandHandler<ResetCounterCommand, Unit>, ResetCounterCommandHandler>();

        return services;
    }
}
