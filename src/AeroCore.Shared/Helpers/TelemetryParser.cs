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
        /// </summary>
        private static TelemetryPacket? Parse(ReadOnlySpan<char> span)
        {
            try
            {
                // Parse Altitude
                int idx = span.IndexOf(',');
                if (idx == -1) return null;
                double altitude = double.Parse(span.Slice(0, idx), CultureInfo.InvariantCulture);
                span = span.Slice(idx + 1);

                // Parse Velocity
                idx = span.IndexOf(',');
                if (idx == -1) return null;
                double velocity = double.Parse(span.Slice(0, idx), CultureInfo.InvariantCulture);
                span = span.Slice(idx + 1);

                // Parse Pitch
                idx = span.IndexOf(',');
                if (idx == -1) return null;
                double pitch = double.Parse(span.Slice(0, idx), CultureInfo.InvariantCulture);
                span = span.Slice(idx + 1);

                // Parse Roll
                // Take until next comma or end of string.
                // This handles cases with or without trailing extra fields.
                idx = span.IndexOf(',');
                ReadOnlySpan<char> rollSpan = (idx == -1) ? span : span.Slice(0, idx);
                double roll = double.Parse(rollSpan, CultureInfo.InvariantCulture);

                return new TelemetryPacket
                {
                    Altitude = altitude,
                    Velocity = velocity,
                    Pitch = pitch,
                    Roll = roll,
                    Timestamp = DateTime.UtcNow
                };
            }
            catch
            {
                // In case of parsing error (e.g. invalid double format), return null.
                return null;
            }
        }
    }
}
