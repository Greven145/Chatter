using System.Text;
using System.Text.Json;

namespace Chatter.MessageBrokers.RabbitMQ
{
    /// <remarks>
    /// Same permanent structural limitation as <see cref="Chatter.MessageBrokers.JsonBodyConverter"/>:
    /// <see cref="Convert{TBody}(byte[])"/>/<see cref="Stringify(object)"/> handle an arbitrary
    /// runtime-determined body type, so there is no fixed
    /// <see cref="System.Text.Json.Serialization.Metadata.JsonTypeInfo{T}"/> to route through — these calls
    /// stay on the always-flagged <see cref="System.Text.Json.JsonSerializerOptions"/>-based overloads regardless
    /// of which options instance is passed.
    /// </remarks>
    public class RabbitMqBodyConverter : IBrokeredMessageBodyConverter
    {
        public string ContentType => "application/json; charset=utf-8";

        public TBody Convert<TBody>(byte[] body)
            => JsonSerializer.Deserialize<TBody>(Stringify(body), ChatterJson.Options);

        public byte[] Convert(object body)
            => GetBytes(Stringify(body));

        public string Stringify(byte[] body)
            => Encoding.UTF8.GetString(body);

        public string Stringify(object body)
            => JsonSerializer.Serialize(body, ChatterJson.Options);

        public byte[] GetBytes(string body)
            => Encoding.UTF8.GetBytes(body);
    }
}
