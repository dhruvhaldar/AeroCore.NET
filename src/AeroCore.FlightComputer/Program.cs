using System;
using System.Threading.Tasks;
using AeroCore.Shared.Services;
using AeroCore.FlightComputer.Services;
using AeroCore.Shared.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AeroCore.FlightComputer
{
    class Program
    {
        static async Task Main(string[] args)
        {
            using IHost host = Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    // Configuration to switch between Mock and Serial
                    // Default to Mock if not specified
                    var useSerial = hostContext.Configuration.GetValue<bool>("UseSerial", false);

                    if (useSerial)
                    {
                        services.AddSingleton<ITelemetryProvider, SerialTelemetryProvider>();
                    }
                    else
                    {
                        services.AddSingleton<ITelemetryProvider, MockTelemetryProvider>();
                    }

                    services.AddSingleton<IFlightComputer, FlightControlUnit>();
                    
                    // Register the worker as a Hosted Service
                    services.AddHostedService<AerospaceWorker>();
                })
                .Build();

            await host.RunAsync();
        }
    }
}
