using Chatter.CQRS.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Chatter.CQRS.Pipeline
{
    public class CommandPipelineBuilder
    {
        public IServiceCollection Services { get; private set; }

        internal CommandPipelineBuilder(IServiceCollection services)
            => Services = services ?? throw new ArgumentNullException(nameof(services));

        [RequiresUnreferencedCode("Reflects over behaviorType's implemented interfaces and, for an open generic behavior, scans its assembly via Scrutor to find and register implementations.")]
        [RequiresDynamicCode("For a closed generic behavior type, resolves the closed ICommandBehavior<> interface via Type.MakeGenericType.")]
        public CommandPipelineBuilder WithBehavior<TCommandBehavior>()
            => WithBehavior(typeof(TCommandBehavior));

        [RequiresUnreferencedCode("Reflects over behaviorType's implemented interfaces and, for an open generic behavior, scans its assembly via Scrutor to find and register implementations.")]
        [RequiresDynamicCode("For a closed generic behavior type, resolves the closed ICommandBehavior<> interface via Type.MakeGenericType.")]
        public CommandPipelineBuilder WithBehavior(Type behaviorType)
        {
            Services.AddPipelineBehavior(behaviorType);
            return this;
        }
    }
}
