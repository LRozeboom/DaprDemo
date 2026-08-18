# Demo diagrams

One Mermaid diagram per demo, sized for a slide. Ready-made PNG exports (2× scale,
white background) are in `docs/images/` — drag them straight onto a slide. To restyle or
re-export, paste a block below into <https://mermaid.live> (SVG stays crisp when scaled),
or re-render everything with:

```bash
npx @mermaid-js/mermaid-cli -i docs/diagrams.md -o /tmp/out.md -e png -b white -s 2
```

Legend used in all four: **blue** = our code, **amber** = the Dapr sidecar,
**purple** = external system, **white** = the presenter's `curl` / the log line.

## Demo 01 — Pub/sub through the sidecar

```mermaid
flowchart LR
    C(["curl POST /greetings"]):::client
    P["demo01-publisher<br/>:5101"]:::app
    PD["daprd"]:::car
    SD["daprd"]:::car
    S["demo01-subscriber<br/>:5102"]:::app
    L(["log: GREETING RECEIVED"]):::client

    subgraph B["pubsub component — swap in YAML, no code change"]
        R[("Redis<br/>:6390")]:::ext
        Q[("RabbitMQ<br/>:5673")]:::ext
    end

    C --> P
    P -->|"publish · topic greetings"| PD
    PD ==> R ==> SD
    PD -.-> Q -.-> SD
    SD -->|"POST /greetings-handler"| S --> L

    classDef app fill:#E8F0FE,stroke:#3B5BA5,color:#12233F
    classDef car fill:#FFF3D6,stroke:#C08A18,color:#4A3400
    classDef ext fill:#EDEAF8,stroke:#6C5CB5,color:#241D4B
    classDef client fill:#FFFFFF,stroke:#7A7A7A,color:#222222
    style B fill:#FAFAFC,stroke:#B9B6C8,stroke-dasharray:4 3,color:#241D4B
```

## Demo 02 — Retries: a non-2xx is the retry mechanism

```mermaid
sequenceDiagram
    autonumber
    actor C as curl
    participant A as demo02-subscriber
    participant D as Dapr sidecar
    participant R as Redis / RabbitMQ

    C->>A: POST /publish
    A->>D: publish · topic flakymessages
    D->>R: store message
    R->>D: deliver
    Note right of A: FlakyDeliveryPlan rolls<br/>1–5 planned failures for this message

    loop planned failures (random 1–5)
        D->>A: POST /flaky-messages-handler
        A-->>D: 500 — failure Result
        Note right of D: not 2xx → retry policy<br/>constant · 2s · max 10
    end

    D->>A: POST /flaky-messages-handler
    A-->>D: 200 OK — handled
    Note over A,R: retries live in resiliency.yaml — no retry code in the app
```

## Demo 03 — State store through the sidecar

```mermaid
flowchart LR
    C["GET /counter<br/>POST /counter/increment<br/>POST /counter/reset"]:::app
    S["CounterStore<br/><i>component name + key,<br/>no Redis client</i>"]:::app
    D["daprd"]:::car
    K[("Redis state store<br/>key: demo-counter")]:::ext

    C --> S -->|"GetStateAndETag / SaveState"| D --> K

    S -.->|"increment: read value + ETag"| RMW["write only if<br/>the ETag still matches"]:::good
    RMW -.->|"ETag stale — someone else wrote"| RETRY["rejected → re-read → retry"]:::warn
    RETRY -.-> RMW

    K -.->|"swap the backing store"| Y["statestore.yaml<br/><i>Postgres, Cosmos, …<br/>no code change</i>"]:::ext

    classDef app fill:#E8F0FE,stroke:#3B5BA5,color:#12233F
    classDef car fill:#FFF3D6,stroke:#C08A18,color:#4A3400
    classDef ext fill:#EDEAF8,stroke:#6C5CB5,color:#241D4B
    classDef good fill:#E3F5E8,stroke:#2E8B57,color:#12331F
    classDef warn fill:#FFF3D6,stroke:#C08A18,color:#4A3400
```

## Demo 04 — Output binding to Discord

```mermaid
flowchart LR
    C(["curl POST /alerts"]):::client

    subgraph API["demo04-api :5401"]
        direction TB
        E["Api<br/>POST /alerts"]:::app
        H["Application<br/>knows only INotifier"]:::app
        DM["Domain<br/>Alert.Create validates"]:::app
        I["Infrastructure<br/>DiscordBindingNotifier"]:::app
        E --> H
        H --> DM
        H --> I
    end

    S["daprd<br/>binding 'discord' · bindings.http"]:::car
    W(["Discord channel"]):::ext
    SEC[/"DISCORD_WEBHOOK_URL<br/>envvar-secrets"/]:::ext
    BAD(["400 · Alert.EmptyTitle"]):::bad

    C --> E
    I -->|"InvokeBindingAsync · create"| S -->|"HTTP POST webhook"| W
    SEC -.->|"resolves url"| S
    DM -.->|"empty title → failure Result"| BAD

    classDef app fill:#E8F0FE,stroke:#3B5BA5,color:#12233F
    classDef car fill:#FFF3D6,stroke:#C08A18,color:#4A3400
    classDef ext fill:#EDEAF8,stroke:#6C5CB5,color:#241D4B
    classDef client fill:#FFFFFF,stroke:#7A7A7A,color:#222222
    classDef bad fill:#FDE8E8,stroke:#C0392B,color:#3F1412
    style API fill:#FAFAFC,stroke:#B9B6C8,color:#12233F
```
