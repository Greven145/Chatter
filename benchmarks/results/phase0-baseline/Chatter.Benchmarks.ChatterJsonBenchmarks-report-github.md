```

BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.1 LTS (Noble Numbat)
AMD Ryzen 7 3700X 3.60GHz, 1 CPU, 8 logical and 4 physical cores
.NET SDK 10.0.111
  [Host]     : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.11 (10.0.11, 10.0.1126.37416), X64 RyuJIT x86-64-v3


```
| Method      | Mean     | Error    | StdDev   | Gen0   | Allocated |
|------------ |---------:|---------:|---------:|-------:|----------:|
| Serialize   | 494.0 ns |  3.86 ns |  3.61 ns | 0.0849 |     712 B |
| Deserialize | 812.2 ns | 12.70 ns | 11.88 ns | 0.0982 |     824 B |
