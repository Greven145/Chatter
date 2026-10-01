using Chatter.MessageBrokers.Routing.Options;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class SendOptionsExtensions
    {
        public static SendOptions WithRoutingSlip(this SendOptions options, RoutingSlip slip, JsonSerializerOptions jsonOptions = null)
        {
            var effectiveOptions = jsonOptions ?? ChatterJson.ReflectionDefaultOrThrow();
            var serializedRoutingSlip = JsonSerializer.Serialize(slip, (JsonTypeInfo<RoutingSlip>)effectiveOptions.GetTypeInfo(typeof(RoutingSlip)));
            options.WithMessageContext(MessageContext.RoutingSlip, serializedRoutingSlip);
            return options;
        }
    }
}
