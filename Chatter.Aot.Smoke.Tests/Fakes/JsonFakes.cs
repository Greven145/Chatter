using System.Text.Json.Serialization;

namespace Chatter.Aot.Smoke.Tests.Fakes;

public enum PingStatus
{
    Open = 0,
    Closed = 1,
}

// Deliberately its own enum, not PingStatus: PingStatus is NumericWriteStringReadEnumConverterTests'
// dedicated KnownGap fixture for ChatterJson.Options' reflection path. Native AOT's codegen
// preservation is whole-program (same finding as Phase 2's constructor-preservation surprise) —
// covering PingStatus here too would make ILC generate real code for it, flipping that other test
// green for the wrong reason instead of the gap it's meant to demonstrate actually closing.
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
