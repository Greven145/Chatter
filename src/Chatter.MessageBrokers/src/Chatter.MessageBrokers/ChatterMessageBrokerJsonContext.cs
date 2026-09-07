using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Chatter.MessageBrokers
{
    /// <summary>
    /// Source-generated type metadata for the envelope shapes <see cref="ChatterJson.CreateAotOptions"/>
    /// needs beyond a consumer's own payload types: MessageContext header values materialize as
    /// <see cref="Dictionary{TKey, TValue}"/>/<see cref="List{T}"/> via <see cref="MaterializingObjectConverter"/>,
    /// whose <c>Write</c> re-dispatches on each boxed value's OWN runtime type. A source-gen resolver, unlike
    /// reflection, cannot synthesize metadata for a type discovered only at runtime, so an undeclared value
    /// type throws <see cref="NotSupportedException"/> at serialize time — every CLR type
    /// <see cref="MessageContext.MaterializeJsonElement"/> can PRODUCE on read
    /// (<see langword="long"/>/<see langword="double"/>/<see cref="DateTime"/>/<see langword="string"/>/
    /// <see langword="bool"/>, plus the two container shapes, recursively) needs a root declaration here, AND
    /// so does every CLR type Chatter's own receivers/senders WRITE into a live <c>MessageContext</c> before
    /// it is ever round-tripped through JSON: <see cref="int"/> (<c>ReceiveAttempts</c>, stamped by every
    /// receiver), <see cref="TimeSpan"/> (<c>TimeToLive</c>), <see cref="Guid"/> (SqlServiceBroker's
    /// conversation handles), and <see cref="ulong"/> (RabbitMQ's <c>DeliveryTag</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Routing.Slips.RoutingSlip"/>/<see cref="Routing.Slips.RoutingStep"/> are deliberately NOT
    /// included: both bind construction through a private <c>[JsonConstructor]</c>, which source generation
    /// cannot invoke (same accessibility wall documented on <see cref="ChatterJson.CreateAotOptions"/>) — they
    /// remain reflection-only, same as any private-member consumer DTO.
    /// </para>
    /// <para>
    /// PERMANENT, OPEN-WORLD LIMITATION: <c>MessageContext</c>/<see cref="Sending.OutboundBrokeredMessage.MessageContext"/>
    /// is a public <see cref="IDictionary{TKey, TValue}"/> — a consumer (or a future broker adapter) can stamp
    /// a header with any CLR type, not only the ones enumerated above. This context can only ever cover the
    /// finite set of types Chatter's own first-party code is known to write today; a value of an undeclared
    /// type stamped by consumer code still throws under the AOT dual path. This is inherent to representing
    /// a dynamically-typed dictionary through a closed, ahead-of-time type registry — not a gap that adding
    /// more <see cref="JsonSerializableAttribute"/> declarations can ever fully close.
    /// </para>
    /// </remarks>
    [JsonSerializable(typeof(Dictionary<string, object>))]
    [JsonSerializable(typeof(List<object>))]
    [JsonSerializable(typeof(IDictionary<string, object>))]
    [JsonSerializable(typeof(long))]
    [JsonSerializable(typeof(double))]
    [JsonSerializable(typeof(DateTime))]
    [JsonSerializable(typeof(string))]
    [JsonSerializable(typeof(bool))]
    [JsonSerializable(typeof(int))]
    [JsonSerializable(typeof(TimeSpan))]
    [JsonSerializable(typeof(Guid))]
    [JsonSerializable(typeof(ulong))]
    internal partial class ChatterMessageBrokerJsonContext : JsonSerializerContext
    {
    }
}
