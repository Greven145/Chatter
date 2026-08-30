using Chatter.CQRS;
using Chatter.CQRS.DependencyInjection;
using Chatter.MessageBrokers.Receiving;
using Chatter.SqlChangeFeed.Configuration;
using Chatter.SqlChangeFeed.Scripts;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.SqlChangeFeed.DependencyInjection
{
    public static class SqlChangeFeedExtensions

    {
        internal static SqlChangeFeedOptionsBuilder AddSqlChangeFeedOptionsBuilder(this IServiceCollection services, string connectionString, string tableName, string databaseName = null)
            => new SqlChangeFeedOptionsBuilder(services, connectionString, databaseName, tableName);

        /// <summary>
        /// Configures a change feed for specified table
        /// </summary>
        /// <param name="rowChangedDataType">A type implementing <see cref="IMessage"/> that maps to a row that changed in the target database</param>
        /// <param name="connectionString">The connection string for the sql server with the database and table to watch for changes</param>
        /// <param name="databaseName">Optional. The database containing the table to watch. If not specified, Database or InitialCatalog of the connectionString will be used.</param>
        /// <param name="tableName">The name of the table to watch</param>
        /// <param name="optionsBuilder">An optional builder allowing more complex change feed configuration</param>
        [RequiresUnreferencedCode("Invokes the generic AddSqlChangeFeed<TRowChangedData> overload via MakeGenericMethod. Use that overload directly for an AOT-safe, closed-generic alternative.")]
        [RequiresDynamicCode("Invokes the generic AddSqlChangeFeed<TRowChangedData> overload via MakeGenericMethod. Use that overload directly for an AOT-safe, closed-generic alternative.")]
        public static IChatterBuilder AddSqlChangeFeed(this IChatterBuilder builder,
                                                       Type rowChangedDataType,
                                                       string connectionString,
                                                       string databaseName,
                                                       string tableName,
                                                       Action<SqlChangeFeedOptionsBuilder> optionsBuilder = null)
        {
            typeof(SqlChangeFeedExtensions).GetMethods()
                             .Where(m => m.IsGenericMethod
                                         && m.Name == nameof(AddSqlChangeFeed))
                             .FirstOrDefault()
                             .MakeGenericMethod(rowChangedDataType)
                             .Invoke(null, new object[] { builder, connectionString, databaseName, tableName, optionsBuilder });

            return builder;
        }

        /// <summary>
        /// Configures a change feed for specified table
        /// </summary>
        /// <typeparam name="TRowChangedData">The <see cref="IMessage"/> representing the state of a changed row in the table being watched</typeparam>
        /// <param name="connectionString">The connection string for the sql server with the database and table to watch for changes</param>
        /// <param name="databaseName">Optional. The database containing the table to watch. If not specified, Database or InitialCatalog of the connectionString will be used.</param>
        /// <param name="tableName">The name of the table to watch</param>
        /// <param name="optionsBuilder">An optional builder allowing more complex change feed configuration</param>
        /// <returns><see cref="IChatterBuilder"/></returns>
        public static IChatterBuilder AddSqlChangeFeed<TRowChangedData>(this IChatterBuilder builder,
                                                                          string connectionString,
                                                                          string databaseName,
                                                                          string tableName,
                                                                          Action<SqlChangeFeedOptionsBuilder> optionsBuilder = null)
            where TRowChangedData : class, IMessage, new()
        {
            var changeFeedOptions = builder.Services.AddSqlChangeFeedOptionsBuilder(connectionString, tableName, databaseName);
            optionsBuilder?.Invoke(changeFeedOptions);
            SqlChangeFeedOptions options = changeFeedOptions.Build();

            builder.Services.AddIfNotRegistered<ISqlDependencyManager<TRowChangedData>>(ServiceLifetime.Scoped, sp =>
            {
                return new SqlDependencyManager<TRowChangedData>(options);
            });

            builder.AddSqlServiceBroker(ssbBuilder =>
            {
                var receiver = string.IsNullOrWhiteSpace(options.ChangeFeedQueueName) ? $"{ChatterServiceBrokerConstants.ChatterQueuePrefix}{typeof(TRowChangedData).Name}" : options.ChangeFeedQueueName;
                var dlq = string.IsNullOrWhiteSpace(options.ChangeFeedDeadLetterServiceName) ? $"{ChatterServiceBrokerConstants.ChatterDeadLetterServicePrefix}{typeof(TRowChangedData).Name}" : options.ChangeFeedDeadLetterServiceName;
                ssbBuilder.AddSqlServiceBrokerOptions(options.ServiceBrokerOptions)
                          .AddQueueReceiver<ProcessChangeFeedCommand<TRowChangedData>>(receiver,
                                                                                         errorQueuePath: options.ReceiverOptions.ErrorQueuePath,
                                                                                         transactionMode: options.ReceiverOptions.TransactionMode,
                                                                                         deadLetterServicePath: dlq);
            });

            if (options.ProcessChangeFeedCommandViaChatter)
            {
                builder.Services.Replace<IBrokeredMessageReceiver<ProcessChangeFeedCommand<TRowChangedData>>, ChangeFeedReceiver<TRowChangedData>>(ServiceLifetime.Scoped);
            }

            return builder;
        }

        /// <summary>
        /// Deploys the SQL and SQL Service Broker dependencies required for the sql change feed
        /// </summary>
        /// <typeparam name="TRowChangedData">The row type to use Sql migrations for</typeparam>
        /// <param name="applicationBuilder">The application builder</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns></returns>
        public static IApplicationBuilder UseChangeFeedSqlMigrations<TRowChangedData>(this IApplicationBuilder applicationBuilder, CancellationToken token = default)
            where TRowChangedData : class, IMessage, new()
        {
            applicationBuilder.ApplicationServices.UseChangeFeedSqlMigrations<TRowChangedData>(token);
            return applicationBuilder;
        }

        /// <summary>
        /// Deploys the SQL and SQL Service Broker dependencies required for the sql change feed
        /// </summary>
        /// <param name="applicationBuilder">The application builder</param>
        /// <param name="rowChangedDataType">The row type to use Sql migrations for</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        [RequiresUnreferencedCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrations<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        [RequiresDynamicCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrations<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        public static IApplicationBuilder UseChangeFeedSqlMigrations(this IApplicationBuilder applicationBuilder, Type rowChangedDataType, CancellationToken token = default)
        {
            applicationBuilder.ApplicationServices.UseChangeFeedSqlMigrations(rowChangedDataType, token);
            return applicationBuilder;
        }

        /// <summary>
        /// Deploys the SQL and SQL Service Broker dependencies required for table changes to be emitted
        /// </summary>
        /// <typeparam name="TRowChangedData">The row type to use Sql migrations for</typeparam>
        /// <param name="provider">The service provider</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns></returns>
        public static IServiceProvider UseChangeFeedSqlMigrations<TRowChangedData>(this IServiceProvider provider, CancellationToken token = default)
            where TRowChangedData : class, IMessage, new()
        {
            using var scope = provider.CreateScope();
            var sdm = scope.ServiceProvider.GetRequiredService<ISqlDependencyManager<TRowChangedData>>();
            RunSqlMigration(sdm, typeof(TRowChangedData).Name, token);
            return provider;
        }

        /// <summary>
        /// Deploys the SQL and SQL Service Broker dependencies required for table changes to be emitted
        /// </summary>
        /// <param name="provider">The service provider</param>
        /// <param name="rowChangedDataType">The row type to use Sql migrations for</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        [RequiresUnreferencedCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrations<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        [RequiresDynamicCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrations<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        public static IServiceProvider UseChangeFeedSqlMigrations(this IServiceProvider provider, Type rowChangedDataType, CancellationToken token = default)
        {
            using var scope = provider.CreateScope();
            var sdm = (ISqlDependencyManager)scope.ServiceProvider.GetRequiredService(typeof(ISqlDependencyManager<>).MakeGenericType(rowChangedDataType));
            RunSqlMigration(sdm, rowChangedDataType.Name, token);
            return provider;
        }

        private static void RunSqlMigration(ISqlDependencyManager sdm, string receiverName, CancellationToken token)
        {
            var conversationQueueName = $"{ChatterServiceBrokerConstants.ChatterQueuePrefix}{receiverName}";
            var conversationServiceName = $"{ChatterServiceBrokerConstants.ChatterServicePrefix}{receiverName}";
            var conversationDeadLetterQueueName = $"{ChatterServiceBrokerConstants.ChatterDeadLetterQueuePrefix}{receiverName}";
            var conversationDeadLetterServiceName = $"{ChatterServiceBrokerConstants.ChatterDeadLetterServicePrefix}{receiverName}";
            var conversationTriggerName = $"{ChatterServiceBrokerConstants.ChatterTriggerPrefix}{receiverName}";
            var installChangeFeedStoredProcName = $"{ChatterServiceBrokerConstants.ChatterInstallChangeFeedPrefix}{receiverName}";
            var uninstallChangeFeedStoredProcName = $"{ChatterServiceBrokerConstants.ChatterUninstallChangeFeedPrefix}{receiverName}";

            sdm.InstallSqlDependencies(installChangeFeedStoredProcName, uninstallChangeFeedStoredProcName, conversationQueueName, conversationServiceName, conversationTriggerName, conversationDeadLetterQueueName, conversationDeadLetterServiceName, token).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Asynchronously deploys the SQL and SQL Service Broker dependencies required for the sql change feed
        /// </summary>
        /// <typeparam name="TRowChangedData">The row type to use Sql migrations for</typeparam>
        /// <param name="applicationBuilder">The application builder</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns>A task that completes when the migration has finished</returns>
        public static Task UseChangeFeedSqlMigrationsAsync<TRowChangedData>(this IApplicationBuilder applicationBuilder, CancellationToken token = default)
            where TRowChangedData : class, IMessage, new()
            => applicationBuilder.ApplicationServices.UseChangeFeedSqlMigrationsAsync<TRowChangedData>(token);

        /// <summary>
        /// Asynchronously deploys the SQL and SQL Service Broker dependencies required for the sql change feed
        /// </summary>
        /// <param name="applicationBuilder">The application builder</param>
        /// <param name="rowChangedDataType">The row type to use Sql migrations for</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns>A task that completes when the migration has finished</returns>
        [RequiresUnreferencedCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrationsAsync<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        [RequiresDynamicCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrationsAsync<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        public static Task UseChangeFeedSqlMigrationsAsync(this IApplicationBuilder applicationBuilder, Type rowChangedDataType, CancellationToken token = default)
            => applicationBuilder.ApplicationServices.UseChangeFeedSqlMigrationsAsync(rowChangedDataType, token);

        /// <summary>
        /// Asynchronously deploys the SQL and SQL Service Broker dependencies required for table changes to be emitted
        /// </summary>
        /// <typeparam name="TRowChangedData">The row type to use Sql migrations for</typeparam>
        /// <param name="provider">The service provider</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns>A task that completes when the migration has finished</returns>
        public static async Task UseChangeFeedSqlMigrationsAsync<TRowChangedData>(this IServiceProvider provider, CancellationToken token = default)
            where TRowChangedData : class, IMessage, new()
        {
            using var scope = provider.CreateScope();
            var sdm = scope.ServiceProvider.GetRequiredService<ISqlDependencyManager<TRowChangedData>>();
            await RunSqlMigrationAsync(sdm, typeof(TRowChangedData).Name, token).ConfigureAwait(false);
        }

        /// <summary>
        /// Asynchronously deploys the SQL and SQL Service Broker dependencies required for table changes to be emitted
        /// </summary>
        /// <param name="provider">The service provider</param>
        /// <param name="rowChangedDataType">The row type to use Sql migrations for</param>
        /// <param name="token">A token to observe while waiting for the migration to complete</param>
        /// <returns>A task that completes when the migration has finished</returns>
        [RequiresUnreferencedCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrationsAsync<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        [RequiresDynamicCode("Resolves ISqlDependencyManager<TRowChangedData> via MakeGenericType. Use the UseChangeFeedSqlMigrationsAsync<TRowChangedData> overload for an AOT-safe, closed-generic alternative.")]
        public static async Task UseChangeFeedSqlMigrationsAsync(this IServiceProvider provider, Type rowChangedDataType, CancellationToken token = default)
        {
            using var scope = provider.CreateScope();
            var sdm = (ISqlDependencyManager)scope.ServiceProvider.GetRequiredService(typeof(ISqlDependencyManager<>).MakeGenericType(rowChangedDataType));
            await RunSqlMigrationAsync(sdm, rowChangedDataType.Name, token).ConfigureAwait(false);
        }

        private static Task RunSqlMigrationAsync(ISqlDependencyManager sdm, string receiverName, CancellationToken token)
        {
            var conversationQueueName = $"{ChatterServiceBrokerConstants.ChatterQueuePrefix}{receiverName}";
            var conversationServiceName = $"{ChatterServiceBrokerConstants.ChatterServicePrefix}{receiverName}";
            var conversationDeadLetterQueueName = $"{ChatterServiceBrokerConstants.ChatterDeadLetterQueuePrefix}{receiverName}";
            var conversationDeadLetterServiceName = $"{ChatterServiceBrokerConstants.ChatterDeadLetterServicePrefix}{receiverName}";
            var conversationTriggerName = $"{ChatterServiceBrokerConstants.ChatterTriggerPrefix}{receiverName}";
            var installChangeFeedStoredProcName = $"{ChatterServiceBrokerConstants.ChatterInstallChangeFeedPrefix}{receiverName}";
            var uninstallChangeFeedStoredProcName = $"{ChatterServiceBrokerConstants.ChatterUninstallChangeFeedPrefix}{receiverName}";

            return sdm.InstallSqlDependencies(installChangeFeedStoredProcName, uninstallChangeFeedStoredProcName, conversationQueueName, conversationServiceName, conversationTriggerName, conversationDeadLetterQueueName, conversationDeadLetterServiceName, token);
        }
    }
}
