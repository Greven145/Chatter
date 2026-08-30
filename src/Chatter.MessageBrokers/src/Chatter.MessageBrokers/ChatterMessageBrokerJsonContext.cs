using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chatter.MessageBrokers
{
    /// <summary>
    /// Source-generated type metadata for the envelope shapes <see cref="ChatterJson.CreateAotOptions"/>
    /// needs beyond a consumer's own payload types: MessageContext header values materialize as
    /// <see cref="Dictionary{TKey, TValue}"/>/<see cref="List{T}"/> via <see cref="MaterializingObjectConverter"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Routing.Slips.RoutingSlip"/>/<see cref="Routing.Slips.RoutingStep"/> are deliberately NOT
    /// included: both bind construction through a private <c>[JsonConstructor]</c>, which source generation
    /// cannot invoke (same accessibility wall documented on <see cref="ChatterJson.CreateAotOptions"/>) — they
    /// remain reflection-only, same as any private-member consumer DTO.
    /// </remarks>
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<object>))]
    internal partial class ChatterMessageBrokerJsonContext : JsonSerializerContext
    {
    }
}
