using Chatter.MessageBrokers.Context;
using System;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public static class MessageBrokerContextExtensions
    {
        public static bool TryGetRoutingSlip(this IMessageBrokerContext mbc, out RoutingSlip routingSlip, JsonSerializerOptions jsonOptions = null)
        {
            try
            {
                if (mbc.BrokeredMessage != null)
                {
                    if (mbc.BrokeredMessage.MessageContext != null)
                    {
                        if (mbc.BrokeredMessage.MessageContext.TryGetValue(MessageContext.RoutingSlip, out var rs) && rs is not null)
                        {
                            // Attachments (IDictionary<string, object>) values are materialized to the CLR
                            // types Newtonsoft's untyped read produced during this deserialize by the global
                            // MaterializingObjectConverter, present on both ChatterJson.Options and any
                            // AOT options built via ChatterJson.CreateAotOptions, so consumers that set
                            // slip.Attachments["foo"] = "bar" and read it back as string/int after
                            // TryGetRoutingSlip don't hit cast failures — no per-seam materialization needed.
                            var effectiveOptions = jsonOptions ?? ChatterJson.ReflectionDefaultOrThrow();
                            RoutingSlip theSlip = JsonSerializer.Deserialize((string)rs, (JsonTypeInfo<RoutingSlip>)effectiveOptions.GetTypeInfo(typeof(RoutingSlip)));
                            routingSlip = theSlip;
                            return true;
                        }
                    }
                }

                if (mbc.Container.TryGet<RoutingSlip>(out var slipFromContainer))
                {
                    routingSlip = slipFromContainer;
                    return true;
                }

                routingSlip = null;
                return false;
            }
            // Deliberately narrow: a malformed stored value (InvalidCastException from the (string) cast)
            // or malformed JSON (JsonException) means "no usable slip", not an error. A type missing from
            // the AOT options' JsonSerializerContext (NotSupportedException) is a real configuration defect
            // and must propagate, not read back silently as "no slip".
            catch (InvalidCastException)
            {
                routingSlip = null;
                return false;
            }
            catch (JsonException)
            {
                routingSlip = null;
                return false;
            }
        }
    }
}
