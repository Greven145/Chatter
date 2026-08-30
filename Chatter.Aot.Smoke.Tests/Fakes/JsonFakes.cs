using System.Text.Json.Serialization;

namespace Chatter.Aot.Smoke.Tests.Fakes;

public enum PingStatus
{
    Open = 0,
    Closed = 1,
}

// Deliberately a separate enum from PingStatus (NumericWriteStringReadEnumConverterTests' KnownGap
// fixture for the reflection path). Native AOT constructor/type preservation is whole-program, not
// per-use-site: referencing PingStatus here would give ILC a reason to generate real code for it
// everywhere, silently flipping that other test green without the gap it exercises actually closing.
public enum PingResultStatus
{
    Open = 0,
    Closed = 1,
}

public sealed class PingPublicDto
{
    public string? Name { get; set; }
    public PingResultStatus Status { get; set; }
}

[JsonSerializable(typeof(PingPublicDto))]
internal partial class PingJsonContext : JsonSerializerContext
{
}

public sealed class PingPrivateSetterDto
{
    public string? Name { get; private set; }
}

public sealed class PingPrivateCtorDto
{
    public string? Name { get; set; }

    private PingPrivateCtorDto()
    {
    }
}
