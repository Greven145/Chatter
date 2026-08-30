using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Aot.Smoke.Tests;

public class ChatterJsonPrivateReflectionTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void EnableNonPublicSetters_UnderNativeAot_ThrowsBeforeModifierRuns()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => JsonSerializer.Deserialize<PingPrivateSetterDto>("{\"Name\":\"abc\"}", ChatterJson.Options));

        Assert.Contains("parameterless constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void EnableNonPublicParameterlessConstructor_UnderNativeAot_ThrowsBeforeModifierRuns()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => JsonSerializer.Deserialize<PingPrivateCtorDto>("{\"Name\":\"abc\"}", ChatterJson.Options));

        Assert.Contains("parameterless constructor", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
