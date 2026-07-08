using Demo04.Bindings.Application.Notifications;
using Demo04.Bindings.Infrastructure.Notifications;
using Microsoft.Extensions.DependencyInjection;

namespace Demo04.Bindings.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDaprClient();

        services.AddScoped<INotifier, DiscordBindingNotifier>();

        return services;
    }
}
