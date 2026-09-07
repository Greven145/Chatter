using Chatter.MessageBrokers.Routing.Options;
using System.Text.Json;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class SendOptionsExtensions
    {
        /// <remarks>
        /// Permanently trim/AOT-analyzer-flagged: <see cref="RoutingSlip"/> binds construction through a
        /// private <c>[JsonConstructor]</c>, which source generation cannot invoke (the same accessibility
        /// wall documented on <see cref="ChatterJson.CreateAotOptions"/> and deliberately excluded from
        /// <see cref="ChatterMessageBrokerJsonContext"/>) — this call stays on the reflection-based
        /// <see cref="System.Text.Json.JsonSerializerOptions"/> overload regardless of which options instance
        /// is passed.
        /// </remarks>
        public static SendOptions WithRoutingSlip(this SendOptions options, RoutingSlip slip)
        {
            var serializedRoutingSlip = JsonSerializer.Serialize(slip, ChatterJson.Options);
            options.WithMessageContext(MessageContext.RoutingSlip, serializedRoutingSlip);
            return options;
        }
    }
}
