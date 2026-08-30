using Chatter.CQRS;
using Chatter.MessageBrokers;

namespace Chatter.Aot.Smoke.Tests.Fakes;

[BrokeredMessage(sendingPath: null, receivingPath: "aot-smoke-test-queue")]
public sealed class ScrutorPongMessage : IMessage
{
}

// Deliberately undecorated: AddReceiver<TMessage> throws if the message type carries [BrokeredMessage]
// (that's the attribute-scan route's territory), so the explicit route needs its own message type.
public sealed class ExplicitPongMessage : IMessage
{
}
