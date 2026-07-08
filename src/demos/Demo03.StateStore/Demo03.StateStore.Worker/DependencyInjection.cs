using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo03.StateStore.Worker.Counter;
using Demo03.StateStore.Worker.Counter.GetCounter;
using Demo03.StateStore.Worker.Counter.IncrementCounter;
using Demo03.StateStore.Worker.Counter.ResetCounter;

namespace Demo03.StateStore.Worker;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDaprClient();

        services.AddSingleton(new CounterOptions(configuration.GetValue<bool>("USE_ETAGS")));
        services.AddSingleton<CounterStore>();
        services.AddSingleton<RunSignal>();

        services.AddScoped<ICommandHandler<IncrementCounterCommand, int>, IncrementCounterCommandHandler>();
        services.AddScoped<ICommandHandler<ResetCounterCommand, Unit>, ResetCounterCommandHandler>();
        services.AddScoped<IQueryHandler<GetCounterQuery, int>, GetCounterQueryHandler>();

        services.AddHostedService<CounterRunner>();

        return services;
    }
}
