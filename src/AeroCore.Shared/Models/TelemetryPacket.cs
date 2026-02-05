using System;

namespace AeroCore.Shared.Models
{
    // Represents a simplified 3-axis telemetry reading
    // Optimized as readonly record struct to avoid heap allocations
    public readonly record struct TelemetryPacket
    {
        private readonly double _altitude;
        public double Altitude
        {
            get => _altitude;
            init
            {
                if (!double.IsFinite(value)) throw new ArgumentException("Altitude must be finite.", nameof(Altitude));
                _altitude = value;
            }
        }

        private readonly double _velocity;
        public double Velocity
        {
            get => _velocity;
            init
            {
                if (!double.IsFinite(value)) throw new ArgumentException("Velocity must be finite.", nameof(Velocity));
                _velocity = value;
            }
        }

        private readonly double _pitch;
        public double Pitch
        {
            get => _pitch;
            init
            {
                if (!double.IsFinite(value)) throw new ArgumentException("Pitch must be finite.", nameof(Pitch));
                _pitch = value;
            }
        }

        private readonly double _roll;
        public double Roll
        {
            get => _roll;
            init
            {
                if (!double.IsFinite(value)) throw new ArgumentException("Roll must be finite.", nameof(Roll));
                _roll = value;
            }
        }

        public DateTime Timestamp { get; init; }
    }
}
