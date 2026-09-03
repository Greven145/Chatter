# Chatter samples

Real, runnable demonstrations of the Chatter libraries — not unit tests, not synthetic AOT smoke fixtures. Each sample is built and run against real infrastructure before being considered working.

## Chatter.Samples.Cqrs

`Chatter.CQRS` used standalone, no broker: a command, a query, an event, and a pipeline behavior, wired up via the library's default reflection-based assembly scanning (`AddChatterCqrs`).

```bash
dotnet run --project samples/Chatter.Samples.Cqrs
```

## Chatter.Samples.RabbitMq

A real publish → RabbitMQ → receive round trip, demonstrating both of Chatter's registration paths side by side:

- `scan` — the default, reflection-based assembly-scanning registration (`AddChatterCqrs`). JIT only; scanning is not AOT-safe.
- `explicit` (default) — the AOT-safe, reflection-free explicit registration API (`AddChatterCqrsWithExplicitHandlers` + `AddCommandHandler`), combined with the AOT-safe JSON path (`WithAotJsonSerialization`) needed for message-body (de)serialization under Native AOT. This is the mode published and run as a real Native AOT binary.

### Run the infrastructure

```bash
cd samples
docker compose up -d
```

RabbitMQ provisions no topology itself — `rabbitmq-definitions.json` declares the work queue, dead-letter queue, and the `guest` user/permissions (the management plugin's `load_definitions` mechanism bypasses the image's normal env-var-driven default-user bootstrap, so the user must be declared here too), imported via `rabbitmq.conf` mounted into `conf.d/` — not as a standalone `/etc/rabbitmq/rabbitmq.conf`, which would replace rather than extend the image's own generated config.

### Run it — JIT, either mode

```bash
dotnet run --project samples/Chatter.Samples.RabbitMq -- scan
dotnet run --project samples/Chatter.Samples.RabbitMq -- explicit
```

### Run it — Native AOT

```bash
dotnet publish samples/Chatter.Samples.RabbitMq -c Release -r linux-x64
samples/Chatter.Samples.RabbitMq/bin/Release/net10.0/linux-x64/publish/Chatter.Samples.RabbitMq explicit
```

A successful run prints `[success] round trip complete, payload=...` and exits `0`.
