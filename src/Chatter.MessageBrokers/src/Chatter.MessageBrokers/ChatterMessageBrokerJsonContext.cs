using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chatter.MessageBrokers
{
    /// <summary>
    /// Source-generated type metadata for the envelope shapes <see cref="ChatterJson.CreateAotOptions"/>
    /// needs beyond a consumer's own payload types: MessageContext header values materialize as
    /// <see cref="Dictionary{TKey, TValue}"/>/<see cref="List{T}"/> via <see cref="MaterializingObjectConverter"/>,
    /// whose <c>Write</c> re-dispatches on each boxed value's OWN runtime type — every CLR type
    /// <see cref="MessageContext.MaterializeJsonElement"/> can produce (<see langword="long"/>,
    /// <see langword="double"/>, <see cref="DateTime"/>, <see langword="string"/>, <see langword="bool"/>,
    /// plus the two container shapes, recursively) therefore needs its own root declaration here too — a
    /// source-gen resolver, unlike reflection, cannot synthesize metadata for a type discovered only at
    /// runtime, so an undeclared leaf value type throws <see cref="NotSupportedException"/> at serialize time.
    /// </summary>
    /// <remarks>
    /// <see cref="Routing.Slips.RoutingSlip"/>/<see cref="Routing.Slips.RoutingStep"/> are deliberately NOT
    /// included: both bind construction through a private <c>[JsonConstructor]</c>, which source generation
    /// cannot invoke (same accessibility wall documented on <see cref="ChatterJson.CreateAotOptions"/>) — they
    /// remain reflection-only, same as any private-member consumer DTO.
    /// </remarks>
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    internal partial class ChatterMessageBrokerJsonContext : JsonSerializerContext
    {
    }
}
