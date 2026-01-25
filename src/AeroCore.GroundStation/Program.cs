using System;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AeroCore.GroundStation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // For Ground Station, we might want to listen to a Serial Port where the drone radio is connected
                    var useSerial = hostContext.Configuration.GetValue<bool>("UseSerial", false);

                    // We need to register ITelemetryProvider. 
                    // Since I haven't moved the providers to Shared yet, I will do it in the next step.
                    // For now, I'll assume they are available in Shared.Services.

                    if (useSerial)
                    {
                        services.AddSingleton<ITelemetryProvider, AeroCore.Shared.Services.SerialTelemetryProvider>();
                    }
                    else
                    {
                        services.AddSingleton<ITelemetryProvider, AeroCore.Shared.Services.MockTelemetryProvider>();
                    }

                    services.AddHostedService<GroundStationWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
