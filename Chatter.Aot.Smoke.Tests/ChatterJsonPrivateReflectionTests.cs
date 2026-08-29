using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Aot.Smoke.Tests;

// Red baseline for #275/#276: ChatterJson.cs's EnableNonPublicSetters (~line 152) and
// EnableNonPublicParameterlessConstructor (~line 214) contract-model modifiers use
// PropertyInfo.SetValue / ConstructorInfo.Invoke via reflection to restore Newtonsoft parity for
// consumer DTOs with non-public setters/constructors. Empirically verified: under full Native AOT
// trimming, deserialization fails BEFORE either modifier gets a chance to run — DefaultJsonTypeInfoResolver
// cannot find ANY usable constructor via reflection for a DTO whose only constructor(s) are not
// otherwise directly referenced elsewhere in the program (this held even for a directly-`new()`'d
// public parameterless ctor in an earlier probe: a direct `newobj` call keeps the constructor's CODE
// callable but does not, by itself, preserve the separate REFLECTION metadata
// DefaultJsonTypeInfoResolver needs). The two modifiers are effectively unreachable code under AOT for
// this reason, not merely "silently drops data" as their design intends for the non-trimmed case.
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
