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

                // Security: Prevent DoS via memory exhaustion or log flooding
                if (value.Length > 50)
                {
                    throw new ArgumentException("ActuatorId cannot exceed 50 characters.", nameof(ActuatorId));
                }

                foreach (char c in value)
                {
                    if (!char.IsLetterOrDigit(c) && c != '_' && c != '-')
                    {
                        throw new ArgumentException($"ActuatorId contains invalid character '{c}'. Only alphanumeric, underscore, and hyphen are allowed.", nameof(ActuatorId));
                    }
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
                // Security: Prevent NaN/Infinity from propagating to control logic
                if (!double.IsFinite(value))
                {
                    throw new ArgumentException("Value must be a finite number.", nameof(Value));
                }

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
