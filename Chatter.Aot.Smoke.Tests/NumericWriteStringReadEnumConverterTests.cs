using Chatter.Aot.Smoke.Tests.Fakes;
using Chatter.MessageBrokers;
using System.Text.Json;

namespace Chatter.Aot.Smoke.Tests;

// NumericWriteStringReadEnumConverter.CreateConverter uses Type.MakeGenericType +
// Activator.CreateInstance to build a closed EnumConverter<TEnum> per encountered enum type.
// Hard Native-AOT-throw site, confirmed here: because
// EnumConverter<PingStatus> is never instantiated via a statically-visible closed generic anywhere in
// the program, the AOT compiler never generates native code for that instantiation, so
// MakeGenericType/Activator.CreateInstance cannot materialize it at runtime.
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
