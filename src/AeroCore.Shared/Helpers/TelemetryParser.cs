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
            // Security: Prevent CPU/Memory exhaustion DoS via excessively long input
            if (span.Length > 1024) return null;

            // Optimization: Parse sequentially to avoid multiple scans (IndexOf + Trim + Parse).
            // We use Utf8Parser.TryParse which returns bytesConsumed, allowing us to advance the span.
            // We also optimize by inlining whitespace checks to avoid method call overhead on the hot path (compact CSV).

            if (!ParseDouble(ref span, out double altitude)) return null;
            if (!SkipComma(ref span)) return null;

            if (!ParseDouble(ref span, out double velocity)) return null;
            if (!SkipComma(ref span)) return null;

            if (!ParseDouble(ref span, out double pitch)) return null;
            if (!SkipComma(ref span)) return null;

            // Roll (last value, no comma check)
            // Inline fast path of TrimWhitespace
            if (span.IsEmpty || span[0] <= 32)
            {
                span = TrimWhitespace(span);
            }
            if (!Utf8Parser.TryParse(span, out double roll, out int bytesConsumed)) return null;

            // Security: Ensure no trailing garbage to prevent injection/integrity issues
            if (!TrimWhitespace(span.Slice(bytesConsumed)).IsEmpty) return null;

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

        private static bool ParseDouble(ref ReadOnlySpan<byte> span, out double value)
        {
            // Optimization: Fast path for no whitespace (compact CSV).
            // If the first byte is > 32 (e.g. digit, dot, minus), it's not whitespace.
            // This avoids the TrimWhitespace method call overhead.
            if (span.IsEmpty || span[0] <= 32)
            {
                span = TrimWhitespace(span);
            }

            if (!Utf8Parser.TryParse(span, out value, out int bytesConsumed)) return false;
            span = span.Slice(bytesConsumed);
            return true;
        }

        private static bool SkipComma(ref ReadOnlySpan<byte> span)
        {
            // Optimization: Fast path for comma immediately following number.
            // 44 is comma, which is > 32. So we check if it's comma directly.
            // If it is comma, we slice and return true.
            if (!span.IsEmpty && span[0] == (byte)',')
            {
                span = span.Slice(1);
                return true;
            }

            // Slow path: potential whitespace
            span = TrimWhitespace(span);
            if (!span.IsEmpty && span[0] == (byte)',')
            {
                span = span.Slice(1);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Parses telemetry data using ReadOnlySpan to avoid string allocations.
        /// Optimized to use TryParse instead of try-catch for better performance on invalid data.
        /// </summary>
        public static TelemetryPacket? Parse(ReadOnlySpan<char> span)
        {
            // Security: Prevent CPU/Memory exhaustion DoS via excessively long input
            if (span.Length > 1024) return null;

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
            // Security: Enforce strict parsing (no trailing fields) to match byte[] overload behavior
            // and prevent data injection or ambiguity.
            if (span.IndexOf(',') != -1) return null;
            if (!double.TryParse(span, CultureInfo.InvariantCulture, out double roll)) return null;

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
