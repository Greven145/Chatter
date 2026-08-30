using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace Chatter.MessageBrokers.Tests.Serialization.UsingChatterJson
{
    // ====================================================================================
    // ChatterJson.CreateAotOptions — the opt-in, source-generation-backed sibling of
    // ChatterJson.Options added by the AOT initiative's Phase 3. Options itself stays untouched;
    // these tests pin the new method's own contract.
    // ====================================================================================
    public partial class WhenCreatingAotOptions : Testing.Core.Context
    {
        private class Poco
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        private class PrivateSetterPoco
        {
            private PrivateSetterPoco() { }
            public string Name { get; private set; }
        }

        [JsonSerializable(typeof(Poco))]
        private partial class PocoJsonContext : JsonSerializerContext
        {
        }

        [JsonSerializable(typeof(PrivateSetterPoco))]
        private partial class PrivateSetterPocoJsonContext : JsonSerializerContext
        {
        }

        [Fact]
        public void MustThrowWhenConsumerContextIsNull()
        {
            Action act = () => ChatterJson.CreateAotOptions(null);

            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void MustReturnADifferentInstanceThanTheReflectionDefault()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);

            options.Should().NotBeSameAs(ChatterJson.Options);
        }

        [Fact]
        public void MustRoundTripAConsumerContextCoveredTypeThroughTheSourceGeneratedResolver()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);
            var original = new Poco { Name = "abc", Value = 42 };

            var json = JsonSerializer.Serialize(original, options);
            var roundTripped = JsonSerializer.Deserialize<Poco>(json, options);

            roundTripped.Should().NotBeNull();
            roundTripped.Name.Should().Be(original.Name);
            roundTripped.Value.Should().Be(original.Value);
        }

        // PARITY: the AOT path shares every non-reflection setting with ChatterJson.Options (encoder,
        // read leniencies, IncludeFields, Populate) — only the TypeInfoResolver differs. Extracted from
        // Options into a shared builder specifically so the two can't drift apart independently.
        [Fact]
        public void MustPreserveTheSameEncoderAndReadLeniencySettingsAsTheReflectionDefault()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);

            options.Encoder.Should().BeSameAs(ChatterJson.Options.Encoder);
            options.PropertyNameCaseInsensitive.Should().Be(ChatterJson.Options.PropertyNameCaseInsensitive);
            options.IncludeFields.Should().Be(ChatterJson.Options.IncludeFields);
            options.PreferredObjectCreationHandling.Should().Be(ChatterJson.Options.PreferredObjectCreationHandling);
            options.NumberHandling.Should().Be(ChatterJson.Options.NumberHandling);
            options.AllowTrailingCommas.Should().Be(ChatterJson.Options.AllowTrailingCommas);
            options.ReadCommentHandling.Should().Be(ChatterJson.Options.ReadCommentHandling);
        }

        // The permanent, documented limitation: source generation cannot touch a private member.
        // Unlike ChatterJson.Options (reflection + EnableNonPublicSetters/EnableNonPublicParameterlessConstructor),
        // this is not a gap Phase 3 (or any future phase) closes for the AOT path.
        [Fact]
        public void MustThrowWhenDeserializingAPrivateMemberDtoUnderSourceGeneration()
        {
            var options = ChatterJson.CreateAotOptions(PrivateSetterPocoJsonContext.Default);

            Action act = () => JsonSerializer.Deserialize<PrivateSetterPoco>("{\"Name\":\"abc\"}", options);

            act.Should().Throw<NotSupportedException>();
        }

        // Envelope-shape coverage: Chatter's own internal context (combined in behind the consumer's)
        // supplies Dictionary<string,object>/List<object> metadata for MessageContext header values
        // materialized via the shared MaterializingObjectConverter, without the consumer needing to
        // declare those BCL shapes in their own JsonSerializerContext.
        [Fact]
        public void MustRoundTripDictionaryOfObjectEnvelopeShapeWithoutConsumerDeclaringIt()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);

            var deserialized = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, object>>(
                "{\"count\":3,\"name\":\"abc\"}", options);

            deserialized["count"].Should().BeOfType<long>().And.Be(3L);
            deserialized["name"].Should().BeOfType<string>().And.Be("abc");
        }
    }
}
