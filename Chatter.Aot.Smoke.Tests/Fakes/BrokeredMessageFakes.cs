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

// Deliberately its own [BrokeredMessage] type, not ScrutorPongMessage: calling the generated
// RegisterAll() makes BrokeredMessageReceiverBackgroundService<T> reachable for whatever T it
// discovers, and Native AOT preservation is whole-program — sharing ScrutorPongMessage here would
// flip ScrutorHandlerRegistrationTests' sibling KnownGap test green for the wrong reason.
[BrokeredMessage(sendingPath: null, receivingPath: "aot-smoke-generated-queue")]
public sealed class GeneratedPongMessage : IMessage
{
}
