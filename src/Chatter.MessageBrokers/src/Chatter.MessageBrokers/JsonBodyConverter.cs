using System.Text;
using System.Text.Json;

namespace Chatter.MessageBrokers
{
    /// <remarks>
    /// <see cref="Convert{TBody}(byte[])"/> and <see cref="Stringify(object)"/> handle an arbitrary
    /// runtime-determined body type — <c>Stringify(object)</c> serializes via <c>body.GetType()</c>, not a
    /// type fixed at compile time. There is no <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/>
    /// to obtain for a type unknown until runtime, so these calls stay on the
    /// <see cref="System.Text.Json.JsonSerializerOptions"/>-based overloads (permanently trim/AOT-analyzer-flagged
    /// by their own BCL signature, independent of which <see cref="System.Text.Json.JsonSerializerOptions"/> instance
    /// is actually passed in) — not a gap threading a different options instance can close.
    /// </remarks>
    public class JsonBodyConverter : IBrokeredMessageBodyConverter
    {
        private readonly JsonSerializerOptions _options;

        // A JsonSerializerOptions registered via WithAotJsonSerialization is injected here; otherwise DI
        // resolves the default parameter value (no registration = no behavior change from before this ctor
        // param existed).
        public JsonBodyConverter(JsonSerializerOptions options = null)
            => _options = options ?? ChatterJson.Options;

        public string ContentType => "application/json";

        public TBody Convert<TBody>(byte[] body)
            => JsonSerializer.Deserialize<TBody>(Stringify(body), _options);

        public byte[] Convert(object body)
            => GetBytes(Stringify(body));

        public string Stringify(byte[] body)
            => Encoding.UTF8.GetString(body);

        public string Stringify(object body)
            => body is null
                ? JsonSerializer.Serialize<object>(null, _options)
                : JsonSerializer.Serialize(body, body.GetType(), _options);

        public byte[] GetBytes(string body)
            => Encoding.UTF8.GetBytes(body);
    }
}
