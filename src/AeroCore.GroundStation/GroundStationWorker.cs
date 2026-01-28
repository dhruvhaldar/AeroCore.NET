using System;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
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

        private void ShowWelcomeBanner()
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("============================================================");
            Console.WriteLine("              AEROCORE GROUND STATION v1.0");
            Console.WriteLine("============================================================");
            Console.ResetColor();
            Console.WriteLine($"  > System Init:      {DateTime.Now:HH:mm:ss}");
            Console.WriteLine("  > System Status:    Ready");
            Console.WriteLine("  > Telemetry Link:   Listening...");
            Console.WriteLine();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Give the host a moment to finish startup logs so the banner appears after them
            await Task.Delay(100, stoppingToken);

            ShowWelcomeBanner();
            _logger.LogInformation("Ground Station Listening for Telemetry...");

            try
            {
                await foreach (var packet in _telemetryProvider.StreamTelemetryAsync(stoppingToken))
                {
                    // Visualize the data
                    PrintTelemetry(packet);
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

        private string GetAnalogGauge(double value, double range, int width = 11)
        {
            var buffer = new char[width];
            int center = width / 2;
            double normalized = Math.Clamp(value / range, -1, 1);
            int fill = (int)Math.Round(Math.Abs(normalized) * center);

            for (int i = 0; i < width; i++) buffer[i] = ' ';
            buffer[center] = '|';

            if (fill > 0)
            {
                if (normalized > 0)
                {
                    for (int i = 1; i <= fill; i++) buffer[center + i] = '=';
                    buffer[center + fill] = '>';
                }
                else
                {
                    for (int i = 1; i <= fill; i++) buffer[center - i] = '=';
                    buffer[center - fill] = '<';
                }
            }
            return new string(buffer);
        }

        private void PrintTelemetry(TelemetryPacket packet)
        {
            // Timestamp
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write($"[GCS] T+{packet.Timestamp:HH:mm:ss.fff} | ");

            // Altitude
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ALT: ");
            Console.ForegroundColor = packet.Altitude < 0 ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write($"{packet.Altitude,8:F2}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" ft");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Velocity
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("VEL: ");
            Console.ForegroundColor = packet.Velocity > 100 ? ConsoleColor.Yellow : ConsoleColor.White;
            Console.Write($"{packet.Velocity,6:F1}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" kts");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Pitch
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("PIT: ");
            Console.ForegroundColor = Math.Abs(packet.Pitch) > 45 ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write($"{packet.Pitch,7:F2}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Pitch Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            Console.ForegroundColor = Math.Abs(packet.Pitch) > 45 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.Write(GetAnalogGauge(packet.Pitch, 45.0));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] | ");

            // Roll
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ROL: ");
            Console.ForegroundColor = Math.Abs(packet.Roll) > 45 ? ConsoleColor.Red : ConsoleColor.White;
            Console.Write($"{packet.Roll,7:F2}");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Roll Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            Console.ForegroundColor = Math.Abs(packet.Roll) > 45 ? ConsoleColor.Red : ConsoleColor.Green;
            Console.Write(GetAnalogGauge(packet.Roll, 45.0));
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("]");

            Console.ResetColor();
        }

    }
}
