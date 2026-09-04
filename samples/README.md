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
2. **`AddMessageBrokersWithExplicitReceivers` instead of `AddMessageBrokers`** — the latter unconditionally scans for `[BrokeredMessage]`-decorated types as part of its own default behavior, even when every receiver is otherwise registered explicitly; the former carries neither `RequiresUnreferencedCode` nor `RequiresDynamicCode` at all.
3. **`AddQueueReceiver<TMessage>` needs no change** — it already delegates to the AOT-safe `Services.AddReceiver<TMessage>` internally (confirmed by reading `RabbitMqOptionsBuilder.cs`), regardless of which registration path the rest of the app uses.
4. **`WithAotJsonSerialization(consumerContext)` with a `[JsonSerializable]`-decorated `JsonSerializerContext`** for every message DTO. This isn't optional polish — without it, the receiver throws at runtime the first time it tries to deserialize a real message under a full AOT publish (hit directly while building `Chatter.Samples.RabbitMq`'s `explicit` mode, before this project existed).

```bash
dotnet publish samples/Chatter.Samples.RabbitMq.Aot -c Release -r linux-x64
samples/Chatter.Samples.RabbitMq.Aot/bin/Release/net10.0/linux-x64/publish/Chatter.Samples.RabbitMq.Aot
```

**This sample's own code is now verified warning-free.** The earlier `AddMessageBrokers()` call was the last remaining Chatter-API call in this project that carried its own trim/AOT annotation — confirmed directly: `dotnet publish -c Release -r linux-x64` produces zero warnings attributed to `Program.cs`/`Ping.cs` once it's swapped for `AddMessageBrokersWithExplicitReceivers`.

The publish as a whole is still not fully warning-free — 21 warnings remain, all from *inside* the referenced libraries' own internals, not this sample's code. Verified twice: once by reading the per-line project attribution, and once by packing `Chatter.CQRS`/`Chatter.MessageBrokers`/`Chatter.MessageBrokers.RabbitMQ` as real `.nupkg`s and restoring them into a fresh scratch console app via plain `PackageReference` (no `ProjectReference` anywhere) — the same warnings appeared there too, confirming they're real for any consumer, not a `ProjectReference`-only artifact. Breakdown:

- **Structurally unavoidable given the library's current design**: `ChatterJson`'s static constructor (`ChatterJson..cctor()`) unconditionally initializes the reflection-based `Options` static field the moment the type is touched at all — including just to call the AOT-safe `CreateAotOptions` static method on the same type. There's no way to reference `ChatterJson` without paying for this today.
- **A static-analysis false positive for this sample's actual runtime configuration, not a real bug**: `JsonBodyConverter`/`RabbitMqBodyConverter`/`MaterializingObjectConverter`/`MessageContext`/`InMemoryBrokeredMessageOutbox` all call `JsonSerializer.Serialize<T>`/`Deserialize<T>` overloads, and `RequiresUnreferencedCode`/`RequiresDynamicCode` are attached to those **BCL method signatures themselves** — the analyzer flags every call to them unconditionally, regardless of which `JsonSerializerOptions` instance is actually passed at runtime. It has no way to statically know that `WithAotJsonSerialization` supplies a fully source-generated, AOT-safe options instance here. The round trip in this very sample runs correctly under a real published Native AOT binary (verified above) — proof these specific call sites are safe *as configured*, even though the build still warns on them. Silencing them for real would mean adding explicit `[UnconditionalSuppressMessage]` justifications at each site, which the library does not currently do.
- **Already tracked**: `MessageDispatcherProvider.GetDispatcher<TMessage>()`'s `IL2090` ([#286](https://github.com/brenpike/Chatter/issues/286)).

## Runner script

`./run.sh <cqrs|rabbitmq-scan|rabbitmq-explicit|rabbitmq-aot|down>` brings up the shared RabbitMQ infra automatically for the broker demos and runs the requested one.
