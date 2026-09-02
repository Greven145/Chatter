using Chatter.MessageBrokers.Receiving;
using Chatter.MessageBrokers.Recovery.Options;
using Chatter.MessageBrokers.Reliability.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Chatter.MessageBrokers.Configuration
{
    public class MessageBrokerOptionsBuilder
    {
        public IServiceCollection Services { get; }
        private readonly IConfiguration _configuration;
        private TransactionMode _transactionMode = TransactionMode.ReceiveOnly;
        private ReliabilityOptions _reliabilityOptions = null;
        private RecoveryOptions _recoveryOptions = null;

        public const string MessageBrokerSectionName = "Chatter:MessageBrokers";

        public static MessageBrokerOptionsBuilder Create(IServiceCollection services)
            => new MessageBrokerOptionsBuilder(services);

        private MessageBrokerOptionsBuilder(IServiceCollection services) : this(services, null) { }
        internal MessageBrokerOptionsBuilder(IServiceCollection services, IConfiguration configuration)
        {
            Services = services;
            _configuration = configuration;
        }

        public MessageBrokerOptionsBuilder WithTransactionMode(TransactionMode transactionMode)
        {
            _transactionMode = transactionMode;
            return this;
        }

        [RequiresUnreferencedCode("Binds MessageBrokerOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        [RequiresDynamicCode("Binds MessageBrokerOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        public MessageBrokerOptions FromConfig(string messageBrokerSectionName = MessageBrokerSectionName)
            => FromConfig(Services, _configuration, messageBrokerSectionName);

        [RequiresUnreferencedCode("Binds MessageBrokerOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        [RequiresDynamicCode("Binds MessageBrokerOptions from an IConfigurationSection via ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        public static MessageBrokerOptions FromConfig(IServiceCollection services, IConfiguration configuration, string messageBrokerSectionName = MessageBrokerSectionName)
        {
            var section = configuration?.GetSection(messageBrokerSectionName);
            if (section != null && section.Exists())
            {
                return Finish(services, BindFromSection(section));
            }
            return new MessageBrokerOptionsBuilder(services, configuration).Build();
        }

        [RequiresUnreferencedCode("Uses ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        [RequiresDynamicCode("Uses ConfigurationBinder.Get<T>, which trimming cannot statically analyze.")]
        private static MessageBrokerOptions BindFromSection(IConfigurationSection section)
            => section.Get<MessageBrokerOptions>();

        public MessageBrokerOptionsBuilder AddReliabilityOptions(Action<ReliabilityOptionsBuilder> builder)
        {
            var b = ReliabilityOptionsBuilder.Create(Services);
            builder?.Invoke(b);
            _reliabilityOptions = b.Build();
            return this;
        }

        public MessageBrokerOptionsBuilder AddRecoveryOptions(Action<RecoveryOptionsBuilder> builder)
        {
            var b = RecoveryOptionsBuilder.Create(Services);
            builder?.Invoke(b);
            _recoveryOptions = b.Build();
            return this;
        }

        internal MessageBrokerOptions Build()
        {
            var messageBrokerOptions = new MessageBrokerOptions
            {
                Reliability = _reliabilityOptions,
                Recovery = _recoveryOptions,
                TransactionMode = _transactionMode
            };

            return Finish(Services, messageBrokerOptions);
        }

        private static MessageBrokerOptions Finish(IServiceCollection services, MessageBrokerOptions messageBrokerOptions)
        {
            if (messageBrokerOptions.Reliability is null)
            {
                messageBrokerOptions.Reliability = ReliabilityOptionsBuilder.Create(services).Build();
            }

            if (messageBrokerOptions.Recovery is null)
            {
                messageBrokerOptions.Recovery = RecoveryOptionsBuilder.Create(services).Build();
            }

            services.AddSingleton(messageBrokerOptions);

            return messageBrokerOptions;
        }
    }
}
