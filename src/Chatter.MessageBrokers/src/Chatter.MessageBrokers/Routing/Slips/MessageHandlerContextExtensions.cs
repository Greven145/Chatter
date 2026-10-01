using Chatter.CQRS.Commands;
using Chatter.CQRS.Context;
using Chatter.MessageBrokers.Context;
using Chatter.MessageBrokers.Routing.Options;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class MessageHandlerContextExtensions
    {
        public static void AddRoutingSlip(this IMessageHandlerContext mhc, RoutingSlip routingSlip)
        {
            mhc.Container.Include(routingSlip);
        }

        public static bool TryGetRoutingSlip(this IMessageHandlerContext mhc, out RoutingSlip routingSlip, JsonSerializerOptions jsonOptions = null)
        {
            if (mhc is IMessageBrokerContext mbc)
            {
                if (mbc.TryGetRoutingSlip(out var rs, jsonOptions))
                {
                    routingSlip = rs;
                    return true;
                }
            }
            routingSlip = null;
            return false;
        }

        public static Task Send<TMessage>(this IMessageHandlerContext context,
                            TMessage message,
                            RoutingSlip slip,
                            SendOptions options = null,
                            JsonSerializerOptions jsonOptions = null) where TMessage : ICommand
        {
            if (context.TryGetBrokeredMessageDispatcher(out var bmd))
            {
                return bmd.Send(message, slip, context.GetTransactionContext(), options, jsonOptions);
            }
            return Task.CompletedTask;
        }

        public static Task Forward(this IMessageHandlerContext context, RoutingSlip slip, JsonSerializerOptions jsonOptions = null)
        {
            if (context.TryGetBrokeredMessageDispatcher(out var brokeredMessageDispatcher))
            {
                var destination = slip.Route.FirstOrDefault()?.DestinationPath;
                if (!string.IsNullOrWhiteSpace(destination))
                {
                    return brokeredMessageDispatcher.Forward(context.GetInboundBrokeredMessage(), slip, context.GetTransactionContext(), jsonOptions);
                }
            }

            return Task.CompletedTask;
        }
    }
}
