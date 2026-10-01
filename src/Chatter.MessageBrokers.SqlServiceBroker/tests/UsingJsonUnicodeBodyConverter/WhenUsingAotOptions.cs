using FluentAssertions;
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace Chatter.MessageBrokers.SqlServiceBroker.Tests.UsingJsonUnicodeBodyConverter
{
    // Pins the GetTypeInfo-based dual path added alongside the AOT initiative (mirrors
    // RabbitMqBodyConverter's equivalent contract). The parameterless public ctor keeps the
    // AS-IS behavior pinned in WhenConverting.cs unchanged.
    public partial class WhenUsingAotOptions : Testing.Core.Context
    {
        private class Poco
        {
            public string Name { get; set; }
            public int Value { get; set; }
        }

        [JsonSerializable(typeof(Poco))]
        private partial class PocoJsonContext : JsonSerializerContext
        {
        }

        [Fact]
        public void MustRoundTripThroughSuppliedOptions()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);
            var sut = new JsonUnicodeBodyConverter(options);
            var original = new Poco { Name = "abc", Value = 42 };

            var bytes = sut.Convert(original);
            var result = sut.Convert<Poco>(bytes);

            result.Name.Should().Be("abc");
            result.Value.Should().Be(42);
        }

        [Fact]
        public void MustProduceSameWireBytesAsReflectionDefault()
        {
            var aotSut = new JsonUnicodeBodyConverter(ChatterJson.CreateAotOptions(PocoJsonContext.Default));
            var reflectionSut = new JsonUnicodeBodyConverter();
            var body = new Poco { Name = "abc", Value = 42 };

            aotSut.Convert(body).Should().Equal(reflectionSut.Convert(body));
        }

        private class UndeclaredPoco
        {
            public string Name { get; set; }
        }

        [Fact]
        public void MustThrowForUndeclaredTypeUnderSuppliedOptions()
        {
            var options = ChatterJson.CreateAotOptions(PocoJsonContext.Default);
            var sut = new JsonUnicodeBodyConverter(options);

            Action act = () => sut.Convert(new UndeclaredPoco { Name = "abc" });

            act.Should().Throw<NotSupportedException>();
        }
    }
}
