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
        private static readonly char[] _spinnerChars = { '⠋', '⠙', '⠹', '⠸', '⠼', '⠴', '⠦', '⠧', '⠇', '⠏' };

        // Rate limiter state for UI updates (to prevent console I/O blocking stream processing)
        private long _lastUiUpdate = 0;
        private int _lastStatusFlags = -1;

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
            Console.WriteLine("════════════════════════════════════════════════════════════");
            Console.WriteLine("              AEROCORE GROUND STATION v1.0");
            Console.WriteLine("════════════════════════════════════════════════════════════");
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
            Console.Write("  (Press ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("Ctrl+C");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" to exit)");
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

            // Attitude Indicator Legend (Explicit 1-to-1 Mapping)
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("◄■■+■");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("■+■■►");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] : Attitude Indicator (Active) | [");
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("+");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("|");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" ");
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write("+");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("   ");
            Console.Write("]");
            Console.WriteLine(" : Attitude Indicator (Neutral)");

            // Trend Indicator Legend (Explicit 1-to-1 Mapping)
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("↑↑");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : Fast Rise | ");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("↑ ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : Slow Rise | ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("→ ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : Stable | ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("↓ ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" : Slow Fall | ");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("↓↓");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine(" : Fast Fall");

            // Status Indicator Legend (Explicit 1-to-1 Mapping)
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write(" OK ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] : Stable | [");
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write("WARN");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] : Warning | [");
            Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("CRIT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("] : Critical (Audible Alert)");

            // Stream Liveness Legend
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write("⠋");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("] : Stream Liveness (Rotating = Active, Color matches status)");

            // Dynamic Label Note
            Console.Write("   ");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.WriteLine("* Alerts highlight labels and display values with high-contrast backgrounds.");

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

                Console.Clear();
                ShowWelcomeBanner();
                _logger.LogInformation("Ground Station Listening for Telemetry...");

                // UX: Show initial empty state with helpful guidance while waiting for the first telemetry packet
                Console.Write("\r");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("[");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write("⠋");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("] ");
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write("AWAITING TELEMETRY STREAM... ");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write("(Ensure sensor is connected and transmitting)");
                Console.ResetColor();

                // Register a callback to print a newline upon cancellation to prevent shutdown logs
                // from appending to the in-place updating (\r) telemetry line.
                using var _ = stoppingToken.Register(() => Console.WriteLine());

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
            // Optimization: Fetch the decimal separator once per UI tick to avoid repeated thread context
            // lookups of CultureInfo.CurrentCulture in the hot path.
            char decSep = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator[0];

            double absPitch = Math.Abs(packet.Pitch);
            double absRoll = Math.Abs(packet.Roll);

            bool critAlt = packet.Altitude < 0;
            bool critPit = absPitch > 45;
            bool critRol = absRoll > 45;
            bool isCrit = critAlt || critPit || critRol;

            bool warnVel = packet.Velocity > 100;
            bool warnPit = !critPit && absPitch > 35;
            bool warnRol = !critRol && absRoll > 35;
            bool isWarn = warnVel || warnPit || warnRol;

            // Build Status String for Title and Console
            string statusStr = "OK";
            ConsoleColor statusColor = ConsoleColor.Green;

            Span<char> reasons = stackalloc char[15]; // Max "ALT,PIT,ROL" is 11 chars
            int reasonsPos = 0;

            if (isCrit)
            {
                statusStr = "CRIT";
                statusColor = ConsoleColor.Red;

                bool first = true;
                if (critAlt) { "ALT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (critPit) { if (!first) { reasons[reasonsPos++] = ','; } "PIT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (critRol) { if (!first) { reasons[reasonsPos++] = ','; } "ROL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; }
            }
            else if (isWarn)
            {
                statusStr = "WARN";
                statusColor = ConsoleColor.Yellow;

                bool first = true;
                if (warnVel) { "VEL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (warnPit) { if (!first) { reasons[reasonsPos++] = ','; } "PIT".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; first = false; }
                if (warnRol) { if (!first) { reasons[reasonsPos++] = ','; } "ROL".AsSpan().CopyTo(reasons.Slice(reasonsPos)); reasonsPos += 3; }
            }

            // Spinner
            // UX: The initial "AWAITING TELEMETRY STREAM..." string might be longer than the incoming telemetry line.
            // When updating in-place using '\r', we must pad the new line to overwrite residual characters.
            // We use trailing spaces at the very end of the line for this, but to keep the structural layout clean
            // we first just write the spinner. We will handle the trailing spaces at the end of this method.
            Console.Write("\r");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = statusColor;
            Console.Write(_spinnerChars[_spinnerIndex]);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] ");
            _spinnerIndex = (_spinnerIndex + 1) % _spinnerChars.Length;
            Console.ResetColor();

            // Timestamp Prefix
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[GCS] @ ");

            // Dynamic Timestamp (Main time high contrast, fractional seconds dimmed to reduce flicker)
            // Optimization: Format DateTime once to "HH:mm:ss.fff" instead of formatting twice.
            // This eliminates redundant tick-to-calendar-part conversion overhead in this high-frequency loop.
            Console.ForegroundColor = ConsoleColor.White;
            Span<char> tsBuffer = stackalloc char[32];

            if (packet.Timestamp.TryFormat(tsBuffer, out int tsWritten, "HH:mm:ss.fff"))
            {
                // "HH:mm:ss" is exactly 8 characters
                Console.Out.Write(tsBuffer.Slice(0, 8));

                if (!isWarn && !isCrit)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }

                Console.Out.Write(tsBuffer.Slice(8, tsWritten - 8));
            }
            else
            {
                string tsStr = packet.Timestamp.ToString("HH:mm:ss.fff");
                Console.Out.Write(tsStr.Substring(0, 8));

                if (!isWarn && !isCrit)
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                }

                Console.Out.Write(tsStr.Substring(8));
            }

            // Timestamp Suffix
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" | ");

            // Altitude
            Console.ForegroundColor = critAlt ? ConsoleColor.Red : ConsoleColor.Cyan;
            Console.Write("ALT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            if (critAlt)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            WriteFormatted(packet.Altitude, 10, "N2", critAlt, decSep);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" ft ");

            if (_lastAltitude.HasValue)
            {
                double delta = packet.Altitude - _lastAltitude.Value;
                if (delta > 1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("↑↑");
                }
                else if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("↑ ");
                }
                else if (delta < -1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("↓↓");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("↓ ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("→ ");
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
            Console.ForegroundColor = warnVel ? ConsoleColor.Yellow : ConsoleColor.Cyan;
            Console.Write("VEL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            if (warnVel)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            WriteFormatted(packet.Velocity, 7, "N1", warnVel, decSep);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" kts ");

            if (_lastVelocity.HasValue)
            {
                double delta = packet.Velocity - _lastVelocity.Value;
                if (delta > 1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("↑↑");
                }
                else if (delta > 0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("↑ ");
                }
                else if (delta < -1.0)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("↓↓");
                }
                else if (delta < -0.1)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Write("↓ ");
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.Write("→ ");
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
            Console.ForegroundColor = critPit ? ConsoleColor.Red : (warnPit ? ConsoleColor.Yellow : ConsoleColor.Cyan);
            Console.Write("PIT");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            if (critPit)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (warnPit)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            WriteFormatted(packet.Pitch, 7, "+0.00;-0.00; 0.00", critPit || warnPit, decSep);
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Gray;
            Console.Write(" deg");

            // Pitch Visual
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(" [");
            PrintGauge(packet.Pitch, 45.0, 35.0);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("] | ");

            // Roll
            Console.ForegroundColor = critRol ? ConsoleColor.Red : (warnRol ? ConsoleColor.Yellow : ConsoleColor.Cyan);
            Console.Write("ROL");
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write(": ");
            if (critRol)
            {
                Console.BackgroundColor = ConsoleColor.Red;
                Console.ForegroundColor = ConsoleColor.White;
            }
            else if (warnRol)
            {
                Console.BackgroundColor = ConsoleColor.Yellow;
                Console.ForegroundColor = ConsoleColor.Black;
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.White;
            }
            WriteFormatted(packet.Roll, 7, "+0.00;-0.00; 0.00", critRol || warnRol, decSep);
            Console.ResetColor();
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

            // Update Console Title (only if changed)
            int currentStatusFlags = (critAlt ? 1 : 0) | (critPit ? 2 : 0) | (critRol ? 4 : 0) |
                                     (warnVel ? 8 : 0) | (warnPit ? 16 : 0) | (warnRol ? 32 : 0);

            if (currentStatusFlags != _lastStatusFlags)
            {
                // Optimization: Avoid multiple string allocations (reasonsStr, fullStatus, Console.Title)
                // by using string interpolation that natively supports ReadOnlySpan<char> in .NET.
                if (reasonsPos > 0)
                {
                    Console.Title = $"AeroCore Ground Station - [{statusStr}] ({reasons.Slice(0, reasonsPos)})";
                }
                else
                {
                    Console.Title = $"AeroCore Ground Station - [{statusStr}]";
                }

                // Emitting an auditory alert when a new CRITICAL state occurs or if CRITICAL reasons change
                if (isCrit)
                {
                    Console.Write("\a");
                }

                _lastStatusFlags = currentStatusFlags;
            }

            // Print Status
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("[");
            Console.ForegroundColor = statusColor;
            if (statusStr == "OK") Console.Write(" OK ");
            else Console.Write(statusStr);
            Console.ForegroundColor = ConsoleColor.DarkGray;
            Console.Write("]");

            if (reasonsPos > 0)
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" (");
                Console.ForegroundColor = statusColor;
                Console.Out.Write(reasons.Slice(0, reasonsPos));
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(")");

                // UX: Ensure trailing spaces pad out the dynamic status to clear residual chars
                // from longer warnings or the initial "AWAITING TELEMETRY STREAM..." text.
                // The awaiting stream text is ~70 chars. The telemetry status is usually shorter.
                int padding = Math.Max(0, 11 - reasonsPos + 40);
                Span<char> spaces = stackalloc char[padding];
                spaces.Fill(' ');
                Console.Out.Write(spaces);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.DarkGray;
                Console.Write(" (");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("Stable");
                Console.ForegroundColor = ConsoleColor.DarkGray;
                // UX: Pad significantly to overwrite the initial "AWAITING TELEMETRY STREAM..." string
                Console.Write(")                                        ");
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

        private void WriteFormatted(double value, int width, ReadOnlySpan<char> format, bool isAlert, char decSep)
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

                        if (!isAlert)
                        {
                            int dotIndex = valueBuffer.Slice(0, charsWritten).IndexOf(decSep);
                            if (dotIndex >= 0)
                            {
                                Console.Out.Write(combinedBuffer.Slice(0, padding + dotIndex));
                                var prevColor = Console.ForegroundColor;
                                Console.ForegroundColor = ConsoleColor.DarkGray;
                                Console.Out.Write(combinedBuffer.Slice(padding + dotIndex, charsWritten - dotIndex));
                                Console.ForegroundColor = prevColor;
                                return;
                            }
                        }

                        Console.Out.Write(combinedBuffer.Slice(0, padding + charsWritten));
                        return;
                    }

                    // Fallback to split writes if buffer is too small (unlikely)
                    Span<char> spaces = stackalloc char[padding];
                    spaces.Fill(' ');
                    Console.Out.Write(spaces);
                }

                if (!isAlert)
                {
                    int dotIndex = valueBuffer.Slice(0, charsWritten).IndexOf(decSep);
                    if (dotIndex >= 0)
                    {
                        Console.Out.Write(valueBuffer.Slice(0, dotIndex));
                        var prevColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Out.Write(valueBuffer.Slice(dotIndex, charsWritten - dotIndex));
                        Console.ForegroundColor = prevColor;
                        return;
                    }
                }

                Console.Out.Write(valueBuffer.Slice(0, charsWritten));
            }
            else
            {
                // Fallback
                string fallbackStr = value.ToString(format.ToString());
                if (!isAlert)
                {
                    int dotIndex = fallbackStr.IndexOf(decSep);
                    if (dotIndex >= 0)
                    {
                        Console.Write(fallbackStr.Substring(0, dotIndex));
                        var prevColor = Console.ForegroundColor;
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.Write(fallbackStr.Substring(dotIndex));
                        Console.ForegroundColor = prevColor;
                        return;
                    }
                }
                Console.Write(fallbackStr);
            }
        }
    }
}
