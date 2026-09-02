using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Pipeline;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitCommandBehaviorRegistrationExtensions
{
    public class WhenAddingCommandBehavior
    {
        [Fact]
        public void MustRegisterCommandBehavior()
        {
            var sc = new ServiceCollection();
            sc.AddCommandBehavior<FakeCommand, FakeCommandBehavior>();

            sc.Should().HaveCount(1);
            sc[0].Lifetime.Should().Be(ServiceLifetime.Transient);
            sc[0].ServiceType.Should().Be(typeof(ICommandBehavior<FakeCommand>));
            sc[0].ImplementationType.Should().Be(typeof(FakeCommandBehavior));
        }

        [Fact]
        public void MustRegisterAdditionalBehaviorAlongsideExisting()
        {
            var sc = new ServiceCollection();
            sc.AddCommandBehavior<FakeCommand, FakeCommandBehavior>();
            sc.AddCommandBehavior<FakeCommand, AnotherFakeCommandBehavior>();

            sc.Should().HaveCount(2);
            sc[0].ImplementationType.Should().Be(typeof(FakeCommandBehavior));
            sc[1].ImplementationType.Should().Be(typeof(AnotherFakeCommandBehavior));
        }

        [Fact]
        public void MustReturnSelf()
        {
            var sc = new ServiceCollection();
            var returnValue = sc.AddCommandBehavior<FakeCommand, FakeCommandBehavior>();
            returnValue.Should().BeSameAs(sc);
        }

        private class FakeCommand : ICommand { }

        private class FakeCommandBehavior : ICommandBehavior<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next) => throw new NotImplementedException();
        }

        private class AnotherFakeCommandBehavior : ICommandBehavior<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next) => throw new NotImplementedException();
        }
    }
}
