using Chatter.CQRS.Pipeline;
using Chatter.MessageBrokers.Routing.Slips;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class CommandPipelineBuilderExtensions
    {
        /// <remarks>
        /// Registers <see cref="RoutingSlipBehavior{TCommand}"/> as an open generic via
        /// <see cref="CommandPipelineBuilder.WithBehavior(System.Type)"/>, which resolves through
        /// <c>RegisterBehaviorForAllCommands</c>'s reflection-based assembly scan. RoutingSlip support
        /// stays unavailable under Native AOT through this registration path until that scan has an
        /// explicit, source-generation-free alternative — see brenpike/Chatter#406/#408.
        /// </remarks>
        public static CommandPipelineBuilder WithRoutingSlipBehavior(this CommandPipelineBuilder pipelineBuilder)
        {
            pipelineBuilder.WithBehavior(typeof(RoutingSlipBehavior<>));
            return pipelineBuilder;
        }
    }
}
