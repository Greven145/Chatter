# Phase 0 baseline (#276)

Captured 2026-08-29 against the unmodified Phase 0 implementation (before any Phase 1/3 production
code changes), so Phase 1 (QueryDispatcher) and Phase 3 (ChatterJson) have a "before" to diff their
"after" numbers against. Manual capture only — not part of CI (see `benchmarks/Chatter.Benchmarks`).

Environment: BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.1 LTS, AMD Ryzen 7 3700X 3.60GHz (1 CPU, 8
logical/4 physical cores), .NET SDK 10.0.111, .NET 10.0.11 runtime, Release config. Absolute
nanosecond/byte figures are machine-dependent — what Phase 1/3 should diff against is the *ratio*
between "before" and "after" runs on the same machine, plus the shape of the allocation profile.

## QueryDispatcher dispatch throughput

| Method | Mean | Allocated |
|---|---|---|
| `NonGenericDynamicDispatch` (`dynamic` + `MakeGenericType`, current) | 429.07 ns | 496 B |
| `GenericDispatch` (statically-typed overload) | 77.33 ns | 288 B |

The non-generic overload Phase 1 targets is ~5.5x slower and allocates ~1.7x more than the
already-fast generic overload — this is the number Phase 1's rewrite should close.

## ChatterJson serialize/deserialize throughput

| Method | Mean | Allocated |
|---|---|---|
| `Serialize` | 494.0 ns | 712 B |
| `Deserialize` | 812.2 ns | 824 B |

Representative brokered-message-shaped DTO (Guid, string, DateTimeOffset, enum, `List<string>`)
through the exact shared `ChatterJson.Options` instance every broker body converter uses today.

## Reproducing

```
cd benchmarks/Chatter.Benchmarks
dotnet run -c Release -- --filter '*'
```

Full BenchmarkDotNet output (histograms, outlier detection, etc.) lands in
`benchmarks/Chatter.Benchmarks/BenchmarkDotNet.Artifacts/` (gitignored); this directory holds only
the exported summary tables (`*-report-github.md`, `*-report.csv`) worth keeping for comparison.
