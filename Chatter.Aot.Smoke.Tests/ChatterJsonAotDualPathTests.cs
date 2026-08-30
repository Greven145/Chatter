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
}
