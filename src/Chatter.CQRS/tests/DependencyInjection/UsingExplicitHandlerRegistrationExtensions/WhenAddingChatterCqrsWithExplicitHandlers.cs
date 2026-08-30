using Chatter.CQRS;
using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Queries;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitHandlerRegistrationExtensions
{
    public class WhenAddingChatterCqrsWithExplicitHandlers
    {
        private readonly IConfiguration _configuration = new Mock<IConfiguration>().Object;

        [Fact]
        public void MustRegisterCoreCqrsServicesWithoutScanningForHandlers()
        {
            var sc = new ServiceCollection();
            sc.AddChatterCqrsWithExplicitHandlers(_configuration);

            sc.Should().Contain(sd => sd.ServiceType == typeof(IMessageDispatcher));
            sc.Should().Contain(sd => sd.ServiceType == typeof(IQueryDispatcher));
            sc.Should().Contain(sd => sd.ServiceType == typeof(IExternalDispatcher));
            sc.Should().NotContain(sd => sd.ServiceType.IsGenericType && sd.ServiceType.GetGenericTypeDefinition() == typeof(IMessageHandler<>));
            sc.Should().NotContain(sd => sd.ServiceType.IsGenericType && sd.ServiceType.GetGenericTypeDefinition() == typeof(IQueryHandler<,>));
        }

        [Fact]
        public void MustReturnChatterBuilder()
        {
            var sc = new ServiceCollection();
            var builder = sc.AddChatterCqrsWithExplicitHandlers(_configuration);
            builder.Should().NotBeNull();
            builder.Services.Should().BeSameAs(sc);
        }

        [Fact]
        public async Task MustDispatchToExplicitlyRegisteredCommandHandler()
        {
            var sc = new ServiceCollection();
            sc.AddLogging();
            sc.AddChatterCqrsWithExplicitHandlers(_configuration);
            sc.AddCommandHandler<FakeCommand, FakeCommandHandler>();

            var provider = sc.BuildServiceProvider();
            using var scope = provider.CreateScope();
            var dispatcher = scope.ServiceProvider.GetRequiredService<IMessageDispatcher>();

            var command = new FakeCommand();
            await dispatcher.Dispatch(command);

            command.WasHandled.Should().BeTrue();
        }

        private class FakeCommand : ICommand
        {
            public bool WasHandled { get; set; }
        }

        private class FakeCommandHandler : IMessageHandler<FakeCommand>
        {
            public Task Handle(FakeCommand message, IMessageHandlerContext context)
            {
                message.WasHandled = true;
                return Task.CompletedTask;
            }
        }
    }
}
