using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using FluentAssertions;
using Xunit;

namespace Chatter.MessageBrokers.Tests.UsingBodyConverterFactory
{
    // Adversarial-review finding: BodyConverterFactory's fallback (unrecognized content type) built a
    // raw `new JsonBodyConverter()` directly, bypassing DI and staying reflection-based regardless of
    // WithAotJsonSerialization. Fixed via the same optional-JsonSerializerOptions-parameter pattern as
    // JsonBodyConverter itself; DI (AddScoped<IBodyConverterFactory, BodyConverterFactory>) resolves it
    // automatically since the factory is itself DI-constructed.
    public partial class WhenCreatingBodyConverter : Testing.Core.Context
    {
        private class FactoryPrivateCtorPoco
        {
            private FactoryPrivateCtorPoco() { }
            public string Name { get; set; }
        }

        [JsonSerializable(typeof(FactoryPrivateCtorPoco))]
        private partial class FactoryPrivateCtorPocoJsonContext : JsonSerializerContext
        {
        }

        // Behavioral proof (not just object-type), distinguishing which resolver the fallback actually
        // used: a private-parameterless-ctor DTO only deserializes on the reflection default
        // (EnableNonPublicParameterlessConstructor).
        [Fact]
        public void MustFallBackToReflectionOptionsForUnrecognizedContentTypeByDefault()
        {
            var sut = new BodyConverterFactory(new List<IBrokeredMessageBodyConverter>());

            var converter = sut.CreateBodyConverter("application/unrecognized");
            var bytes = converter.GetBytes("{\"Name\":\"abc\"}");

            converter.Convert<FactoryPrivateCtorPoco>(bytes).Name.Should().Be("abc");
        }

        // The same DTO throws once WithAotJsonSerialization's options reach the fallback — proving the
        // injected options are genuinely wired through, not ignored.
        [Fact]
        public void MustUseInjectedAotOptionsForUnrecognizedContentTypeWhenProvided()
        {
            var aotOptions = ChatterJson.CreateAotOptions(FactoryPrivateCtorPocoJsonContext.Default);
            var sut = new BodyConverterFactory(new List<IBrokeredMessageBodyConverter>(), aotOptions);

            var converter = sut.CreateBodyConverter("application/unrecognized");
            var bytes = converter.GetBytes("{\"Name\":\"abc\"}");

            Action act = () => converter.Convert<FactoryPrivateCtorPoco>(bytes);

            act.Should().Throw<NotSupportedException>();
        }
    }
}
