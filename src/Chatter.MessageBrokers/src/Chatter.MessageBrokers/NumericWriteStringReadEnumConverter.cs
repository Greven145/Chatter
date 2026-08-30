using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Chatter.MessageBrokers
{
    /// <summary>
    /// Global enum <see cref="JsonConverter{T}"/> restoring Newtonsoft read-leniency parity for enum DTO
    /// properties: Newtonsoft's default <c>JsonConvert.DeserializeObject</c> accepted BOTH the enum NAME
    /// (e.g. <c>{"Status":"Booked"}</c>) and its numeric value on read, whereas System.Text.Json's shared
    /// <see cref="ChatterJson.Options"/> (no enum converter) reads numbers ONLY and throws
    /// <see cref="JsonException"/> on a name. This converter accepts names (case-insensitively, matching
    /// Newtonsoft) AND numbers on READ, while WRITING the numeric value so the wire output stays byte-identical
    /// to both the prior Newtonsoft serialization and the pre-converter STJ output (Newtonsoft's default and
    /// STJ's default both write enums as numbers). Golden byte-parity is therefore preserved.
    /// </summary>
    /// <remarks>
    /// READ-leniency-only by construction: the <see cref="JsonConverter{T}.Write"/> path emits the numeric
    /// value via <see cref="Utf8JsonWriter.WriteNumberValue(long)"/> (signed) / <c>WriteNumberValue(ulong)</c>
    /// (for <c>ulong</c>-backed enums), never the name, so no <c>WriteAsString</c>-style wire change is
    /// introduced. Nullable enum members are handled by STJ's surrounding nullable wrapper (it unwraps to the
    /// underlying enum type and dispatches here), so this converter only needs to match non-nullable enum types.
    /// Typed <c>T = object</c> (not a per-enum generic instantiation) so <see cref="CanConvert"/> alone
    /// selects applicability — no <c>MakeGenericType</c>/<c>Activator.CreateInstance</c> per enum type.
    /// </remarks>
    internal sealed class NumericWriteStringReadEnumConverter : JsonConverter<object>
    {
        public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            switch (reader.TokenType)
            {
                case JsonTokenType.String:
                    // Newtonsoft parsed enum names case-insensitively (and tolerated comma-separated
                    // [Flags] combinations). Enum.Parse(ignoreCase: true) restores both behaviors.
                    var name = reader.GetString();
                    try
                    {
                        return Enum.Parse(typeToConvert, name, ignoreCase: true);
                    }
                    catch (ArgumentException)
                    {
                        throw new JsonException(
                            $"The JSON value '{name}' could not be converted to {typeToConvert}.");
                    }

                case JsonTokenType.Number:
                    // STJ's own numeric read path. Route through the underlying integral type so values
                    // outside the int range (long/ulong-backed enums) round-trip.
                    return ReadNumber(ref reader, typeToConvert);

                default:
                    throw new JsonException(
                        $"Unexpected token {reader.TokenType} when reading enum {typeToConvert}.");
            }
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            // Write the NUMERIC value to preserve Newtonsoft/STJ default numeric wire parity.
            if (Enum.GetUnderlyingType(value.GetType()) == typeof(ulong))
            {
                writer.WriteNumberValue(Convert.ToUInt64(value));
            }
            else
            {
                writer.WriteNumberValue(Convert.ToInt64(value));
            }
        }

        private static object ReadNumber(ref Utf8JsonReader reader, Type typeToConvert)
        {
            if (Enum.GetUnderlyingType(typeToConvert) == typeof(ulong))
            {
                return Enum.ToObject(typeToConvert, reader.GetUInt64());
            }

            return Enum.ToObject(typeToConvert, reader.GetInt64());
        }
    }
}
