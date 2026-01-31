using System;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Helpers;
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
        private double? _lastAltitude;
        private double? _lastVelocity;

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
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [LEGEND]");
            Console.WriteLine("   ALT: Altitude (ft)        | VEL: Velocity (kts)");
            Console.WriteLine("   PIT/ROL: Pitch/Roll (deg) | Gauge: +/- 45 deg");
            Console.WriteLine("   Trends: ^ Climb/Accel     | v Descend/Decel");
            Console.WriteLine("   Alerts: RED = Critical Value");
            Console.ResetColor();
            Console.WriteLine("============================================================");
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

        private void PrintTelemetry(TelemetryPacket packet)
        {
            // Timestamp
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[GCS] T+");

            Span<char> tsBuffer = stackalloc char[20];
            if (packet.Timestamp.TryFormat(tsBuffer, out int tsWritten, "HH:mm:ss.fff"))
            {
                Console.Out.Write(tsBuffer.Slice(0, tsWritten));
            }
            else
            {
                Console.Write(packet.Timestamp.ToString("HH:mm:ss.fff"));
            }

            Console.Write(" | ");

            // Altitude
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ALT: ");
            Console.ForegroundColor = packet.Altitude < 0 ? ConsoleColor.Red : ConsoleColor.White;
            WriteFormatted(packet.Altitude, 8, "F2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" ft ");

            if (_lastAltitude.HasValue)
            {
                double delta = packet.Altitude - _lastAltitude.Value;
                if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("v");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("-");
                }
            }
            else
            {
                Console.Write(" ");
            }
            _lastAltitude = packet.Altitude;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Velocity
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("VEL: ");
            Console.ForegroundColor = packet.Velocity > 100 ? ConsoleColor.Yellow : ConsoleColor.White;
            WriteFormatted(packet.Velocity, 6, "F1");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" kts ");

            if (_lastVelocity.HasValue)
            {
                double delta = packet.Velocity - _lastVelocity.Value;
                if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("v");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("-");
                }
            }
            else
            {
                Console.Write(" ");
            }
            _lastVelocity = packet.Velocity;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Pitch
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("PIT: ");
            Console.ForegroundColor = Math.Abs(packet.Pitch) > 45 ? ConsoleColor.Red : ConsoleColor.White;
            WriteFormatted(packet.Pitch, 5, "F2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Pitch Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            Console.ForegroundColor = ConsoleColor.Green;

            Span<char> gaugeBuffer = stackalloc char[11];
            GaugeVisualizer.Fill(gaugeBuffer, packet.Pitch, 45.0);
            Console.Out.Write(gaugeBuffer);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] | ");

            // Roll
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ROL: ");
            Console.ForegroundColor = Math.Abs(packet.Roll) > 45 ? ConsoleColor.Red : ConsoleColor.White;
            WriteFormatted(packet.Roll, 5, "F2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Roll Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            Console.ForegroundColor = ConsoleColor.Green;

            GaugeVisualizer.Fill(gaugeBuffer, packet.Roll, 45.0);
            Console.Out.Write(gaugeBuffer);

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("]");

            Console.ResetColor();
        }

        private void WriteFormatted(double value, int width, ReadOnlySpan<char> format)
        {
            Span<char> buffer = stackalloc char[32];
            // Use default provider (CurrentCulture) to match Console.Write behavior
            if (value.TryFormat(buffer, out int charsWritten, format, provider: null))
            {
                int padding = width - charsWritten;
                if (padding > 0)
                {
                    Span<char> spaces = stackalloc char[padding];
                    spaces.Fill(' ');
                    Console.Out.Write(spaces);
                }
                Console.Out.Write(buffer.Slice(0, charsWritten));
            }
            else
            {
                // Fallback
                Console.Write(value.ToString(format.ToString()));
            }
        }
    }
}
