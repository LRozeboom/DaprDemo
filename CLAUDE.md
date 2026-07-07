# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose & intent

Lunch-lecture demo material: four small, independent Dapr concept demos (pub/sub, retries, state store with ETags, output bindings) in one .NET 10 solution, orchestrated by a single .NET Aspire AppHost. The code is optimized for being shown live on a projector — demo resources are explicit-start ("click Start in the Aspire dashboard" is the talk's remote control), ports are fixed, and log lines are the payoff of each demo. Keep changes presentation-friendly: deterministic, readable, and minimal.

The README.md is the talk script — it documents how each demo runs, the curl commands, reset steps between dry runs, and known-unverified paths. Keep it in sync when changing demo behavior, ports, or resource names.

## Commands

```bash
dotnet build src/DaprDemos.sln          # build everything (warnings are errors)
dotnet run --project src/DaprDemos.AppHost   # launch: prints the Aspire dashboard URL
```

There are no test projects. Verification is manual: launch the AppHost (needs Docker running and `dapr init` done) and drive the demos with the curl commands in the README. `DEMO_AUTOSTART=true` before launch starts every demo immediately — useful for smoke-testing without clicking through the dashboard.

- `TreatWarningsAsErrors` is on solution-wide (`src/Directory.Build.props`).
- Package versions are centrally managed in `src/Directory.Packages.props`; `Aspire.Hosting.AppHost` is version-pinned via the `Aspire.AppHost.Sdk` in `global.json` and must NOT be added there.

## Architecture

### Orchestration (src/DaprDemos.AppHost)

`AppHost.cs` is the single composition root. It starts Redis (host port 6390, password `daprdemos`, TLS) and RabbitMQ (host port 5673) eagerly, then registers each demo app with a Dapr sidecar via `CommunityToolkit.Aspire.Hosting.Dapr`. Non-obvious, load-bearing details (all commented in the file):

- **Fixed non-default host ports** because `dapr init` owns 6379 and 5672 is commonly taken. The Dapr component YAMLs under `src/DaprDemos.AppHost/dapr/` hard-code these localhost addresses, so ports/credentials must match on both sides.
- **App health checks on sidecars are load-bearing**: sidecars start eagerly while apps are explicit-start; daprd only registers pub/sub subscriptions once the app's `/health` probe succeeds.
- Sidecars `WaitFor` the broker containers because daprd fails fatally if a component's backing service is unreachable at init.
- Demo 03 runs **two named resources of the same project** (not `WithReplicas`) so they can be started one at a time during the talk.

Dapr components live in `src/DaprDemos.AppHost/dapr/`: `pubsub.yaml` (Redis; `pubsub.rabbitmq.yaml.disabled` is the drop-in swap for demo 01 — the `.disabled` extension only prevents daprd loading two components named `pubsub`), `statestore.yaml`, `discord.yaml` (HTTP output binding), and `secretstore.yaml` (env-var secret store resolving `DISCORD_WEBHOOK_URL`).

### Shared projects

- **DaprDemos.ServiceDefaults** — standard Aspire service defaults (OpenTelemetry, health endpoints, service discovery) plus OpenAPI/Scalar. Every app calls `AddServiceDefaults()` and `MapDefaultEndpoints()`, which maps `/health`, `/alive`, and (in Development) `/openapi/v1.json` + `/scalar`.
- **DaprDemos.SharedKernel** — `Result<T>`/`Error` (functional result pattern) and `ICommandHandler`/`IQueryHandler` abstractions.
- **DaprDemos.Contracts** — pub/sub component name, topic names, and event records shared between publishers and subscribers.

### Demo apps (src/demos/)

All demos follow the same vertical-slice CQRS shape: minimal-API or controller endpoint → `ICommandHandler`/`IQueryHandler` → `Result<T>` mapped back to an HTTP status via `Match`. Each app has a `DependencyInjection.cs` registering its handlers. Demo 04 additionally splits into Domain/Application/Infrastructure/Api projects to demonstrate Clean Architecture — the application layer depends only on `INotifier`; the Dapr binding call lives in `DiscordBindingNotifier` in Infrastructure.

Conventions that carry meaning here:

- **Errors are values, never exceptions**: domain/application failures return `Result` failures with coded `Error`s (e.g. `Alert.EmptyTitle`); demo 02's whole point is that a failure `Result` → HTTP 500 → Dapr redelivery *is* the retry mechanism.
- **HTTPS redirection is deliberately absent** in every app — it breaks Dapr sidecar communication.
- Pub/sub subscribers use attribute-routed controllers with `[Topic]`; publishers use `DaprClient.PublishEventAsync` with names from `DaprDemos.Contracts`.
