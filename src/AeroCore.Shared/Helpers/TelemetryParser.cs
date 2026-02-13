using System;
using System.Buffers.Text;
using System.Globalization;
using AeroCore.Shared.Models;

namespace AeroCore.Shared.Helpers
{
    public static class TelemetryParser
    {
        public static TelemetryPacket? ParseFromCsv(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            return Parse(line.AsSpan());
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan&lt;byte&gt; to avoid char conversion overhead.
        /// Uses Utf8Parser for high-performance parsing of ASCII data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<byte> span)
        {
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

            return new TelemetryPacket
            {
                Altitude = altitude,
                Velocity = velocity,
                Pitch = pitch,
                Roll = roll,
                Timestamp = DateTime.UtcNow
            };
        }

        private static ReadOnlySpan<byte> Trim(ReadOnlySpan<byte> span)
        {
            int start = 0;
            while (start < span.Length && IsWhiteSpace(span[start]))
            {
                start++;
            }

            int end = span.Length - 1;
            while (end >= start && IsWhiteSpace(span[end]))
            {
                end--;
            }

            if (start > end) return ReadOnlySpan<byte>.Empty;
            return span.Slice(start, end - start + 1);
        }

        private static bool IsWhiteSpace(byte b)
        {
            // Check for space (32) and tab (9).
            // CR (13) and LF (10) are usually handled by line splitting, but we include them just in case.
            return b == 32 || b == 9 || b == 13 || b == 10;
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan to avoid string allocations.
        /// Optimized to use TryParse instead of try-catch for better performance on invalid data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<char> span)
        {
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

            return new TelemetryPacket
            {
                Altitude = altitude,
                Velocity = velocity,
                Pitch = pitch,
                Roll = roll,
                Timestamp = DateTime.UtcNow
            };
        }
    }
}
