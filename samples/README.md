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

Because this project compiles *both* modes into the same binary (picked at runtime via a CLI argument), a `dotnet publish -r linux-x64` on it is **not** warning-free — the trim/AOT analyzer correctly flags the `scan` branch's genuinely unsafe calls even though only `explicit` is ever meant to run under AOT. That's expected and useful: it's a real side-by-side comparison of what the analyzer actually reports for each path. See `Chatter.Samples.RabbitMq.Aot` below for what a publish that only ever compiles the AOT-safe path looks like.

## Chatter.Samples.RabbitMq.Aot

The same publish → RabbitMQ → receive round trip as above, but this project contains **zero** reflection-based Chatter API calls at all — no `AddChatterCqrs`, no runtime mode switch, `PublishAot=true` set directly in the csproj. What changes versus the default reflection-based path, concretely:

1. **`AddChatterCqrsWithExplicitHandlers` instead of `AddChatterCqrs`** — no assembly scan; handlers are registered one at a time via `AddCommandHandler<TCommand, THandler>()`.
2. **`AddQueueReceiver<TMessage>` needs no change** — it already delegates to the AOT-safe `Services.AddReceiver<TMessage>` internally (confirmed by reading `RabbitMqOptionsBuilder.cs`), regardless of which registration path the rest of the app uses.
3. **`WithAotJsonSerialization(consumerContext)` with a `[JsonSerializable]`-decorated `JsonSerializerContext`** for every message DTO. This isn't optional polish — without it, the receiver throws at runtime the first time it tries to deserialize a real message under a full AOT publish (hit directly while building `Chatter.Samples.RabbitMq`'s `explicit` mode, before this project existed).

```bash
dotnet publish samples/Chatter.Samples.RabbitMq.Aot -c Release -r linux-x64
samples/Chatter.Samples.RabbitMq.Aot/bin/Release/net10.0/linux-x64/publish/Chatter.Samples.RabbitMq.Aot
```

**This publish is not warning-free either — verified directly, not assumed clean.** 25 warnings, in two genuinely different categories:

- Most of them (~20) originate from *inside* `Chatter.CQRS.csproj`/`Chatter.MessageBrokers.csproj`/`Chatter.MessageBrokers.RabbitMQ.csproj` themselves — confirmed by reading the originating-project attribution MSBuild prints on each warning line. This sample references those libraries via `ProjectReference` (source), so the trim/AOT analyzer re-walks their own internal implementation (`ChatterJson.Options`'s reflection default, the reflection-based body converters, the tracked `MessageDispatcherProvider` gap — [#286](https://github.com/brenpike/Chatter/issues/286)) as part of *this* build graph. A real external consumer referencing the published NuGet packages would never see these — the analyzer only re-walks source it's compiling, not a package's prebuilt internals.
- One warning is genuinely load-bearing and **not eliminable by writing different consumer code today**: `AddMessageBrokers()` itself is unconditionally annotated `RequiresUnreferencedCode`/`RequiresDynamicCode`, because it always performs some `[BrokeredMessage]`-attribute reflection scanning as part of its own default behavior — regardless of whether every receiver afterward is registered explicitly via `AddQueueReceiver`/`AddReceiver<TMessage>`. Unlike `Chatter.CQRS`, `Chatter.MessageBrokers` has no separate non-scanning entry point equivalent to `AddChatterCqrsWithExplicitHandlers`. Closing this is real, scoped follow-up work, not something this sample can route around.

## Runner script

`./run.sh <cqrs|rabbitmq-scan|rabbitmq-explicit|rabbitmq-aot|down>` brings up the shared RabbitMQ infra automatically for the broker demos and runs the requested one.
