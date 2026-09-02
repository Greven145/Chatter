using Chatter.CQRS.Commands;
using Chatter.CQRS.Pipeline;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An AOT-safe, reflection-free alternative to <see cref="CommandPipelineBuilder.WithBehavior{TCommandBehavior}"/>
    /// and <see cref="ServiceCollectionExtensions.AddPipelineBehavior(IServiceCollection, System.Type)"/>. Register
    /// each behavior explicitly, once per command type, via <see cref="AddCommandBehavior{TCommand, TCommandBehavior}"/>.
    /// </summary>
    public static class ExplicitCommandBehaviorRegistrationExtensions
    {
        /// <summary>
        /// Registers <typeparamref name="TCommandBehavior"/> as a behavior in the pipeline for
        /// <typeparamref name="TCommand"/>, alongside any other behaviors already registered for that command type —
        /// matching the additive semantics of the reflection-based path
        /// (<see cref="ServiceCollectionExtensions.RegisterBehaviorForCommand"/>). There is no explicit,
        /// reflection-free equivalent of <see cref="ServiceCollectionExtensions.RegisterBehaviorForAllCommands"/>
        /// (an open generic behavior applied to every command type): that requires enumerating every command type at
        /// runtime, which is unavoidably a whole-program scan. To apply the same behavior to multiple commands under
        /// AOT, call this method once per command type.
        /// </summary>
        public static IServiceCollection AddCommandBehavior<TCommand, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] TCommandBehavior>(this IServiceCollection services)
            where TCommand : ICommand
            where TCommandBehavior : class, ICommandBehavior<TCommand>
            => services.AddTransient<ICommandBehavior<TCommand>, TCommandBehavior>();
    }
}
