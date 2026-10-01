using System;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers.RabbitMQ
{
    /// <remarks>
    /// Serializes and deserializes through <see cref="JsonSerializerOptions.GetTypeInfo(System.Type)"/>. Under
    /// Native AOT the body type MUST be declared in the <see cref="System.Text.Json.Serialization.JsonSerializerContext"/>
    /// registered with WithAotJsonSerialization; an undeclared type throws <see cref="System.NotSupportedException"/>.
    /// </remarks>
    public class RabbitMqBodyConverter : IBrokeredMessageBodyConverter
    {
        private readonly JsonSerializerOptions _options;

        public RabbitMqBodyConverter() : this(null) { }

        internal RabbitMqBodyConverter(JsonSerializerOptions options)
            => _options = options ?? (RuntimeFeature.IsDynamicCodeSupported
                ? ChatterJson.Options
                : throw new InvalidOperationException("No JsonSerializerOptions is available under Native AOT. Register a source-generated JsonSerializerContext with WithAotJsonSerialization."));

        public string ContentType => "application/json; charset=utf-8";

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
