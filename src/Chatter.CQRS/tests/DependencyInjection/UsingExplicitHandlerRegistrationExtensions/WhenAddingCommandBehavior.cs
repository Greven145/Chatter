using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Pipeline;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitHandlerRegistrationExtensions
{
    public class WhenAddingCommandBehavior
    {
        private readonly IConfiguration _configuration = new Mock<IConfiguration>().Object;

        [Fact]
        public void MustRegisterBothWhenCalledTwiceForSameCommandAndBehaviorPair()
        {
            var sc = new ServiceCollection();
            sc.AddCommandBehavior<FakeCommand, FakeCommandBehavior>();
            sc.AddCommandBehavior<FakeCommand, FakeCommandBehavior>();

            sc.Should().HaveCount(2);
            sc[0].ImplementationType.Should().Be(typeof(FakeCommandBehavior));
            sc[1].ImplementationType.Should().Be(typeof(FakeCommandBehavior));
        }

        [Fact]
        public async Task MustExecuteBehaviorTwiceWhenRegisteredTwiceForSameCommandAndBehaviorPair()
        {
            // Documents pre-existing, unchanged behavior (same as RegisterBehaviorForCommand and
            // AddReceiver<TMessage>): no duplicate-registration guard. Two registrations of the same
            // TCommand/TCommandBehavior pair both run, once each, per dispatch.
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddChatterCqrsWithExplicitHandlers(_configuration);
            sc.AddCommandHandler<FakeCommand, FakeCommandHandler>();
            sc.AddCommandBehavior<FakeCommand, CountingCommandBehavior>();
            sc.AddCommandBehavior<FakeCommand, CountingCommandBehavior>();

            using var provider = sc.BuildServiceProvider();
            var dispatcher = provider.GetRequiredService<IMessageDispatcher>();

            var command = new FakeCommand();
            await dispatcher.Dispatch(command);

            command.ExecutionCount.Should().Be(2);
        }

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

        private class FakeCommand : ICommand
        {
            public int ExecutionCount { get; set; }
        }

        private class FakeCommandHandler : IMessageHandler<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext context) => Task.CompletedTask;
        }

        private class FakeCommandBehavior : ICommandBehavior<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next) => throw new NotImplementedException();
        }

        private class AnotherFakeCommandBehavior : ICommandBehavior<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next) => throw new NotImplementedException();
        }

        private class CountingCommandBehavior : ICommandBehavior<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next)
            {
                message.ExecutionCount++;
                return next();
            }
        }
    }
}
