using System.Text.Json.Serialization;
using Dapr;
using Dapr.Client;
using DaprDemos.Contracts.Messaging;
using DaprDemos.SharedKernel.Results;
using Demo04.Bindings.Application.Notifications;
using Demo04.Bindings.Domain.Alerts;
using Microsoft.Extensions.Logging;

namespace Demo04.Bindings.Infrastructure.Notifications;

// The vendor details (binding component name, Discord webhook payload shape) live here on
// purpose: the Application layer only knows INotifier, so replacing Discord is an
// Infrastructure-only change. Mirrors the production DiscordBindingNotifier.
public sealed class DiscordBindingNotifier(
    DaprClient daprClient,
    ILogger<DiscordBindingNotifier> logger) : INotifier
{
    private const string CreateOperation = "create";

    public async Task<Result<Unit>> NotifyAsync(Alert alert, CancellationToken cancellationToken)
    {
        var payload = new DiscordWebhookPayload($"**{alert.Title}**\n{alert.Message}");
        var metadata = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        try
        {
            await daprClient.InvokeBindingAsync(Components.Discord, CreateOperation, payload, metadata, cancellationToken);
            return Unit.Value;
        }
        catch (Exception exception) when (exception is DaprException or InvalidOperationException)
        {
            logger.LogError(exception, "Failed to deliver alert through the {Binding} output binding", Components.Discord);
            return NotifierErrors.DeliveryFailed(exception.Message);
        }
    }

    private sealed record DiscordWebhookPayload([property: JsonPropertyName("content")] string Content);
}
