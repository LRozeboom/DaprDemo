# Dapr lunch-lecture demos

Four small, independent Dapr concept demos in one .NET 10 solution, orchestrated by a single
.NET Aspire AppHost. Every demo resource is **explicit-start**: nothing runs until you click
*Start* on it in the Aspire dashboard.

| Demo | Concept | Resources | Fixed port(s) |
|------|---------|-----------|---------------|
| 01 PubSub | Pub/sub through the sidecar; swap Redis → RabbitMQ with zero code changes | `demo01-publisher`, `demo01-subscriber` | 5101, 5102 |
| 02 Retries | Non-2xx from a subscriber ⇒ Dapr redelivers; the Result pattern *is* the retry mechanism | `demo02-subscriber` | 5201 |
| 03 StateStore | Optimistic concurrency with ETags; without them concurrent writers lose updates | `demo03-worker-a`, `demo03-worker-b` | 5301, 5302 |
| 04 Bindings | HTTP output binding to Discord behind a Clean Architecture `INotifier` | `demo04-api` | 5401 |

Shared infrastructure starts eagerly so containers are warm before the talk:
Redis (`localhost:6390`, password `daprdemos`, TLS) and RabbitMQ (`localhost:5673`,
`guest`/`guest`, management UI at <http://localhost:15672>).

## Prerequisites

- Docker (Desktop) running
- .NET 10 SDK
- Dapr CLI, initialized (`dapr init`) — the sidecars use the default placement/scheduler services
- Optional, for demo 04: a Discord webhook URL in the `DISCORD_WEBHOOK_URL` environment variable
  (set it in the shell **before** launching the AppHost; it is never stored in the repo)

## Launch

```bash
dotnet run --project src/DaprDemos.AppHost
```

Open the dashboard URL printed in the console. All demo apps (and their Dapr sidecars) are
stopped; Redis and RabbitMQ come up automatically.

Dry-run tip: `DEMO_AUTOSTART=true` starts every demo immediately instead of explicit-start.

### Interactive API testing (Scalar)

Every demo app serves a [Scalar](https://scalar.com/) API reference at `/scalar`
(OpenAPI document at `/openapi/v1.json`) while running in Development — an alternative to the
`curl` commands below. Once an app is started in the dashboard, open:

- Demo 01: <http://localhost:5101/scalar> (publisher)
- Demo 02: <http://localhost:5201/scalar>
- Demo 03: <http://localhost:5301/scalar> (worker A) / <http://localhost:5302/scalar> (worker B)
- Demo 04: <http://localhost:5401/scalar>

### How a demo starts

Each app has a Dapr sidecar resource (`<name>-dapr-cli`). Sidecars wait for the broker
containers, then run with **app health checks** enabled: while the app is stopped the probe
fails, and the moment you start the app the sidecar sees it healthy and registers its pub/sub
subscriptions. So during the talk you only click *Start* on the app resources.

## Demo 01 — PubSub (Redis → RabbitMQ swap)

Start `demo01-publisher` and `demo01-subscriber` in the dashboard, then:

```bash
curl -X POST http://localhost:5101/greetings -H "Content-Type: application/json" -d "{\"message\":\"Hello from the audience!\"}"
```

Point at the `demo01-subscriber` console logs: `GREETING RECEIVED: "Hello from the audience!"`.
The publisher endpoint is plain HTTP — any language could publish this event; only the sidecar
knows Redis is behind it.

**The swap (zero code changes):**

1. Copy the RabbitMQ variant over the active component:
   ```powershell
   Copy-Item src/DaprDemos.AppHost/dapr/pubsub.rabbitmq.yaml.disabled src/DaprDemos.AppHost/dapr/pubsub.yaml -Force
   ```
   (The `.disabled` extension only exists so daprd never loads two components named `pubsub`.)
2. In the dashboard, restart the two sidecar resources `demo01-publisher-dapr-cli` and
   `demo01-subscriber-dapr-cli`. **The app processes keep running** — only the sidecars reload.
3. Run the same curl again. The greeting arrives exactly as before, and the message traffic is
   now visible in the RabbitMQ management UI at <http://localhost:15672> (guest/guest) —
   watch the `greetings` exchange / `demo01-subscriber-greetings` queue.

Restore afterwards: `git checkout -- src/DaprDemos.AppHost/dapr/pubsub.yaml`.

## Demo 02 — Retries via non-2xx

Start `demo02-subscriber`, then:

```bash
curl -X POST http://localhost:5201/fail-next/3
curl -X POST http://localhost:5201/publish
```

Watch the console logs: three ❌ `failing delivery on purpose — Dapr will redeliver` lines,
then the success line on attempt 4. With the Redis component, redeliveries arrive ~5–6 s apart
(`processingTimeout: 5s` + `redeliverInterval: 1s` in `pubsub.yaml`); after the demo 01 swap,
RabbitMQ redelivers instantly. The subscriber's handler returns a failure `Result`, the
controller maps it to HTTP 500, and Dapr does the rest — no retry code anywhere.

## Demo 03 — State store & ETag concurrency

Start `demo03-worker-a` and `demo03-worker-b`. Both increment the same state key
(`demo-counter`) 200 times per run.

```bash
curl -X POST http://localhost:5301/reset
curl -X POST http://localhost:5301/run & curl -X POST http://localhost:5302/run
# wait for both 🏁 "Run finished" log lines, then:
curl http://localhost:5301/counter
```

- Default (`USE_ETAGS=false`): the counter ends **well below 400** — plain read-modify-write
  loses updates. Each worker's 🏁 log line shows what it observed.
- Set `USE_ETAGS=true` (environment variable before launching the AppHost — it feeds the
  `use-etags` parameter) and repeat: the counter ends at **exactly 400**. The handler's ETag
  retry loop (`IncrementCounterCommandHandler`) is the only difference.

## Demo 04 — Output binding to Discord

Requires `DISCORD_WEBHOOK_URL` set before launch. Start `demo04-api`, then:

```bash
curl -i -X POST http://localhost:5401/alerts -H "Content-Type: application/json" -d "{\"title\":\"Lunch lecture\",\"message\":\"Dapr output bindings work!\"}"
```

`202 Accepted`, and the alert pops up in Discord. The application layer only knows
`INotifier`; the Discord/webhook details live in `DiscordBindingNotifier` in Infrastructure,
and the webhook URL itself lives only in the environment (resolved by the sidecar through the
`envvar-secrets` secret store component).

Validation failure path:

```bash
curl -i -X POST http://localhost:5401/alerts -H "Content-Type: application/json" -d "{\"title\":\"\",\"message\":\"no title\"}"
```

`400` with error code `Alert.EmptyTitle` — the domain's `Alert.Create` returned a failure
`Result` and nobody threw an exception.

## Reset between dry runs

- **Counter:** `curl -X POST http://localhost:5301/reset` (or delete the key directly:
  `docker exec <redis-container> redis-cli -p 6380 -a daprdemos DEL demo-counter` — port 6380
  is the container's plain-text port; 6379 is TLS).
- **Pub/sub component:** `git checkout -- src/DaprDemos.AppHost/dapr/pubsub.yaml` if you did the swap.
- **RabbitMQ queues:** purge via the management UI (Queues → purge) if a dry run left messages behind.
- **Demo 02 attempt counters** are in-memory — restarting `demo02-subscriber` clears them.
- Stopping the AppHost removes the containers; state does not survive between sessions.

## Environment notes

- Redis uses host port **6390** and RabbitMQ **5673** on purpose: `dapr init` already owns
  6379 (`dapr_redis`), and 5672 is commonly taken by other local projects.
- Aspire 13 starts the Redis container with TLS enabled (self-signed dev certificate); the
  Dapr components therefore set `enableTLS: "true"` (Dapr's Redis client does not verify the
  certificate).
- Component `initTimeout` is 120 s so a slow first container pull can't kill a sidecar.
