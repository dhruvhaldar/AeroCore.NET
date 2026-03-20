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
                // Security: Enforce physical bounds to prevent out-of-range data injection
                if (value < -10000.0 || value > 100000.0) throw new ArgumentOutOfRangeException(nameof(Altitude), "Altitude must be between -10,000 and 100,000 feet.");
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
                // Security: Enforce physical bounds to prevent out-of-range data injection
                if (value < 0.0 || value > 10000.0) throw new ArgumentOutOfRangeException(nameof(Velocity), "Velocity must be between 0.0 and 10,000 knots.");
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
                // Security: Enforce physical bounds to prevent out-of-range data injection
                if (value < -180.0 || value > 180.0) throw new ArgumentOutOfRangeException(nameof(Pitch), "Pitch must be between -180.0 and 180.0 degrees.");
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
                // Security: Enforce physical bounds to prevent out-of-range data injection
                if (value < -180.0 || value > 180.0) throw new ArgumentOutOfRangeException(nameof(Roll), "Roll must be between -180.0 and 180.0 degrees.");
                _roll = value;
            }
        }

        public DateTime Timestamp { get; init; }

        internal TelemetryPacket(double altitude, double velocity, double pitch, double roll, DateTime timestamp)
        {
            if (!double.IsFinite(altitude)) throw new ArgumentException("Altitude must be finite.", nameof(altitude));
            if (altitude < -10000.0 || altitude > 100000.0) throw new ArgumentOutOfRangeException(nameof(altitude), "Altitude must be between -10,000 and 100,000 feet.");

            if (!double.IsFinite(velocity)) throw new ArgumentException("Velocity must be finite.", nameof(velocity));
            if (velocity < 0.0 || velocity > 10000.0) throw new ArgumentOutOfRangeException(nameof(velocity), "Velocity must be between 0.0 and 10,000 knots.");

            if (!double.IsFinite(pitch)) throw new ArgumentException("Pitch must be finite.", nameof(pitch));
            if (pitch < -180.0 || pitch > 180.0) throw new ArgumentOutOfRangeException(nameof(pitch), "Pitch must be between -180.0 and 180.0 degrees.");

            if (!double.IsFinite(roll)) throw new ArgumentException("Roll must be finite.", nameof(roll));
            if (roll < -180.0 || roll > 180.0) throw new ArgumentOutOfRangeException(nameof(roll), "Roll must be between -180.0 and 180.0 degrees.");

            _altitude = altitude;
            _velocity = velocity;
            _pitch = pitch;
            _roll = roll;
            Timestamp = timestamp;
        }
    }
}
