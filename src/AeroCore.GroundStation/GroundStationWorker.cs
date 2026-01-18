using System;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AeroCore.GroundStation
{
    public class GroundStationWorker : BackgroundService
    {
        private readonly ITelemetryProvider _telemetryProvider;
        private readonly ILogger<GroundStationWorker> _logger;

        public GroundStationWorker(ITelemetryProvider telemetryProvider, ILogger<GroundStationWorker> logger)
        {
            _telemetryProvider = telemetryProvider;
            _logger = logger;
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Ground Station Starting...");
            await _telemetryProvider.InitializeAsync(cancellationToken);
            await base.StartAsync(cancellationToken);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Ground Station Listening for Telemetry...");

            try
            {
                await foreach (var packet in _telemetryProvider.StreamTelemetryAsync(stoppingToken))
                {
                    // Visualize the data
                    // In a real app this might update a UI or push to a database
                    Console.WriteLine($"[GCS] T+{packet.Timestamp:HH:mm:ss.fff} | ALT: {packet.Altitude,8:F2} | VEL: {packet.Velocity,6:F1} | PIT: {packet.Pitch,5:F2} | ROL: {packet.Roll,5:F2}");
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Ground Station Stopped.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ground Station Error");
            }
        }
    }
}
