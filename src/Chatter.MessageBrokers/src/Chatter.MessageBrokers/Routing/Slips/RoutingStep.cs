using System.Text.Json.Serialization;

namespace Chatter.MessageBrokers.Routing.Slips
{
    public class RoutingStep
    {
        [JsonConstructor]
        internal RoutingStep() { }

        internal RoutingStep(string destinationPath) 
            => DestinationPath = destinationPath;

        public string DestinationPath { get; set; }
    }
}
