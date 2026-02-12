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

            Console.Write("  > System Status:    ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Ready");
            Console.ResetColor();

            Console.Write("  > Telemetry Link:   ");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Listening...");
            Console.ResetColor();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  (Press Ctrl+C to exit)");
            Console.ResetColor();

            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("  [LEGEND]");

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ALT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": Altitude (ft)   ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("VEL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(": Velocity (kts)");

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("PIT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": Pitch (deg)     ");

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ROL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(": Roll (deg)");

            Console.WriteLine();

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("<====");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("====>");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" : Visual Attitude Indicator");

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("^");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" / ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("v");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("       : Rising / Falling Trend");

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Green");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("Yel");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("Red");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" : Stable / Warning / Critical");

            Console.ResetColor();
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
            double absPitch = Math.Abs(packet.Pitch);
            if (absPitch > 45) Console.ForegroundColor = ConsoleColor.Red;
            else if (absPitch > 35) Console.ForegroundColor = ConsoleColor.Yellow;
            else Console.ForegroundColor = ConsoleColor.White;
            WriteFormatted(packet.Pitch, 5, "F2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Pitch Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            PrintGauge(packet.Pitch, 45.0);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] | ");

            // Roll
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ROL: ");
            double absRoll = Math.Abs(packet.Roll);
            if (absRoll > 45) Console.ForegroundColor = ConsoleColor.Red;
            else if (absRoll > 35) Console.ForegroundColor = ConsoleColor.Yellow;
            else Console.ForegroundColor = ConsoleColor.White;
            WriteFormatted(packet.Roll, 5, "F2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Roll Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            PrintGauge(packet.Roll, 45.0);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("]");

            Console.ResetColor();
        }

        private void PrintGauge(double value, double range)
        {
            var absValue = Math.Abs(value);
            ConsoleColor barColor;
            if (absValue > range) barColor = ConsoleColor.Red;
            else if (absValue > range * 0.8) barColor = ConsoleColor.Yellow;
            else barColor = ConsoleColor.Green;

            Span<char> buffer = stackalloc char[11];
            GaugeVisualizer.Fill(buffer, value, range);

            int center = buffer.Length / 2;

            // Print Left
            if (center > 0)
            {
                Console.ForegroundColor = barColor;
                Console.Out.Write(buffer.Slice(0, center));
            }

            // Print Center (Anchor)
            Console.ForegroundColor = ConsoleColor.White;
            Console.Out.Write(buffer.Slice(center, 1));

            // Print Right
            if (center + 1 < buffer.Length)
            {
                Console.ForegroundColor = barColor;
                Console.Out.Write(buffer.Slice(center + 1));
            }
        }

        private void WriteFormatted(double value, int width, ReadOnlySpan<char> format)
        {
            // Allocate a buffer large enough for typical numbers + padding.
            // 32 chars is usually enough for double string representation.
            Span<char> valueBuffer = stackalloc char[32];

            // Use default provider (CurrentCulture) to match Console.Write behavior
            if (value.TryFormat(valueBuffer, out int charsWritten, format, provider: null))
            {
                int padding = width - charsWritten;

                if (padding > 0)
                {
                    // Optimization: Combine padding and value into a single buffer to reduce Console syscalls by 50%.
                    // Allocation on stack is cheap; Console I/O is expensive.
                    Span<char> combinedBuffer = stackalloc char[64];
                    if (padding + charsWritten <= combinedBuffer.Length)
                    {
                        combinedBuffer.Slice(0, padding).Fill(' ');
                        valueBuffer.Slice(0, charsWritten).CopyTo(combinedBuffer.Slice(padding));
                        Console.Out.Write(combinedBuffer.Slice(0, padding + charsWritten));
                        return;
                    }

                    // Fallback to split writes if buffer is too small (unlikely)
                    Span<char> spaces = stackalloc char[padding];
                    spaces.Fill(' ');
                    Console.Out.Write(spaces);
                }
                Console.Out.Write(valueBuffer.Slice(0, charsWritten));
            }
            else
            {
                // Fallback
                Console.Write(value.ToString(format.ToString()));
            }
        }
    }
}
