namespace Chatter.Aot.Smoke.Tests.Fakes;

public enum PingStatus
{
    Open = 0,
    Closed = 1,
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
