using Chatter.CQRS.Context;
using Chatter.CQRS.Queries;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.CQRS.Tests.DependencyInjection.UsingExplicitHandlerRegistrationExtensions
{
    public class WhenAddingQueryHandler
    {
        [Fact]
        public void MustRegisterQueryHandler()
        {
            var sc = new ServiceCollection();
            sc.AddQueryHandler<FakeQuery, string, FakeQueryHandler>();

            sc.Should().HaveCount(1);
            sc[0].Lifetime.Should().Be(ServiceLifetime.Transient);
            sc[0].ServiceType.Should().Be(typeof(IQueryHandler<FakeQuery, string>));
            sc[0].ImplementationType.Should().Be(typeof(FakeQueryHandler));
        }

        [Fact]
        public void MustThrowWhenHandlerAlreadyRegisteredForQuery()
        {
            var sc = new ServiceCollection();
            sc.AddQueryHandler<FakeQuery, string, FakeQueryHandler>();

            FluentActions.Invoking(() => sc.AddQueryHandler<FakeQuery, string, FakeQueryHandler2>())
                .Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void MustReturnSelf()
        {
            var sc = new ServiceCollection();
            var returnValue = sc.AddQueryHandler<FakeQuery, string, FakeQueryHandler>();
            returnValue.Should().BeSameAs(sc);
        }

        private class FakeQuery : IQuery<string> { }
        private class FakeQueryHandler : IQueryHandler<FakeQuery, string>
        {
            public Task<string> Handle(FakeQuery query, IQueryHandlerContext context) => throw new NotImplementedException();
        }

        private class FakeQueryHandler2 : IQueryHandler<FakeQuery, string>
        {
            public Task<string> Handle(FakeQuery query, IQueryHandlerContext context) => throw new NotImplementedException();
        }
    }
}
