using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using Chatter.MessageBrokers.Reliability.Outbox;

namespace Chatter.MessageBrokers.Reliability.Configuration
{
    public class ReliabilityOptionsBuilder
    {
        private bool _routeMessagesToOutbox = false;
        private double _minutesToLiveInMemory = 10;
        private bool _enableOutboxPollingProcessor = false;
        private int _outboxProcessingIntervalInMilliseconds = 5000;

        public const string ReliabilityOptionsSectionName = "Chatter:MessageBrokers:Reliability";
        private readonly IServiceCollection _services;
        private readonly IConfiguration _configuration;

        public static ReliabilityOptionsBuilder Create(IServiceCollection services)
            => new ReliabilityOptionsBuilder(services);

        private ReliabilityOptionsBuilder(IServiceCollection services) : this(services, null) { }
        private ReliabilityOptionsBuilder(IServiceCollection services, IConfiguration configuration)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _configuration = configuration;
        }

        [RequiresUnreferencedCode("Binds ReliabilityOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze. Use the fluent, non-config API (Create(services).WithOutboxRouting()...Build()) for an AOT-safe alternative.")]
        [RequiresDynamicCode("Binds ReliabilityOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze. Use the fluent, non-config API (Create(services).WithOutboxRouting()...Build()) for an AOT-safe alternative.")]
        public static ReliabilityOptions FromConfig(IServiceCollection services, IConfiguration configuration, string reliabilityOptionsSectionName = ReliabilityOptionsSectionName)
        {
            var section = configuration?.GetSection(reliabilityOptionsSectionName);
            if (section != null && section.Exists())
            {
                var reliabilityOptions = section.Get<ReliabilityOptions>();
                services.Configure<ReliabilityOptions>(section);
                services.AddSingleton(reliabilityOptions);
                return reliabilityOptions;
            }
            return new ReliabilityOptionsBuilder(services, configuration).Build();
        }

        /// <summary>
        /// Enables routing of messages to an outbox, rather than directly to messaging infrastructure. Using <see cref="OutboxProcessingBehavior{TMessage}"/>
        /// automatically enables outbox routing.
        /// </summary>
        /// <returns><see cref="ReliabilityOptionsBuilder"/></returns>
        public ReliabilityOptionsBuilder WithOutboxRouting()
        {
            _routeMessagesToOutbox = true;
            return this;
        }

        /// <summary>
        /// Defines how long outbox messages will live within the <see cref="InMemoryBrokeredMessageOutbox"/>. The <see cref="InMemoryBrokeredMessageOutbox"/>
        /// is registered by Chatter by default if no other persistance strategy is used. Default value is 10.
        /// </summary>
        /// <param name="timeToLiveInMinutes">The time messages will be maintained within the <see cref="InMemoryBrokeredMessageOutbox"/> before being purged.</param>
        /// <returns><see cref="ReliabilityOptionsBuilder"/></returns>
        public ReliabilityOptionsBuilder WithInMemoryOutboxTimeToLive(double timeToLiveInMinutes)
        {
            _minutesToLiveInMemory = timeToLiveInMinutes;
            return this;
        }

        /// <summary>
        /// Enables the <see cref="BrokeredMessageOutboxProcessor"/> which processes messages from the outbox and sends them to messaging infrastructure 
        /// at a timed interval. The default polling interval is 5000 milliseconds. This does not enable sending of messages to the outbox by default which
        /// must be done by calling <see cref="WithOutboxRouting"/> or by using <see cref="OutboxProcessingBehavior{TMessage}"/>.
        /// </summary>
        /// <param name="outboxProcessingIntervalInMilliseconds">The interval to wait before the outbox is checked for brokered messages</param>
        /// <returns><see cref="ReliabilityOptionsBuilder"/></returns>
        public ReliabilityOptionsBuilder WithOutboxPollingProcessor(int outboxProcessingIntervalInMilliseconds = 5000)
        {
            _enableOutboxPollingProcessor = true;
            _outboxProcessingIntervalInMilliseconds = outboxProcessingIntervalInMilliseconds;
            return this;
        }

        public ReliabilityOptions Build()
        {
            var reliabilityOptions = new ReliabilityOptions
            {
                RouteMessagesToOutbox = _routeMessagesToOutbox,
                MinutesToLiveInMemory = _minutesToLiveInMemory,
                EnableOutboxPollingProcessor = _enableOutboxPollingProcessor,
                OutboxProcessingIntervalInMilliseconds = _outboxProcessingIntervalInMilliseconds
            };

            _services.AddSingleton(reliabilityOptions);

            return reliabilityOptions;
        }
    }
}
