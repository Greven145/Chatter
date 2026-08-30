using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitHandlerRegistrationExtensions
{
    public class WhenAddingCommandHandler
    {
        [Fact]
        public void MustRegisterCommandHandler()
        {
            var sc = new ServiceCollection();
            sc.AddCommandHandler<FakeCommand, FakeCommandHandler>();

            sc.Should().HaveCount(1);
            sc[0].Lifetime.Should().Be(ServiceLifetime.Transient);
            sc[0].ServiceType.Should().Be(typeof(IMessageHandler<FakeCommand>));
            sc[0].ImplementationType.Should().Be(typeof(FakeCommandHandler));
        }

        [Fact]
        public void MustReplaceExistingRegistration()
        {
            var sc = new ServiceCollection();
            sc.AddCommandHandler<FakeCommand, FakeCommandHandler>();
            sc.AddCommandHandler<FakeCommand, FakeCommandHandler2>();

            sc.Should().HaveCount(1);
            sc.Single().ImplementationType.Should().Be(typeof(FakeCommandHandler2));
        }

        [Fact]
        public void MustReturnSelf()
        {
            var sc = new ServiceCollection();
            var returnValue = sc.AddCommandHandler<FakeCommand, FakeCommandHandler>();
            returnValue.Should().BeSameAs(sc);
        }

        private class FakeCommand : ICommand { }
        private class FakeCommandHandler : IMessageHandler<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext context) => throw new NotImplementedException();
        }

        private class FakeCommandHandler2 : IMessageHandler<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext context) => throw new NotImplementedException();
        }
    }
}
