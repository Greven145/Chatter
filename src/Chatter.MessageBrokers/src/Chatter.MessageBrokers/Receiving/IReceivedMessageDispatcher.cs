using Chatter.CQRS;
using Chatter.MessageBrokers.Context;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.MessageBrokers.Receiving
{
    public interface IReceivedMessageDispatcher
    {
        Task DispatchAsync<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TMessage>(TMessage payload, MessageBrokerContext messageContext, CancellationToken receiverTokenSource) where TMessage : class, IMessage;
    }
}
