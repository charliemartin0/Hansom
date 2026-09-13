# Hansom

A Wolverine-shaped in-process bus for .NET, built one slice at a time. Onion-architecture kernel, in-memory defaults, broker adapters in sibling projects. The shape is borrowed from Wolverine; the catalog is not cloned.

## Why

Most .NET apps that need messaging end up choosing between two extremes. A thin MediatR that has no opinion about retries, scheduling, or persistence — and a full MassTransit / NServiceBus / Wolverine stack that ships every transport, codegen, and integration at once. Hansom is the middle path. It borrows the *shape* of Wolverine — handler catalog, in-process `Invoke`/`Publish`, cascading messages, envelope-based execution — and stops at the kernel. Broker adapters are separate packages. The onion architecture is enforced. Every slice ships with prove-with tests.

The project started as a learning exercise. Twenty-six slices in, the kernel is real, the slices are tested, and the design choices (no source generation, strict layering, slice-driven delivery) make it worth a name and a roadmap of its own.

## Status

The kernel ships. The broker adapters are scaffolds. There is no 1.0 release yet.

| Layer | Status |
| --- | --- |
| Kernel (`src/Hansom`) | Ships: handler discovery, in-process `Invoke`/`Publish`, cascading messages, scheduled envelopes, instance sagas, in-memory queues + transport, in-memory inbox/outbox/dead-letter/saga stores, JSON serialization, observability hooks. |
| Persistence | In-memory stores only. PostgreSQL, RabbitMQ, and HTTP adapter projects exist as empty scaffolds ready to be filled. |
| Tests | 280 kernel facts + 3 sample facts. xUnit, snake_case behaviour sentences, small fakes (no broker, no DB). |
| Sample | `Helpdesk` — an `OrderSaga` exercised end-to-end against the in-memory kernel. |
| Release | Not yet. Roadmap below. |

## Quickstart

```bash
dotnet build src/Hansom
dotnet test
dotnet run --project samples/Helpdesk/src/Helpdesk.Host
```

The Helpdesk sample runs against the in-memory kernel: instance saga, local queues, in-memory store. It is the smallest conversation Hansom can have. Everything in the roadmap is "more of the same, on real infrastructure."

## Layout

```
Hansom/
  src/Hansom/
    Domain/                      Envelope, wire names, saga identity, named errors — no I/O
    Application/                 Invoke, cascades, routing, execution, sagas — ports only
    Infrastructure/              host, JSON, local queues, transport/persistence ports
  src/Hansom.Postgresql/         persistence adapter (empty scaffold)
  src/Hansom.RabbitMQ/           broker adapter (empty scaffold)
  src/Hansom.Http/               HTTP front door (empty scaffold)
  samples/Helpdesk/src/
    Helpdesk.Domain/             entities / value objects — no Hansom, no infra
    Helpdesk.Application/        messages + handlers — Domain + Hansom
    Helpdesk.Infrastructure/     in-memory wiring — Application + Hansom
    Helpdesk.Host/               composition root
  tests/Hansom.Tests/            xUnit facts, small fakes
  tests/Helpdesk.Tests/          sample facts
```

`src/Hansom` does not `ProjectReference` the adapter projects. Npgsql stays in `Hansom.Postgresql`; Rabbit in `Hansom.RabbitMQ`; HTTP in `Hansom.Http`. The dependency direction is enforced by project rules, not by convention.

## What's done

Twenty-six slices have landed. The highlights, in order:

- **A1+A2** unified envelope delivery
- **DX #1** `UseHansom` single-line composition root
- **DX #2** reject unknown handler slots at startup
- **Helpdesk** end-to-end sample with `OrderSaga`
- **ScheduledCascade** API
- **OrderTimeout** in-progress saga using scheduled envelopes
- **Queue/transport saga dispatch** fix
- **Perf #1** cached invoker (no reflection on the hot path)
- **Perf #2** catalog dictionary (`O(1)` type → handlers)
- **Perf #3** `Task<T>` unpack in the executor
- **A3+A4** onion cleanup
- **A5** dead-letter wiring
- **A6** validators at host start
- **A7** Requeue / Discard switch arms on the executor

The codemap (regenerable from the slice history) records every slice.

## What's next

Honest about order. Not every item ships next. Each is a slice: prove-with test first, then the smallest type that makes it pass.

1. **PostgreSQL adapter** — durable inbox / outbox / saga stores against Npgsql. Ports exist; the adapter fills them.
2. **RabbitMQ adapter** — broker transport + durable subscriptions.
3. **HTTP adapter** — request/response across processes against the same execution pipeline.
4. **First-party logging middleware** — structured logs around handler invocation.
5. **Validator middleware** — pluggable handler-input validation.
6. **Outbox middleware** — kernel-level transactional outbox over the persistence ports.
7. **Polymorphic serialization** — discriminator-aware JSON for heterogeneous payloads.
8. **Observability polish** — OTel exporters, in-flight gauges, latency histograms beyond the static `ActivitySource`/`Meter`.
9. **1.0** — once the kernel is documented, one broker adapter is shipped, and the slice history reads clean.

## Inspiration

Hansom borrows the *shape* of [Wolverine](https://github.com/JasperFx/wolverine) by Jeremy Miller — handler catalog, envelope model, in-process `Invoke`/`Publish`, cascading messages. It does not clone Wolverine's catalog. There is no source generation, no Marten / EF integration, no in-core broker transports. The kernel stops at "Wolverine-shaped in-process" and refuses to grow beyond what the test suite proves.

Phoenix credits Rails. Hansom credits Wolverine.

## Project rules

- One slice at a time. No stacked PRs.
- Every slice ships with prove-with tests.
- `src/Hansom` does not `ProjectReference` any adapter.
- Branch from `origin/main`, PR to `main`. The slice is the unit of progress, not the commit.

See `AGENTS.md` and `.cursor/rules/` for the full set.

## Build

```bash
dotnet build src/Hansom
dotnet test
dotnet run --project samples/Helpdesk/src/Helpdesk.Host
```

.NET 10. Targets `net10.0` via `Directory.Build.props`. A `.slnx` exists at the root for `dotnet test`. No CI yet.

## License

[MIT](LICENSE).
