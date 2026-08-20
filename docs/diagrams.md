# Demo diagrams

One Mermaid diagram per demo — two for demo 04, the problem then the mechanism — sized for a
slide. Ready-made PNG exports (2× scale, white background) are in `docs/images/` — drag them
straight onto a slide. To restyle or
re-export, paste a block below into <https://mermaid.live> (SVG stays crisp when scaled),
or re-render everything with:

```bash
# from the repo root — mermaid-cli names the exports after the -o file, in document order
npx -y @mermaid-js/mermaid-cli -i docs/diagrams.md -o docs/images/out.md -e png -b white -s 2
cd docs/images
mv out-1.png demo01-pubsub.png
mv out-2.png demo02-retries.png
mv out-3.png demo03-statestore.png
mv out-4.png demo04-outbox-problem.png
mv out-5.png demo04-outbox.png
rm out.md
```

Legend used throughout: **blue** = our code, **amber** = the Dapr sidecar,
**purple** = external system, **white** = the presenter's `curl` / the log line,
**red** = the failure path.

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

## Demo 04a — The dual-write problem the outbox solves

```mermaid
flowchart TB
    subgraph BAD["without an outbox — two writes, no transaction"]
        direction LR
        A1["order service"]:::app
        D1[("database")]:::ext
        B1[("broker")]:::ext
        A1 -->|"1 · save order"| D1
        A1 -->|"2 · publish OrderPlaced"| B1
        X1(["crash between 1 and 2<br/>order stored, nobody told"]):::bad
        X2(["publish first, save fails<br/>event for an order that<br/>does not exist"]):::bad
        D1 -.-> X1
        B1 -.-> X2
    end

    subgraph GOOD["with Dapr's outbox — one transaction"]
        direction LR
        A2["order service<br/><i>writes state, never publishes</i>"]:::app
        S2["daprd"]:::car
        D2[("Postgres<br/>order row + outbox marker")]:::ext
        B2[("Redis<br/>topic orders")]:::ext
        A2 -->|"one state transaction"| S2
        S2 ==>|"commit"| D2
        D2 -.->|"committed?"| S2
        S2 ==>|"only then: publish"| B2
        OK(["either both, or neither"]):::good
        B2 --- OK
    end

    BAD ~~~ GOOD

    classDef app fill:#E8F0FE,stroke:#3B5BA5,color:#12233F
    classDef car fill:#FFF3D6,stroke:#C08A18,color:#4A3400
    classDef ext fill:#EDEAF8,stroke:#6C5CB5,color:#241D4B
    classDef good fill:#E3F5E8,stroke:#2E8B57,color:#12331F
    classDef bad fill:#FDE8E8,stroke:#C0392B,color:#3F1412
    style BAD fill:#FFF7F7,stroke:#C0392B,stroke-dasharray:4 3,color:#3F1412
    style GOOD fill:#F5FBF6,stroke:#2E8B57,stroke-dasharray:4 3,color:#12331F
```

## Demo 04b — Transactional outbox: the flow

```mermaid
sequenceDiagram
    autonumber
    actor C as curl
    participant A as demo04-outbox
    participant D as daprd
    participant P as Postgres<br/>outboxstore
    participant R as Redis<br/>pubsub

    C->>A: POST /orders
    A->>D: one state transaction:<br/>order row + outbox.projection (the event)
    Note right of A: no PublishEventAsync anywhere<br/>the app only writes state
    D->>P: BEGIN · order row + outbox marker · COMMIT
    D-->>A: committed
    A-->>C: 202 Accepted
    D->>P: marker there?
    P-->>D: yes — the write is durable
    D->>R: publish OrderPlaced · topic orders
    R->>D: deliver
    D->>A: POST /orders-handler
    A->>P: read order by id
    Note right of A: log: ORDER RECEIVED — the state<br/>store already has it as 'Placed'
    Note over D,P: rolled back instead? no marker row,<br/>so the event is never published
```

