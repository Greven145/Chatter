using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Aot.Smoke.Tests;

public class ChatterJsonAotDualPathTests
{
    [Fact]
    public void CreateAotOptions_UnderNativeAot_RoundTripsThroughSourceGeneratedResolver()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);
        var original = new PingPublicDto { Name = "abc", Status = PingResultStatus.Closed };

        var json = JsonSerializer.Serialize(original, options);
        var roundTripped = JsonSerializer.Deserialize<PingPublicDto>(json, options);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.Name, roundTripped!.Name);
        Assert.Equal(original.Status, roundTripped.Status);
    }

    [Fact]
    public void JsonBodyConverter_UnderNativeAot_RoundTripsThroughInjectedAotOptions()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);
        var sut = new JsonBodyConverter(options);
        var original = new PingPublicDto { Name = "xyz", Status = PingResultStatus.Open };

        var bytes = sut.Convert(original);
        var roundTripped = sut.Convert<PingPublicDto>(bytes);

        Assert.NotNull(roundTripped);
        Assert.Equal(original.Name, roundTripped!.Name);
        Assert.Equal(original.Status, roundTripped.Status);
    }

    // Adversarial-review CRITICAL finding, re-verified against a real published AOT binary (not just
    // JIT): case-insensitive property matching still applies through the combined source-gen resolver.
    [Fact]
    public void CreateAotOptions_UnderNativeAot_ReadsCamelCasePropertyNameCaseInsensitively()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);

        var result = JsonSerializer.Deserialize<PingPublicDto>("{\"name\":\"abc\",\"status\":1}", options);

        Assert.NotNull(result);
        Assert.Equal("abc", result!.Name);
        Assert.Equal(PingResultStatus.Closed, result.Status);
    }

    // Adversarial-review HIGH finding, re-verified against a real published AOT binary: a null body
    // still stringifies to the literal JSON null on the injected-AOT-options path, matching the
    // reflection path's parity contract (Newtonsoft.SerializeObject(null) => "null").
    [Fact]
    public void JsonBodyConverter_UnderNativeAot_StringifiesNullBodyWithoutThrowing()
    {
        var options = ChatterJson.CreateAotOptions(PingJsonContext.Default);
        var sut = new JsonBodyConverter(options);

        var json = sut.Stringify((object?)null);

        Assert.Equal("null", json);
    }
}
