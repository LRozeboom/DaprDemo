using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo01.PubSub.Subscriber.Greetings.HandleGreeting;

namespace Demo01.PubSub.Subscriber;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<ICommandHandler<HandleGreetingCommand, Unit>, HandleGreetingCommandHandler>();

        return services;
    }
}
