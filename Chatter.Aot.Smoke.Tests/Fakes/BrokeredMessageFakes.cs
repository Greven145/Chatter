using Chatter.CQRS;
using Chatter.MessageBrokers;

namespace Chatter.Aot.Smoke.Tests.Fakes;

[BrokeredMessage(sendingPath: null, receivingPath: "aot-smoke-test-queue")]
public sealed class ScrutorPongMessage : IMessage
{
}
