using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers
{
    /// <remarks>
    /// Serializes and deserializes through <see cref="JsonSerializerOptions.GetTypeInfo(System.Type)"/>. Under
    /// Native AOT the body type MUST be declared in the <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>
    /// registered with WithAotJsonSerialization; an undeclared type throws <see cref="System.NotSupportedException"/>.
    /// </remarks>
    public class JsonBodyConverter : IBrokeredMessageBodyConverter
    {
        private readonly JsonSerializerOptions _options;

        // A JsonSerializerOptions registered via WithAotJsonSerialization is injected here; otherwise DI
        // resolves the default parameter value (no registration = no behavior change from before this ctor
        // param existed).
        public JsonBodyConverter(JsonSerializerOptions options = null)
            => _options = options ?? ChatterJson.ReflectionDefaultOrThrow();

        public string ContentType => "application/json";

        public TBody Convert<TBody>(byte[] body)
            => JsonSerializer.Deserialize(Stringify(body), (JsonTypeInfo<TBody>)_options.GetTypeInfo(typeof(TBody)));

        public byte[] Convert(object body)
            => GetBytes(Stringify(body));

        public string Stringify(byte[] body)
            => Encoding.UTF8.GetString(body);

        public string Stringify(object body)
            => body is null
                ? "null"
                : JsonSerializer.Serialize(body, _options.GetTypeInfo(body.GetType()));

        public byte[] GetBytes(string body)
            => Encoding.UTF8.GetBytes(body);
    }
}
