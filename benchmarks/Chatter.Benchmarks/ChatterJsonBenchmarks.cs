using BenchmarkDotNet.Attributes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Benchmarks;

// Baseline for #275 Phase 3 (ChatterJson's reflection-based DefaultJsonTypeInfoResolver + custom
// modifiers is a planned dual-path rewrite target, adding a source-generated path alongside the
// existing one). Serializes/deserializes a representative brokered-message-shaped DTO through the
// exact shared ChatterJson.Options instance every broker body converter uses today.
[MemoryDiagnoser]
public class ChatterJsonBenchmarks
{
    private static readonly BenchmarkMessage Message = new()
    {
        Id = Guid.NewGuid(),
        Name = "order-placed",
        CreatedAt = DateTimeOffset.UtcNow,
        Status = BenchmarkStatus.Booked,
        Tags = new List<string> { "orders", "priority", "eu-west" },
    };

    private static readonly string SerializedMessage = JsonSerializer.Serialize(Message, ChatterJson.Options);

    [Benchmark]
    public string Serialize() => JsonSerializer.Serialize(Message, ChatterJson.Options);

    [Benchmark]
    public BenchmarkMessage? Deserialize() => JsonSerializer.Deserialize<BenchmarkMessage>(SerializedMessage, ChatterJson.Options);

    public enum BenchmarkStatus
    {
        Pending = 0,
        Booked = 1,
        Cancelled = 2,
    }

    public sealed class BenchmarkMessage
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }
        public BenchmarkStatus Status { get; set; }
        public List<string> Tags { get; set; } = new();
    }
}
