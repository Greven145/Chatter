using Chatter.CQRS.Context;
using Chatter.CQRS.Events;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitHandlerRegistrationExtensions
{
    public class WhenAddingEventHandler
    {
        [Fact]
        public void MustRegisterEventHandler()
        {
            var sc = new ServiceCollection();
            sc.AddEventHandler<FakeEvent, FakeEventHandler>();

            sc.Should().HaveCount(1);
            sc[0].Lifetime.Should().Be(ServiceLifetime.Transient);
            sc[0].ServiceType.Should().Be(typeof(IMessageHandler<FakeEvent>));
            sc[0].ImplementationType.Should().Be(typeof(FakeEventHandler));
        }

        [Fact]
        public void MustAppendAdditionalHandlersForSameEvent()
        {
            var sc = new ServiceCollection();
            sc.AddEventHandler<FakeEvent, FakeEventHandler>();
            sc.AddEventHandler<FakeEvent, FakeEventHandler2>();

            sc.Should().HaveCount(2);
            sc.Select(sd => sd.ImplementationType).Should().BeEquivalentTo(new[] { typeof(FakeEventHandler), typeof(FakeEventHandler2) });
        }

        [Fact]
        public void MustReturnSelf()
        {
            var sc = new ServiceCollection();
            var returnValue = sc.AddEventHandler<FakeEvent, FakeEventHandler>();
            returnValue.Should().BeSameAs(sc);
        }

        private class FakeEvent : IEvent { }
        private class FakeEventHandler : IMessageHandler<FakeEvent>
        {
            public Task Handle(FakeEvent message, IMessageHandlerContext context) => throw new NotImplementedException();
        }

        private class FakeEventHandler2 : IMessageHandler<FakeEvent>
        {
            public Task Handle(FakeEvent message, IMessageHandlerContext context) => throw new NotImplementedException();
        }
    }
}
