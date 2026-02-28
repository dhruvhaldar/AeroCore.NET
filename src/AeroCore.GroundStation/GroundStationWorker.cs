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
        private int _spinnerIndex = 0;
        private static readonly char[] _spinnerChars = { '|', '/', '-', '\\' };

        // Rate limiter state for UI updates (to prevent console I/O blocking stream processing)
        private long _lastUiUpdate = 0;
        private string _lastTitleStatus = string.Empty;

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
            Console.Title = "AeroCore Ground Station v1.0";
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
            var providerName = _telemetryProvider.GetType().Name.Replace("TelemetryProvider", "");
            Console.WriteLine($"Listening ({providerName})...");
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
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("<==+=");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("==+=>");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] / [");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("   + ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" +   ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]");
            Console.WriteLine(" : Visual Attitude Indicator");

            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("^^");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("^");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("-");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("v");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("/");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("vv");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" : Fast / Slow / Stable Trend");

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
            // Hide cursor to prevent distraction
            try { Console.CursorVisible = false; } catch { }

            try
            {
                // Give the host a moment to finish startup logs so the banner appears after them
                await Task.Delay(100, stoppingToken);

                ShowWelcomeBanner();
                _logger.LogInformation("Ground Station Listening for Telemetry...");

                try
                {
                    await foreach (var packet in _telemetryProvider.StreamTelemetryAsync(stoppingToken))
                    {
                        // Optimization: Throttle UI updates to ~20 FPS (50ms) to prevent Console I/O from blocking the telemetry stream.
                        // This ensures we process incoming packets as fast as possible to avoid serial buffer overflow,
                        // while still providing a smooth visual update for the user.
                        // Use Environment.TickCount64 instead of DateTime.UtcNow to accurately measure wall-clock time
                        // while avoiding expensive system calls in the per-packet hot loop.
                        var now = Environment.TickCount64;
                        if ((now - _lastUiUpdate) >= 50)
                        {
                            // Visualize the data
                            PrintTelemetry(packet);
                            _lastUiUpdate = now;
                        }
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
            finally
            {
                // Restore cursor on exit
                try { Console.CursorVisible = true; } catch { }
            }
        }

        private void PrintTelemetry(TelemetryPacket packet)
        {
            // Spinner
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(_spinnerChars[_spinnerIndex]);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");
            _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;
            Console.ResetColor();

            // Timestamp
            Console.ForegroundColor = ConsoleColor.DarkGray;

            // Optimization: Combine prefix, timestamp, and suffix into a single buffer to reduce Console syscalls by 66%.
            Span<char> lineBuffer = stackalloc char[64];
            int pos = 0;

            // "[GCS] @ " to denote Wall Clock Time
            "[GCS] @ ".AsSpan().CopyTo(lineBuffer);
            pos += 8;

            // Timestamp
            if (packet.Timestamp.TryFormat(lineBuffer.Slice(pos), out int tsWritten, "HH:mm:ss.fff"))
            {
                pos += tsWritten;
            }
            else
            {
                // Fallback (unlikely)
                var tsStr = packet.Timestamp.ToString("HH:mm:ss.fff");
                tsStr.AsSpan().CopyTo(lineBuffer.Slice(pos));
                pos += tsStr.Length;
            }

            // " | "
            " | ".AsSpan().CopyTo(lineBuffer.Slice(pos));
            pos += 3;

            Console.Out.Write(lineBuffer.Slice(0, pos));

            // Altitude
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ALT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            Console.ForegroundColor = packet.Altitude < 0 ? ConsoleColor.Red : ConsoleColor.White;
            WriteFormatted(packet.Altitude, 10, "N2");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" ft ");

            if (_lastAltitude.HasValue)
            {
                double delta = packet.Altitude - _lastAltitude.Value;
                if (delta > 1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^^");
                }
                else if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^ ");
                }
                else if (delta < -1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("vv");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("v ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("- ");
                }
            }
            else
            {
                Console.Write("  ");
            }
            _lastAltitude = packet.Altitude;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Velocity
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("VEL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            Console.ForegroundColor = packet.Velocity > 100 ? ConsoleColor.Yellow : ConsoleColor.White;
            WriteFormatted(packet.Velocity, 7, "N1");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" kts ");

            if (_lastVelocity.HasValue)
            {
                double delta = packet.Velocity - _lastVelocity.Value;
                if (delta > 1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^^");
                }
                else if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("^ ");
                }
                else if (delta < -1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("vv");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("v ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("- ");
                }
            }
            else
            {
                Console.Write("  ");
            }
            _lastVelocity = packet.Velocity;

            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Pitch
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("PIT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
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
            PrintGauge(packet.Pitch, 45.0, 35.0);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] | ");

            // Roll
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("ROL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
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
            PrintGauge(packet.Roll, 45.0, 35.0);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]");

            // Status Indicator
            Console.Write(" | ");

            bool critAlt = packet.Altitude < 0;
            bool critPit = Math.Abs(packet.Pitch) > 45;
            bool critRol = Math.Abs(packet.Roll) > 45;
            bool isCrit = critAlt || critPit || critRol;

            bool warnVel = packet.Velocity > 100;
            bool warnPit = !critPit && Math.Abs(packet.Pitch) > 35;
            bool warnRol = !critRol && Math.Abs(packet.Roll) > 35;
            bool isWarn = warnVel || warnPit || warnRol;

            // Build Status String for Title and Console
            string statusStr = "OK";
            string reasonsStr = "";
            ConsoleColor statusColor = ConsoleColor.Green;

            if (isCrit)
            {
                statusStr = "CRIT";
                statusColor = ConsoleColor.Red;

                Span<char> reasons = stackalloc char[15]; // Max "ALT,PIT,ROL" is 11 chars
                int reasonsPos = 0;
                bool first = true;
                if (critAlt) { "ALT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (critPit) { if (!first) { reasons[reasonsPos++] = ','; } "PIT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (critRol) { if (!first) { reasons[reasonsPos++] = ','; } "ROL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; }
                reasonsStr = reasons.Slice(0, reasonsPos).ToString();
            }
            else if (isWarn)
            {
                statusStr = "WARN";
                statusColor = ConsoleColor.Yellow;

                Span<char> reasons = stackalloc char[15];
                int reasonsPos = 0;
                bool first = true;
                if (warnVel) { "VEL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (warnPit) { if (!first) { reasons[reasonsPos++] = ','; } "PIT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (warnRol) { if (!first) { reasons[reasonsPos++] = ','; } "ROL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; }
                reasonsStr = reasons.Slice(0, reasonsPos).ToString();
            }

            // Update Console Title (only if changed)
            string fullStatus = string.IsNullOrEmpty(reasonsStr) ? $"[{statusStr}]" : $"[{statusStr}] ({reasonsStr})";
            if (fullStatus != _lastTitleStatus)
            {
                Console.Title = $"AeroCore Ground Station - {fullStatus}";
                _lastTitleStatus = fullStatus;
            }

            // Print Status
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = statusColor;
            if (statusStr == "OK") Console.Write(" OK ");
            else Console.Write($"{statusStr}");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]");

            if (!string.IsNullOrEmpty(reasonsStr))
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine($" ({reasonsStr})");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.WriteLine(" (Stable)");
            }

            Console.ResetColor();
        }

        private void PrintGauge(double value, double range, double warnThreshold)
        {
            var absValue = Math.Abs(value);
            ConsoleColor barColor;
            if (absValue > range) barColor = ConsoleColor.Red;
            else if (absValue > warnThreshold) barColor = ConsoleColor.Yellow;
            else barColor = ConsoleColor.Green;

            Span<char> buffer = stackalloc char[11];
            int fill = GaugeVisualizer.Fill(buffer, value, range);

            int center = buffer.Length / 2;

            // Print Left
            if (center > 0)
            {
                var leftPart = buffer.Slice(0, center);
                if (value < 0 && fill > 0)
                {
                    int barStart = Math.Max(0, center - fill);
                    if (barStart > 0)
                    {
                        PrintSegmentWithMarkers(leftPart.Slice(0, barStart));
                    }
                    Console.ForegroundColor = barColor;
                    Console.Out.Write(leftPart.Slice(barStart));
                }
                else
                {
                    PrintSegmentWithMarkers(leftPart);
                }
            }

            // Print Center (Anchor)
            Console.ForegroundColor = ConsoleColor.White;
            Console.Out.Write(buffer.Slice(center, 1));

            // Print Right
            if (center + 1 < buffer.Length)
            {
                var rightPart = buffer.Slice(center + 1);
                if (value > 0 && fill > 0)
                {
                    int barEnd = Math.Min(fill, rightPart.Length);
                    Console.ForegroundColor = barColor;
                    Console.Out.Write(rightPart.Slice(0, barEnd));

                    if (barEnd < rightPart.Length)
                    {
                        PrintSegmentWithMarkers(rightPart.Slice(barEnd));
                    }
                }
                else
                {
                    PrintSegmentWithMarkers(rightPart);
                }
            }
        }

        private void PrintSegmentWithMarkers(ReadOnlySpan<char> segment)
        {
            // Optimization: Batch contiguous writes of the same color to reduce Console syscalls.
            // Instead of writing character by character (~5-11 calls), we write in chunks (~1-3 calls).
            int start = 0;
            for (int i = 0; i < segment.Length; i++)
            {
                if (segment[i] == '+')
                {
                    // Flush previous DarkGray segment
                    if (i > start)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Out.Write(segment.Slice(start, i - start));
                    }

                    // Write marker
                    Console.ForegroundColor = ConsoleColor.Gray;
                    Console.Out.Write('+');

                    start = i + 1;
                }
            }

            // Flush remaining DarkGray segment
            if (start < segment.Length)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Out.Write(segment.Slice(start));
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
