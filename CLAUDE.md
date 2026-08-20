# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Purpose & intent

Lunch-lecture demo material: four small Dapr concept demos (pub/sub, retries, state store with ETags, transactional outbox) in one .NET 10 solution, orchestrated by a single .NET Aspire AppHost. The code is optimized for being shown live on a projector — one `dotnet run` starts everything, ports are fixed, and log lines are the payoff of each demo. Keep changes presentation-friendly: deterministic, readable, and minimal.

The README.md is the talk script — it documents how each demo runs, the curl commands, reset steps between dry runs, and known-unverified paths. Keep it in sync when changing demo behavior, ports, or resource names.

## Commands

```bash
dotnet build src/DaprDemos.slnx         # build everything (warnings are errors)
dotnet run --project src/DaprDemos.AppHost   # launch: prints the Aspire dashboard URL
```

There are no test projects. Verification is manual: launch the AppHost (needs Docker running and `dapr init` done), wait for every resource to go green, and drive the demos with the curl commands in the README.

- `TreatWarningsAsErrors` is on solution-wide (`src/Directory.Build.props`).
- Package versions are centrally managed in `src/Directory.Packages.props`; `Aspire.Hosting.AppHost` is version-pinned via the `Aspire.AppHost.Sdk` in `global.json` and must NOT be added there.

## Architecture

### Orchestration (src/DaprDemos.AppHost)

`AppHost.cs` is the single composition root. It starts Redis (host port 6390, password `daprdemos`, TLS), RabbitMQ (host port 5673) and Postgres (host port 5433, `postgres`/`daprdemos`) eagerly, then registers each demo app with a Dapr sidecar via `CommunityToolkit.Aspire.Hosting.Dapr`. Non-obvious, load-bearing details (all commented in the file):

- **Fixed non-default host ports** because `dapr init` owns 6379, and 5672/5432 are commonly taken. The Dapr component YAMLs under `src/DaprDemos.AppHost/dapr/` hard-code these localhost addresses, so ports/credentials must match on both sides.
- **App health checks on sidecars are load-bearing**: a sidecar can be up before its app is listening, and daprd only registers pub/sub subscriptions once the app's `/health` probe succeeds — which is also what lets a single app be restarted from the dashboard mid-talk.
- Sidecars `WaitFor` the broker containers because daprd fails fatally if a component's backing service is unreachable at init.
- Demo 03 is a single worker (`demo03-worker`, port 5301) with three state-backed endpoints. It was deliberately reduced from a two-worker concurrency setup: the point is that Dapr abstracts the state store away, and the contention apparatus distracted from it. Don't add load generators or extra workers back.
- Demo 04 (`demo04-outbox`, port 5401) is one app that both places orders and subscribes to them, like demo 02. It replaced an output-binding-to-Discord demo so the four demos build on each other: it is state (03) + pub/sub (01) in one flow. It was deliberately stripped of its failure machinery (a `failDeliveries` switch that made the consumer reject deliveries, and a `forceConflict` switch that wrote a stale ETag to force a rollback): the demo is a walkthrough of *setting up* an outbox store, and the forced failures buried that. Don't add failure switches, arming endpoints, or a rollback path back — the rollback guarantee is a talking point in the README, not a live run.

Dapr components live in `src/DaprDemos.AppHost/dapr/`: `pubsub.yaml` (Redis; `pubsub.rabbitmq.yaml.disabled` is the drop-in swap for demo 01 — the `.disabled` extension only prevents daprd loading two components named `pubsub`), `statestore.yaml` (demo 03, Redis) and `outboxstore.yaml` (demo 04, Postgres). The same folder also holds `resiliency.yaml` (a `Resiliency` spec, not a component) — daprd loads it from the resources path.

Several YAML settings are load-bearing for the demos and easy to break:

- `statestore.yaml` sets `keyPrefix: none` so the key in Redis is the literal `demo-counter` the code names, rather than the default per-app-id `demo03-worker||demo-counter` — it keeps the redis-cli inspection in the README honest.
- `resiliency.yaml` defines the visible retries (`constant`, 2 s, max 10, inbound on the `pubsub` component, scoped to `demo02-subscriber` only — retries are demo 02's story, not demo 04's). `pubsub.yaml`'s `processingTimeout: 60s` / `redeliverInterval: 15s` are deliberately set well above the ~20 s retry window so Redis's reclaim loop never delivers a duplicate on top of an in-flight retry.
- `outboxstore.yaml` is the whole of demo 04: `outboxPublishPubsub: pubsub` + `outboxPublishTopic: orders` are the two lines that turn a state transaction into a published event — that pair, plus the store type, is what the demo walks the audience through. It is **Postgres on purpose** — Dapr's Redis state store cannot roll a transaction back, so the outbox marker row would survive a failed write and the event would be published anyway, which is precisely the bug the demo claims to prevent.

### Shared projects

- **DaprDemos.ServiceDefaults** — standard Aspire service defaults (OpenTelemetry, health endpoints, service discovery) plus OpenAPI/Scalar. Every app calls `AddServiceDefaults()` and `MapDefaultEndpoints()`, which maps `/health`, `/alive`, and (in Development) `/openapi/v1.json` + `/scalar`.
- **DaprDemos.SharedKernel** — `Result<T>`/`Error` (functional result pattern) and `ICommandHandler`/`IQueryHandler` abstractions.
- **DaprDemos.Contracts** — pub/sub component name, topic names, and event records shared between publishers and subscribers.

### Demo apps (src/demos/)

All demos follow the same vertical-slice CQRS shape: attribute-routed controller action (in each app's `Controllers/` folder; no minimal APIs — every app calls `AddControllers()` + `MapControllers()`) → `ICommandHandler`/`IQueryHandler` → `Result<T>` mapped back to an HTTP status via `Match`. Each app has a `DependencyInjection.cs` registering its handlers.

Conventions that carry meaning here:

- **Errors are values, never exceptions**: domain/application failures return `Result` failures with coded `Error`s (e.g. `Order.EmptyCustomer`); demo 02's whole point is that a failure `Result` → HTTP 500 → Dapr retry *is* the retry mechanism. Demo 02 has no arming endpoint: `FlakyDeliveryPlan` (singleton) rolls a random 1–5 planned failures per message id on first delivery and counts attempts, so `/publish` alone produces the retry sequence in the logs.
- **HTTPS redirection is deliberately absent** in every app — it breaks Dapr sidecar communication.
- Pub/sub subscriber actions carry `[Topic]` (those apps also need `AddControllers().AddDapr()` plus `UseCloudEvents()`/`MapSubscribeHandler()`); publishers use `DaprClient.PublishEventAsync` with names from `DaprDemos.Contracts`. Demo 04 is the exception and must stay one: it never publishes explicitly — `OrderStore.CommitAsync` puts the event in the state transaction as an `outbox.projection` operation, and its `contentType: application/json` metadata is load-bearing (without it Dapr publishes the payload as `text/plain` and the subscriber receives a JSON string instead of an object).
