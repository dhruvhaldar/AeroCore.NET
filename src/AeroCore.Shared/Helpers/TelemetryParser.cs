using System;
using System.Buffers.Text;
using System.Globalization;
using AeroCore.Shared.Models;

namespace AeroCore.Shared.Helpers
{
    public static class TelemetryParser
    {
        private const int MaxLineLength = 1024;
        private static readonly byte[] _trimChars = new byte[] { 32, 9, 13, 10 };

        public static TelemetryPacket? ParseFromCsv(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;
            if (line.Length > MaxLineLength) return null;

            return Parse(line.AsSpan());
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan&lt;byte&gt; to avoid char conversion overhead.
        /// Uses Utf8Parser for high-performance parsing of ASCII data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<byte> span)
        {
            if (span.Length > MaxLineLength) return null;

            // Parse Altitude
            int idx = span.IndexOf((byte)',');
            if (idx == -1) return null;

            // Utf8Parser does not handle whitespace by default, so we trim.
            if (!Utf8Parser.TryParse(Trim(span.Slice(0, idx)), out double altitude, out _)) return null;
            span = span.Slice(idx + 1);

            // Parse Velocity
            idx = span.IndexOf((byte)',');
            if (idx == -1) return null;

            if (!Utf8Parser.TryParse(Trim(span.Slice(0, idx)), out double velocity, out _)) return null;
            span = span.Slice(idx + 1);

            // Parse Pitch
            idx = span.IndexOf((byte)',');
            if (idx == -1) return null;

            if (!Utf8Parser.TryParse(Trim(span.Slice(0, idx)), out double pitch, out _)) return null;
            span = span.Slice(idx + 1);

            // Parse Roll
            idx = span.IndexOf((byte)',');
            ReadOnlySpan<byte> rollSpan = (idx == -1) ? span : span.Slice(0, idx);

            if (!Utf8Parser.TryParse(Trim(rollSpan), out double roll, out _)) return null;

            // Security: Prevent NaN/Infinity from propagating to control logic
            if (!double.IsFinite(altitude) || !double.IsFinite(velocity) ||
                !double.IsFinite(pitch) || !double.IsFinite(roll))
            {
                return null;
            }

            // Optimization: Use internal constructor to avoid redundant double.IsFinite checks in property setters.
            // We've already validated the values above.
            return new TelemetryPacket(altitude, velocity, pitch, roll, DateTime.UtcNow);
        }

        private static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> span)
        {
            // Use built-in vectorized Trim which is significantly faster than manual loop.
            // Trims space (32), tab (9), CR (13), and LF (10).
            return span.Trim(_trimChars);
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan to avoid string allocations.
        /// Optimized to use TryParse instead of try-catch for better performance on invalid data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<char> span)
        {
            if (span.Length > MaxLineLength) return null;

            // Parse Altitude
            int idx = span.IndexOf(',');
            if (idx == -1) return null;
            if (!double.TryParse(span.Slice(0, idx), CultureInfo.InvariantCulture, out double altitude)) return null;
            span = span.Slice(idx + 1);

            // Parse Velocity
            idx = span.IndexOf(',');
            if (idx == -1) return null;
            if (!double.TryParse(span.Slice(0, idx), CultureInfo.InvariantCulture, out double velocity)) return null;
            span = span.Slice(idx + 1);

            // Parse Pitch
            idx = span.IndexOf(',');
            if (idx == -1) return null;
            if (!double.TryParse(span.Slice(0, idx), CultureInfo.InvariantCulture, out double pitch)) return null;
            span = span.Slice(idx + 1);

            // Parse Roll
            // Take until next comma or end of string.
            // This handles cases with or without trailing extra fields.
            idx = span.IndexOf(',');
            ReadOnlySpan<char> rollSpan = (idx == -1) ? span : span.Slice(0, idx);
            if (!double.TryParse(rollSpan, CultureInfo.InvariantCulture, out double roll)) return null;

            // Security: Prevent NaN/Infinity from propagating to control logic
            if (!double.IsFinite(altitude) || !double.IsFinite(velocity) ||
                !double.IsFinite(pitch) || !double.IsFinite(roll))
            {
                return null;
            }

            // Optimization: Use internal constructor to avoid redundant double.IsFinite checks in property setters.
            // We've already validated the values above.
            return new TelemetryPacket(altitude, velocity, pitch, roll, DateTime.UtcNow);
        }
    }
}
