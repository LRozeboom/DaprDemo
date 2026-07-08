using DaprDemos.SharedKernel.Results;

namespace Demo01.PubSub.Publisher.Greetings;

public static class GreetingErrors
{
    public static Error EmptyMessage() =>
        new("Greeting.EmptyMessage", "A greeting message must not be empty.");
}
