using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Chatter.MessageBrokers
{
    /// <remarks>
    /// Registered as an <see cref="IHostedService"/> alongside a broker module's Native AOT body-converter
    /// branch so a missing <c>WithAotJsonSerialization</c> opt-in fails when the host constructs its hosted
    /// services (composition/startup time), not on the first message the converter actually touches.
    /// </remarks>
    public sealed class AotJsonOptionsStartupGuard : IHostedService
    {
        public AotJsonOptionsStartupGuard(IServiceProvider serviceProvider)
        {
            if (serviceProvider.GetService<JsonSerializerOptions>() is null)
            {
                ChatterJson.ReflectionDefaultOrThrow();
            }
        }

        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
