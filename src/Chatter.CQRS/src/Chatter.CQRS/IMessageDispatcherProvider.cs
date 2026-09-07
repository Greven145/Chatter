using System.Diagnostics.CodeAnalysis;

namespace Chatter.CQRS
{
    /// <summary>
    /// Creates an <see cref="IMessageDispatcher"/>
    /// </summary>
    public interface IMessageDispatcherProvider
    {
        /// <summary>
        /// Gets an <see cref="IDispatchMessages"/> based on the <typeparamref name="TMessage"/> provided.
        /// </summary>
        /// <typeparam name="TMessage">The type of <see cref="IDispatchMessages"/> to get.</typeparam>
        /// <returns>A message dispatcher</returns>
        IDispatchMessages GetDispatcher<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)] TMessage>() where TMessage : IMessage;
    }
}
