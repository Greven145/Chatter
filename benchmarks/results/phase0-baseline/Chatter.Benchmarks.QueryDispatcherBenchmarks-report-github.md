```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.1 LTS (Noble Numbat)
AMD Ryzen 7 3700X 3.60GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.111
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method                    | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| NonGenericDynamicDispatch | 429.07 ns | 4.621 ns | 4.096 ns |  1.00 | 0.0591 |     496 B |        1.00 |
| GenericDispatch           |  77.33 ns | 1.355 ns | 1.267 ns |  0.18 | 0.0343 |     288 B |        0.58 |
