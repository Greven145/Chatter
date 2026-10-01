using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.CQRS.Pipeline;
using Chatter.MessageBrokers.Context;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public class RoutingSlipBehavior<TMessage> : ICommandBehavior<TMessage> where TMessage : ICommand
    {
        private readonly ILogger<RoutingSlipBehavior<TMessage>> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        // jsonOptions is DI-resolved when a consumer calls WithAotJsonSerialization, same optional-parameter
        // pattern as JsonBodyConverter (#509/#18) -- the real, documented production path for this behavior
        // under Native AOT, so no caller needs to pass it explicitly.
        public RoutingSlipBehavior(ILogger<RoutingSlipBehavior<TMessage>> logger, JsonSerializerOptions jsonOptions = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = jsonOptions;
        }

        public async Task Handle(TMessage message, IMessageHandlerContext messageHandlerContext, CommandHandlerDelegate next)
        {
            _logger.LogDebug($"Entering {nameof(RoutingSlipBehavior<TMessage>)}.");
            if (!(messageHandlerContext is IMessageBrokerContext messageBrokerContext))
            {
                _logger.LogTrace($"No brokered message context found. Continuing pipeline execution.");
                await next().ConfigureAwait(false);
                return;
            }

            if (!(messageBrokerContext.TryGetRoutingSlip(out var theSlip, _jsonOptions)))
            {
                _logger.LogTrace($"No routing slip found. Continuing pipeline execution.");
                await next().ConfigureAwait(false);
                return;
            }

            messageBrokerContext.Container.Include(theSlip);

            if (theSlip.Route?.FirstOrDefault() == null)
            {
                _logger.LogTrace($"No routes left in routing slip. Continuing pipeline execution.");
                await next().ConfigureAwait(false);
                return;
            }

            _logger.LogDebug("Continuing pipeline execution.");
            await next().ConfigureAwait(false);

            try
            {
                _logger.LogTrace($"Sending message to '{theSlip.Route?.FirstOrDefault()?.DestinationPath}'");
                await messageHandlerContext.Send(message, theSlip, jsonOptions: _jsonOptions).ConfigureAwait(false);
                _logger.LogDebug("Sent message to next routing slip destination");

            }
            catch (Exception e)
            {
                _logger.LogTrace(e, $"Error routing message '{typeof(TMessage).Name}' to next routing slip destination ({theSlip.Route?.FirstOrDefault().DestinationPath})");
                throw;
            }
            finally
            {
                _logger.LogDebug($"Finishing {nameof(RoutingSlipBehavior<TMessage>)}.");
            }
        }
    }
}
