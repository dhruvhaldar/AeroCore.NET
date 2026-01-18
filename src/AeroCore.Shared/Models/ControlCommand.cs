using System;

namespace AeroCore.Shared.Models
{
    // Represents a command sent to actuators
    public record ControlCommand
    {
        public string ActuatorId { get; init; } = string.Empty;
        public double Value { get; init; } // 0.0 to 1.0
        public DateTime Timestamp { get; init; }
    }
}
