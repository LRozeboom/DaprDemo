# Dapr lunch-lecture demos

Four small, independent Dapr concept demos in one .NET 10 solution, orchestrated by a single
.NET Aspire AppHost. Every demo resource is **explicit-start**: nothing runs until you click
*Start* on it in the Aspire dashboard.

| Demo | Concept | Resources | Fixed port(s) |
|------|---------|-----------|---------------|
| 01 PubSub | Pub/sub through the sidecar; swap Redis → RabbitMQ with zero code changes | `demo01-publisher`, `demo01-subscriber` | 5101, 5102 |
| 02 Retries | Non-2xx from a subscriber ⇒ Dapr retries per a resiliency policy; the Result pattern *is* the retry mechanism | `demo02-subscriber` | 5201 |
| 03 StateStore | State through the sidecar — no Redis client in the app; ETags for optimistic concurrency | `demo03-worker` | 5301 |
| 04 Outbox | Transactional outbox: one state transaction stores the order **and** publishes the event | `demo04-outbox` | 5401 |

Demo 04 is the finale: it combines all three earlier concepts — state (03) + pub/sub (01) +
retries (02) — into the pattern that makes event-driven architecture safe.

Shared infrastructure starts eagerly so containers are warm before the talk:
Redis (`localhost:6390`, password `daprdemos`, TLS), RabbitMQ (`localhost:5673`,
`guest`/`guest`, management UI at <http://localhost:15672>) and Postgres (`localhost:5433`,
`postgres`/`daprdemos`, database `postgres` — demo 04's outbox store).

## Prerequisites

- Docker (Desktop) running
- .NET 10 SDK
- Dapr CLI, initialized (`dapr init`) — the sidecars use the default placement/scheduler services

## Launch

```bash
dotnet run --project src/DaprDemos.AppHost
```

Open the dashboard URL printed in the console. All demo apps (and their Dapr sidecars) are
stopped; Redis, RabbitMQ and Postgres come up automatically.

Dry-run tip: `DEMO_AUTOSTART=true` starts every demo immediately instead of explicit-start.

### Interactive API testing (Scalar)

Every demo app serves a [Scalar](https://scalar.com/) API reference at `/scalar`
(OpenAPI document at `/openapi/v1.json`) while running in Development — an alternative to the
`curl` commands below. Once an app is started in the dashboard, open:

- Demo 01: <http://localhost:5101/scalar> (publisher)
- Demo 02: <http://localhost:5201/scalar>
- Demo 03: <http://localhost:5301/scalar>
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
curl -X POST http://localhost:5201/publish
```

There is nothing to arm first: every message rolls its own dice. `FlakyDeliveryPlan` picks a
random **1–5** deliveries to fail per message id and counts the attempts, so the log tells the
whole story:

```text
Failed Attempt 1 of 3 for message <id>: failing delivery on purpose — Dapr will redeliver
Failed Attempt 2 of 3 ...
Failed Attempt 3 of 3 ...
Succeeded Attempt 4: processed message <id> after 3 failed deliveries
```

The retries come from `dapr/resiliency.yaml` — a `constant` policy, **2 s apart, max 10
retries**, scoped to `demo02-subscriber` and bound to *inbound* deliveries of the `pubsub`
component. Publish again to get a different number of failures. The subscriber's handler
returns a failure `Result`, the controller maps it to HTTP 500, and Dapr does the rest — no
retry code anywhere in the app.

The broker's own redelivery is only a backstop: `pubsub.yaml` sets `processingTimeout: 60s` and
`redeliverInterval: 15s`, comfortably above the ~20 s the retry policy can hold a message, so
Redis never delivers a duplicate on top of a retry that is still running.

## Demo 03 — State store

Start `demo03-worker`. Three endpoints, all backed by the Dapr state store — one counter under
the key `demo-counter`:

```bash
curl http://localhost:5301/counter                    # read
curl -X POST http://localhost:5301/counter/increment  # read-modify-write
curl -X POST http://localhost:5301/counter/reset      # back to 0
```

```json
{"value":3,"etag":"3"}
```

The point is what the app *doesn't* contain: no Redis client, no connection string, no
serializer. `CounterStore` is the entire storage layer, and it names a component and a key —
nothing else. Swapping Redis for Postgres or Cosmos is an edit to `statestore.yaml` with no code
touched.

**ETags:** `/counter` hands back the ETag the store currently holds for the key, and it changes on
every write. `IncrementCounterCommandHandler` reads the value *together with* its ETag and writes
back only if that ETag still matches, so a concurrent writer cannot be silently overwritten — the
store rejects the write, and the handler re-reads and tries again (up to `MaxAttempts`). That is
optimistic concurrency you get from the state store instead of writing yourself.

Talking point: the value survives restarts of the app but not of the Redis container — stopping
the AppHost takes the state with it.

## Demo 04 — Transactional outbox

The finale, and the one that ties demos 01-03 together. Start `demo04-outbox`, then place an
order:

```bash
curl -i -X POST http://localhost:5401/orders -H "Content-Type: application/json" -d "{\"customer\":\"Ada Lovelace\",\"amount\":42.50}"
```

`202 Accepted` with the order id, and about a second later the subscriber logs:

```text
Committed order <id> for Ada Lovelace (42.50) under key order-<id> — the OrderPlaced event rode along in the same transaction
ORDER RECEIVED <id>: Ada Lovelace for 42.50 — the state store already has it as 'Placed' (attempt 1)
```

**The problem this solves.** An order service normally does two writes: save the order to the
database, then publish `OrderPlaced` to the broker. There is no transaction spanning both. Crash
between them and you either have an order nobody hears about, or — if you publish first — an
event for an order that does not exist. The classic fix is an outbox table plus a relay process
you write and operate yourself.

**What the app does instead.** `OrderStore.CommitAsync` runs *one* Dapr state transaction with
two operations on the same key:

1. the order row that gets stored, and
2. the same key marked `outbox.projection: true` — never written to the store, it only says what
   the published message should look like (which is why `OrderPlacedEvent` on the topic is
   narrower than the row in the database).

There is no `PublishEventAsync` anywhere in demo 04. Dapr writes an extra marker row inside that
same transaction, and its sidecar publishes the event to the `orders` topic only after the
transaction commits — so "row stored" and "event published" cannot come apart. The entire pattern
is four lines of metadata in `dapr/outboxstore.yaml`:

```yaml
- name: outboxPublishPubsub    # which pub/sub component to publish on
  value: pubsub
- name: outboxPublishTopic     # which topic subscribers listen to
  value: orders
```

Check the row that was stored (state, demo 03) — and note the event carried no `status` field:

```bash
curl http://localhost:5401/orders/<id>
```

```json
{"id":"...","customer":"Ada Lovelace","amount":42.50,"placedAt":"...","status":"Placed"}
```

### The rollback: no ghost events

Same endpoint, same code path, but the state write is made to fail — the transaction carries a
stale ETag, exactly the conflict demo 03 guards against:

```bash
curl -i -X POST http://localhost:5401/orders -H "Content-Type: application/json" -d "{\"customer\":\"Ada Lovelace\",\"amount\":42.50,\"forceConflict\":true}"
```

`409 Conflict` with `Order.TransactionRejected`, and then **nothing**: no `ORDER RECEIVED` line,
ever. The transaction rolled back, so the marker row never existed, and ~10 s later the sidecar
logs that it discarded the pending message (`outbox state not found ... discarding message`).
That is the whole promise of the pattern in one curl: a write that fails publishes nothing.

> This is why demo 04's store is Postgres and not Redis. Dapr's Redis state store runs a
> "transaction" as a pipeline and cannot roll back — the marker row would survive the failed
> write and the event would go out anyway. Swapping the store is still a YAML edit (demo 03's
> point), but *which* store you pick decides whether you actually get atomicity.

### At-least-once, so consumers must cope (demo 02, again)

The outbox guarantees the event is delivered *at least* once, so the consumer has to survive a
failed delivery. Ask an order to fail its first two deliveries:

```bash
curl -i -X POST http://localhost:5401/orders -H "Content-Type: application/json" -d "{\"customer\":\"Grace Hopper\",\"amount\":99.00,\"failDeliveries\":2}"
```

```text
Failed Attempt 1 of 2 for order <id>: failing delivery on purpose — Dapr will redeliver
Failed Attempt 2 of 2 ...
ORDER RECEIVED <id>: Grace Hopper for 99.00 — the state store already has it as 'Placed' (attempt 3)
```

Those retries are the same `resiliency.yaml` policy demo 02 uses, now scoped to `demo04-outbox`
as well. State + pub/sub + retries, one flow, no infrastructure code in the app.

### Inspecting the outbox in Postgres

```bash
docker exec -it <postgres-container> psql -U postgres -c "select key, value from state;"
```

The `order-<id>` rows are the orders. Marker rows named `outbox-<uuid>` appear inside the
transaction and are deleted by the sidecar the moment the event has been published — catch one by
running the query immediately after a POST.

## Reset between dry runs

- **Counter:** `curl -X POST http://localhost:5301/counter/reset` (or delete the key directly:
  `docker exec <redis-container> redis-cli -p 6380 -a daprdemos DEL demo-counter` — port 6380
  is the container's plain-text port; 6379 is TLS).
- **Pub/sub component:** `git checkout -- src/DaprDemos.AppHost/dapr/pubsub.yaml` if you did the swap.
- **RabbitMQ queues:** purge via the management UI (Queues → purge) if a dry run left messages behind.
- **Demo 02** needs no reset: each `/publish` creates a new message id with a fresh failure
  count. (The attempt counters are in-memory anyway — restarting `demo02-subscriber` clears them.)
- **Demo 04** needs no reset either: every order gets a new id and its own key. To wipe the
  orders anyway: `docker exec <postgres-container> psql -U postgres -c "delete from state;"`.
- Stopping the AppHost removes the containers; state does not survive between sessions.

## Environment notes

- Redis uses host port **6390**, RabbitMQ **5673** and Postgres **5433** on purpose: `dapr init`
  already owns 6379 (`dapr_redis`), and 5672/5432 are commonly taken by other local projects.
- Aspire 13 starts the Redis container with TLS enabled (self-signed dev certificate); the
  Dapr components therefore set `enableTLS: "true"` (Dapr's Redis client does not verify the
  certificate).
- Component `initTimeout` is 120 s so a slow first container pull can't kill a sidecar.
- Both state stores set `keyPrefix: none`, so the keys in Redis and Postgres are the literal
  `demo-counter` / `order-<id>` the code names rather than the default per-app-id
  `demo03-worker||demo-counter` — which is why the `redis-cli DEL demo-counter` above works.
- Demo 04's `outboxstore` is a *second* state store component, on Postgres: only it has the
  outbox metadata, so demos 03 and 04 stay independent. Dapr creates its `state` table on first
  connect — no schema to set up.
