using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Aot.Smoke.Tests;

public class NumericWriteStringReadEnumConverterTests
{
    [Fact]
    [Trait("AotStatus", "KnownGap")]
    public void EnumConverter_UnderNativeAot_ThrowsMissingNativeCodeOrMetadata()
    {
        var ex = Assert.Throws<NotSupportedException>(
            () => JsonSerializer.Deserialize<PingStatus>("\"Closed\"", ChatterJson.Options));

        Assert.Contains("missing native code or metadata", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
