using System;

namespace AeroCore.Shared.Models
{
    // Represents a simplified 3-axis telemetry reading
    // Optimized as readonly record struct to avoid heap allocations
    public readonly record struct TelemetryPacket
    {
        public double Altitude { get; init; }
        public double Velocity { get; init; }
        public double Pitch { get; init; }
        public double Roll { get; init; }
        public DateTime Timestamp { get; init; }
    }
}
