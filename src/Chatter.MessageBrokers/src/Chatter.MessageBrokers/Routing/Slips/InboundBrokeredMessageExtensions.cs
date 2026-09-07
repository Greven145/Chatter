using Chatter.MessageBrokers.Receiving;
using System.Text.Json;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class InboundBrokeredMessageExtensions
    {
        /// <remarks>
        /// Same permanent limitation as <see cref="SendOptionsExtensions.WithRoutingSlip"/>: <see cref="RoutingSlip"/>'s
        /// private <c>[JsonConstructor]</c> is unreachable by source generation, so this call stays on the
        /// reflection-based <see cref="System.Text.Json.JsonSerializerOptions"/> overload regardless of which
        /// options instance is passed.
        /// </remarks>
        public static InboundBrokeredMessage WithRoutingSlip(this InboundBrokeredMessage message, RoutingSlip slip)
        {
            var serializedRoutingSlip = JsonSerializer.Serialize(slip, ChatterJson.Options);
            message.MessageContextImpl[MessageContext.RoutingSlip] = serializedRoutingSlip;
            return message;
        }
    }
}
