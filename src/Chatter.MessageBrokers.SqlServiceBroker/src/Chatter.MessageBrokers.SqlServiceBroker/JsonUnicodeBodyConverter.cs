using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers.SqlServiceBroker
{
    /// <remarks>
    /// Serializes and deserializes through <see cref="JsonSerializerOptions.GetTypeInfo(System.Type)"/>. Under
    /// Native AOT the body type MUST be declared in the <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>
    /// registered with WithAotJsonSerialization; an undeclared type throws <see cref="System.NotSupportedException"/>.
    /// </remarks>
    public class JsonUnicodeBodyConverter : IBrokeredMessageBodyConverter
    {
        private readonly JsonSerializerOptions _options;

        public JsonUnicodeBodyConverter() : this(null) { }

        internal JsonUnicodeBodyConverter(JsonSerializerOptions options)
            => _options = options ?? (RuntimeFeature.IsDynamicCodeSupported
                ? ChatterJson.Options
                : throw new InvalidOperationException("No JsonSerializerOptions is available under Native AOT. Register a source-generated JsonSerializerContext with WithAotJsonSerialization."));

        public string ContentType => "application/json; charset=utf-16";

        public TBody Convert<TBody>(byte[] body)
            => JsonSerializer.Deserialize(Stringify(body), (JsonTypeInfo<TBody>)_options.GetTypeInfo(typeof(TBody)));

        public byte[] Convert(object body)
            => GetBytes(Stringify(body));

        public string Stringify(byte[] body)
            => Encoding.Unicode.GetString(body);

        public string Stringify(object body)
            => body is null
                ? "null"
                : JsonSerializer.Serialize(body, _options.GetTypeInfo(body.GetType()));

        public byte[] GetBytes(string body)
            => Encoding.Unicode.GetBytes(body);
    }
}
