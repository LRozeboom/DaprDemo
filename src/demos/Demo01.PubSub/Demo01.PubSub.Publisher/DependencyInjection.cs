using Demo01.PubSub.Publisher.Greetings.PublishGreeting;

namespace Demo01.PubSub.Publisher;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddDaprClient();

        services.AddScoped<ICommandHandler<PublishGreetingCommand, Guid>, PublishGreetingCommandHandler>();

        return services;
    }
}
