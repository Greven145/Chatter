using Chatter.MessageBrokers.Receiving;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class InboundBrokeredMessageExtensions
    {
        public static InboundBrokeredMessage WithRoutingSlip(this InboundBrokeredMessage message, RoutingSlip slip, JsonSerializerOptions jsonOptions = null)
        {
            var effectiveOptions = jsonOptions ?? ChatterJson.ReflectionDefaultOrThrow();
            var serializedRoutingSlip = JsonSerializer.Serialize(slip, (JsonTypeInfo<RoutingSlip>)effectiveOptions.GetTypeInfo(typeof(RoutingSlip)));
            message.MessageContextImpl[MessageContext.RoutingSlip] = serializedRoutingSlip;
            return message;
        }
    }
}
