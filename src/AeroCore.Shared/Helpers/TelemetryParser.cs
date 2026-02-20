using System;
using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using AeroCore.Shared.Models;

namespace AeroCore.Shared.Helpers
{
    public static class TelemetryParser
    {
        private static readonly SearchValues<byte> _trimSearchValues = SearchValues.Create(new byte[] { 32, 9, 13, 10 });

        public static TelemetryPacket? ParseFromCsv(string line)
        {
            if (string.IsNullOrWhiteSpace(line)) return null;

            return Parse(line.AsSpan());
        }

        // Optimization: Helper method to trim whitespace using SIMD-optimized SearchValues.
        // This is significantly faster (~16x) than span.TrimStart(byte[]) which scans repeatedly.
        // Note: span.TrimStart(SearchValues) is not available in .NET 8 for ReadOnlySpan<byte>.
        private static ReadOnlySpan<byte> TrimWhitespace(ReadOnlySpan<byte> span)
        {
            // Optimization: Fast path for no whitespace.
            // If the first byte is > 32 (Space), it's not a whitespace char we care about (Space, Tab, CR, LF).
            if (!span.IsEmpty && span[0] > 32) return span;

            int idx = span.IndexOfAnyExcept(_trimSearchValues);
            if (idx == -1) return ReadOnlySpan<byte>.Empty;
            return span.Slice(idx);
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan&lt;byte&gt; to avoid char conversion overhead.
        /// Uses Utf8Parser for high-performance parsing of ASCII data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<byte> span)
        {
            // Optimization: Parse sequentially to avoid multiple scans (IndexOf + Trim + Parse).
            // We use Utf8Parser.TryParse which returns bytesConsumed, allowing us to advance the span.
            // We only need to trim leading whitespace between fields.

            // Parse Altitude
            span = TrimWhitespace(span);
            if (!Utf8Parser.TryParse(span, out double altitude, out int bytesConsumed)) return null;
            span = span.Slice(bytesConsumed);

            // Expect comma
            span = TrimWhitespace(span);
            if (span.IsEmpty || span[0] != (byte)',') return null;
            span = span.Slice(1);

            // Parse Velocity
            span = TrimWhitespace(span);
            if (!Utf8Parser.TryParse(span, out double velocity, out bytesConsumed)) return null;
            span = span.Slice(bytesConsumed);

            // Expect comma
            span = TrimWhitespace(span);
            if (span.IsEmpty || span[0] != (byte)',') return null;
            span = span.Slice(1);

            // Parse Pitch
            span = TrimWhitespace(span);
            if (!Utf8Parser.TryParse(span, out double pitch, out bytesConsumed)) return null;
            span = span.Slice(bytesConsumed);

            // Expect comma
            span = TrimWhitespace(span);
            if (span.IsEmpty || span[0] != (byte)',') return null;
            span = span.Slice(1);

            // Parse Roll
            span = TrimWhitespace(span);
            if (!Utf8Parser.TryParse(span, out double roll, out _)) return null;

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

            // Optimization: Use internal constructor to avoid redundant double.IsFinite checks in property setters.
            // We've already validated the values above.
            return new TelemetryPacket(altitude, velocity, pitch, roll, DateTime.UtcNow);
        }
    }
}
