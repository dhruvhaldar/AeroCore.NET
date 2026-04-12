using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AeroCore.FlightComputer
{
    // Background service wrapper to bridge .NET Generic Host with our FCU
    public class AerospaceWorker : BackgroundService
    {
        private readonly IFlightComputer _fcu;
        private readonly ILogger<AerospaceWorker> _logger;

        public AerospaceWorker(IFlightComputer fcu, ILogger<AerospaceWorker> logger)
        {
            _fcu = fcu;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("System Boot Sequence Initiated.");
            await _fcu.InitializeAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Yield to allow host startup logs to complete before drawing the banner
            await Task.Delay(100, stoppingToken);

            System.Console.Title = "AeroCore Flight Computer v1.0";
            System.Console.WriteLine();
            System.Console.ForegroundColor = System.ConsoleColor.Cyan;
            System.Console.WriteLine("════════════════════════════════════════════════════════════");
            System.Console.WriteLine("              AEROCORE FLIGHT COMPUTER v1.0");
            System.Console.WriteLine("════════════════════════════════════════════════════════════");
            System.Console.ResetColor();
            System.Console.ForegroundColor = System.ConsoleColor.DarkGray;
            System.Console.Write("  (Press ");
            System.Console.ForegroundColor = System.ConsoleColor.White;
            System.Console.Write("Ctrl+C");
            System.Console.ForegroundColor = System.ConsoleColor.DarkGray;
            System.Console.WriteLine(" to exit)");
            System.Console.ResetColor();
            System.Console.WriteLine();

            // Fire and forget the control loop logic inside the hosted service context
            await _fcu.ProcessLoopAsync(stoppingToken);
        }
    }
}
