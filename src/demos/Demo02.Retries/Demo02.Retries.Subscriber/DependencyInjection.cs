using DaprDemos.SharedKernel.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo02.Retries.Subscriber.FlakyMessages;
using Demo02.Retries.Subscriber.FlakyMessages.HandleFlakyMessage;
using Demo02.Retries.Subscriber.FlakyMessages.PublishFlakyMessage;

namespace Demo02.Retries.Subscriber;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddDaprClient();

        services.AddSingleton<FailureToggle>();
        services.AddSingleton<DeliveryAttempts>();

        services.AddScoped<ICommandHandler<PublishFlakyMessageCommand, Guid>, PublishFlakyMessageCommandHandler>();
        services.AddScoped<ICommandHandler<HandleFlakyMessageCommand, Unit>, HandleFlakyMessageCommandHandler>();

        return services;
    }
}
