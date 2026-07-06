using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Application.Alerts.RaiseAlert;
using Microsoft.Extensions.DependencyInjection;

namespace Demo04.Bindings.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);

        services.AddScoped<ICommandHandler<RaiseAlertCommand, Unit>, RaiseAlertCommandHandler>();

        return services;
    }
}
