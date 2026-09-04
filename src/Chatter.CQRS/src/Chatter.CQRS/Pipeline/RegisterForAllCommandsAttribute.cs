using System;

namespace Chatter.CQRS.Pipeline
{
    /// <summary>
    /// Marks an open-generic <see cref="ICommandBehavior{TMessage}"/> implementation for discovery by
    /// Chatter.SourceGenerators' compile-time "all commands" behavior registration generator — the
    /// AOT-safe alternative to <c>Chatter.CQRS.DependencyInjection.ServiceCollectionExtensions.RegisterBehaviorForAllCommands</c>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class RegisterForAllCommandsAttribute : Attribute
    {
    }
}
