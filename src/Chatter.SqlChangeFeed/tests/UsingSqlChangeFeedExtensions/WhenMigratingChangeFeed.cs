using Chatter.SqlChangeFeed.DependencyInjection;
using FluentAssertions;
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
            public List<(string Install, string Uninstall, string Queue, string Service, string Trigger, string DeadLetterQueue, string DeadLetterService)> InstallCalls { get; } = new();

            public Task InstallSqlDependencies(string installationProcedureName = "",
                                                string uninstallationProcedureName = "",
                                                string conversationQueueName = "",
                                                string conversationServiceName = "",
                                                string conversationTriggerName = "",
                                                string deadLetterQueueName = "",
                                                string deadLetterServiceName = "",
                                                CancellationToken token = default)
            {
                InstallCalls.Add((installationProcedureName, uninstallationProcedureName, conversationQueueName, conversationServiceName, conversationTriggerName, deadLetterQueueName, deadLetterServiceName));
                return Task.CompletedTask;
            }

            public Task UninstallSqlDependencies(string uninstallationProcedureName = "", CancellationToken token = default)
                => Task.CompletedTask;
        }

        private static IServiceProvider BuildProvider(RecordingSqlDependencyManager manager)
        {
            var services = new ServiceCollection();
            services.AddSingleton<ISqlDependencyManager<FakeRowData>>(manager);
            return services.BuildServiceProvider();
        }

        [Fact]
        public void MustInstallDependenciesWithNamesDerivedFromRowTypeName_GenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var provider = BuildProvider(manager);

            provider.UseChangeFeedSqlMigrations<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public void MustInstallSameDependenciesViaTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            BuildProvider(managerGeneric).UseChangeFeedSqlMigrations<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            BuildProvider(managerTyped).UseChangeFeedSqlMigrations(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }

        [Fact]
        public async Task MustInstallDependenciesAsync_GenericOverload()
        {
            var manager = new RecordingSqlDependencyManager();
            var provider = BuildProvider(manager);

            await provider.UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            manager.InstallCalls.Should().ContainSingle();
            manager.InstallCalls[0].Queue.Should().Contain(nameof(FakeRowData));
        }

        [Fact]
        public async Task MustInstallSameDependenciesAsyncViaTypeBasedOverload()
        {
            var managerGeneric = new RecordingSqlDependencyManager();
            await BuildProvider(managerGeneric).UseChangeFeedSqlMigrationsAsync<FakeRowData>();

            var managerTyped = new RecordingSqlDependencyManager();
            await BuildProvider(managerTyped).UseChangeFeedSqlMigrationsAsync(typeof(FakeRowData));

            managerTyped.InstallCalls.Should().BeEquivalentTo(managerGeneric.InstallCalls);
        }
    }
}
