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

            // Expected CSV format: Altitude,Velocity,Pitch,Roll
            try
            {
                var parts = line.Split(',');
                if (parts.Length >= 4)
                {
                    return new TelemetryPacket
                    {
                        Altitude = double.Parse(parts[0], CultureInfo.InvariantCulture),
                        Velocity = double.Parse(parts[1], CultureInfo.InvariantCulture),
                        Pitch = double.Parse(parts[2], CultureInfo.InvariantCulture),
                        Roll = double.Parse(parts[3], CultureInfo.InvariantCulture),
                        Timestamp = DateTime.UtcNow
                    };
                }
            }
            catch
            {
                // In production code we might want to log this failure, but for a pure helper 
                // returning null is often sufficient or we could let exception bubble up.
                // Keeping it simple as per original design: swallow and return null.
            }
            return null;
        }
    }
}
