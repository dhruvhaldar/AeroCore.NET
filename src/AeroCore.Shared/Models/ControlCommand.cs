using System;

namespace AeroCore.Shared.Models
{
    // Represents a command sent to actuators
    public record ControlCommand
    {
        private string _actuatorId = "UNKNOWN";
        public string ActuatorId
        {
            get => _actuatorId;
            init
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("ActuatorId cannot be null or whitespace.", nameof(ActuatorId));
                }
                _actuatorId = value;
            }
        }

        private double _value;
        public double Value
        {
            get => _value;
            init
            {
                if (value < 0.0 || value > 1.0)
                {
                    throw new ArgumentOutOfRangeException(nameof(Value), value, "Value must be between 0.0 and 1.0.");
                }
                _value = value;
            }
        }

        public DateTime Timestamp { get; init; }
    }
}
