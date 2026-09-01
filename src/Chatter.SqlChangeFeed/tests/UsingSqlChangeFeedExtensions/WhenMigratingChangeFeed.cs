using Chatter.SqlChangeFeed.DependencyInjection;
using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Chatter.SqlChangeFeed.Tests.UsingSqlChangeFeedExtensions
{
    public class WhenMigratingChangeFeed : Testing.Core.Context
    {
        private sealed class RecordingSqlDependencyManager : ISqlDependencyManager<FakeRowData>
        {
            public List<(string Install, string Uninstall, string Queue, string Service, string Trigger, string DeadLetterQueue, string DeadLetterService, CancellationToken Token)> InstallCalls { get; } = new();

            public Task InstallSqlDependencies(string installationProcedureName = "",
                                                string uninstallationProcedureName = "",
                                                string conversationQueueName = "",
                                                string conversationServiceName = "",
                                                string conversationTriggerName = "",
                                                string deadLetterQueueName = "",
                                                string deadLetterServiceName = "",
                                                CancellationToken token = default)
            {
                InstallCalls.Add((installationProcedureName, uninstallationProcedureName, conversationQueueName, conversationServiceName, conversationTriggerName, deadLetterQueueName, deadLetterServiceName, token));
                return Task.CompletedTask;
            }

            public Task UninstallSqlDependencies(string uninstallationProcedureName = "", CancellationToken token = default)
                => Task.CompletedTask;
        }

        private sealed class FakeApplicationBuilder : IApplicationBuilder
        {
            public FakeApplicationBuilder(IServiceProvider services) => ApplicationServices = services;

            public IServiceProvider ApplicationServices { get; set; }
            public IFeatureCollection ServerFeatures => throw new NotSupportedException();
            public IDictionary<string, object> Properties => throw new NotSupportedException();
            public IApplicationBuilder Use(Func<RequestDelegate, RequestDelegate> middleware) => throw new NotSupportedException();
            public IApplicationBuilder New() => throw new NotSupportedException();
            public RequestDelegate Build() => throw new NotSupportedException();
        }

        // Registered Scoped to match AddSqlChangeFeed<T>'s real production lifetime: a Singleton double would
        // pass identically whether or not the implementation's `using var scope = provider.CreateScope()`
        // actually executes, masking a regression that silently dropped scope creation.
        private static IServiceProvider BuildProvider(RecordingSqlDependencyManager manager)
        {
            var services = new ServiceCollection();
            services.AddScoped<ISqlDependencyManager<FakeRowData>>(_ => manager);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void MustInstallDependenciesWithNamesDerivedFromRowTypeName_ProviderGenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var provider = BuildProvider(manager);

            provider.UseChangeFeedSqlMigrations<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public void MustInstallSameDependenciesViaProviderTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            BuildProvider(managerGeneric).UseChangeFeedSqlMigrations<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            BuildProvider(managerTyped).UseChangeFeedSqlMigrations(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }

        [Fact]
        public async Task MustInstallDependenciesAsync_ProviderGenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var provider = BuildProvider(manager);

            await provider.UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public async Task MustInstallSameDependenciesAsyncViaProviderTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            await BuildProvider(managerGeneric).UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            await BuildProvider(managerTyped).UseChangeFeedSqlMigrationsAsync(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }

        [Fact]
        public void MustPropagateCancellationTokenToInstallSqlDependencies()
        {
            var manager = new RecordingSqlDependencyManager();
            var provider = BuildProvider(manager);
            using var cts = new CancellationTokenSource();

            provider.UseChangeFeedSqlMigrations<FakeRowData>(cts.Token);

            manager.InstallCalls[0].Token.Should().Be(cts.Token);
        }

        [Fact]
        public void MustInstallDependenciesWithNamesDerivedFromRowTypeName_ApplicationBuilderGenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var applicationBuilder = new FakeApplicationBuilder(BuildProvider(manager));

            applicationBuilder.UseChangeFeedSqlMigrations<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public void MustInstallSameDependenciesViaApplicationBuilderTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            new FakeApplicationBuilder(BuildProvider(managerGeneric)).UseChangeFeedSqlMigrations<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            new FakeApplicationBuilder(BuildProvider(managerTyped)).UseChangeFeedSqlMigrations(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }

        [Fact]
        public async Task MustInstallDependenciesAsync_ApplicationBuilderGenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var applicationBuilder = new FakeApplicationBuilder(BuildProvider(manager));

            await applicationBuilder.UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public async Task MustInstallSameDependenciesAsyncViaApplicationBuilderTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            await new FakeApplicationBuilder(BuildProvider(managerGeneric)).UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            await new FakeApplicationBuilder(BuildProvider(managerTyped)).UseChangeFeedSqlMigrationsAsync(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }
    }
}
