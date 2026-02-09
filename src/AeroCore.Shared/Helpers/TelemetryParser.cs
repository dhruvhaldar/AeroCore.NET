using System;
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
