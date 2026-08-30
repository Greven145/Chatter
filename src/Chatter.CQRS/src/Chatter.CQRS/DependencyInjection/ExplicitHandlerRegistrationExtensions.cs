using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.DependencyInjection;
using Chatter.CQRS.Events;
using Chatter.CQRS.Pipeline;
using Chatter.CQRS.Queries;
using Microsoft.Extensions.Configuration;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// An AOT-safe, reflection-free alternative to <see cref="CqrsExtensions.AddChatterCqrs(IServiceCollection, IConfiguration, Action{CommandPipelineBuilder}, Action{AssemblySourceFilterBuilder})"/>
    /// and its Scrutor assembly-scanning overloads. Register each handler explicitly via
    /// <see cref="AddCommandHandler{TCommand, THandler}"/>, <see cref="AddEventHandler{TEvent, THandler}"/>, and
    /// <see cref="AddQueryHandler{TQuery, TResult, THandler}"/>.
    /// </summary>
    public static class ExplicitHandlerRegistrationExtensions
    {
        /// <summary>
        /// Adds chatter cqrs capabilities without scanning assemblies for handlers. Handlers must be registered
        /// explicitly via <see cref="AddCommandHandler{TCommand, THandler}"/>, <see cref="AddEventHandler{TEvent, THandler}"/>,
        /// and <see cref="AddQueryHandler{TQuery, TResult, THandler}"/>.
        /// </summary>
        /// <param name="services">The <see cref="IServiceCollection"/> used to register services used for cqrs capabilities</param>
        /// <param name="configuration">The <see cref="IConfiguration"/> used for configuration based settings</param>
        /// <param name="pipelineBuilder">An optional builder used to define an <see cref="ICommandBehaviorPipeline{TMessage}"/></param>
        /// <returns>An <see cref="IChatterBuilder"/> used to configure Chatter capabilities</returns>
        public static IChatterBuilder AddChatterCqrsWithExplicitHandlers(this IServiceCollection services, IConfiguration configuration, Action<CommandPipelineBuilder> pipelineBuilder = null)
        {
            var chatterBuilder = ChatterBuilder.Create(services, configuration, AssemblySourceFilterBuilder.New().Build());

            return CqrsExtensions.AddCoreCqrsServices(chatterBuilder, pipelineBuilder);
        }

        /// <summary>
        /// Registers <typeparamref name="THandler"/> as the handler for <typeparamref name="TCommand"/>, replacing any
        /// existing registration — matching the single-handler-per-command semantics of the Scrutor-scanned path
        /// (<c>RegistrationStrategy.Replace</c> in <see cref="CqrsExtensions.AddCommandHandlers"/>).
        /// </summary>
        public static IServiceCollection AddCommandHandler<TCommand, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services)
            where TCommand : ICommand
            where THandler : class, IMessageHandler<TCommand>
            => services.Replace<IMessageHandler<TCommand>, THandler>(ServiceLifetime.Transient);

        /// <summary>
        /// Registers <typeparamref name="THandler"/> as a handler for <typeparamref name="TEvent"/>, alongside any
        /// existing registrations — matching the multiple-handlers-per-event semantics of the Scrutor-scanned path
        /// (<c>RegistrationStrategy.Append</c> in <see cref="CqrsExtensions.AddEventHandlers"/>).
        /// </summary>
        public static IServiceCollection AddEventHandler<TEvent, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services)
            where TEvent : IEvent
            where THandler : class, IMessageHandler<TEvent>
            => services.AddTransient<IMessageHandler<TEvent>, THandler>();

        /// <summary>
        /// Registers <typeparamref name="THandler"/> as the handler for <typeparamref name="TQuery"/>, throwing if a
        /// handler is already registered — matching the single-handler-per-query semantics of the Scrutor-scanned
        /// path (<c>RegistrationStrategy.Throw</c> in <see cref="CqrsExtensions.AddQueryHandlers"/>).
        /// </summary>
        /// <exception cref="InvalidOperationException">A handler for <typeparamref name="TQuery"/> is already registered.</exception>
        public static IServiceCollection AddQueryHandler<TQuery, TResult, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] THandler>(this IServiceCollection services)
            where TQuery : class, IQuery<TResult>
            where THandler : class, IQueryHandler<TQuery, TResult>
        {
            if (services.Any(d => d.ServiceType == typeof(IQueryHandler<TQuery, TResult>)))
            {
                throw new InvalidOperationException($"A handler for '{typeof(TQuery).Name}' is already registered.");
            }

            return services.AddTransient<IQueryHandler<TQuery, TResult>, THandler>();
        }
    }
}
