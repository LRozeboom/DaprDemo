using DaprDemos.SharedKernel.Results;

namespace Demo04.Bindings.Domain.Alerts;

public sealed record Alert(string Title, string Message, DateTimeOffset RaisedAt)
{
    public static Result<Alert> Create(string title, string message, DateTimeOffset raisedAt)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return AlertErrors.EmptyTitle();
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            return AlertErrors.EmptyMessage();
        }

        return new Alert(title.Trim(), message.Trim(), raisedAt);
    }
}
