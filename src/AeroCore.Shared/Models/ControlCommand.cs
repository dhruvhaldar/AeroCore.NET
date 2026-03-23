using System;

namespace AeroCore.Shared.Models
{
    // Represents a command sent to actuators
    public readonly record struct ControlCommand
    {
        private readonly string? _actuatorId;
        public string ActuatorId
        {
            get => _actuatorId ?? "UNKNOWN";
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
                    bool isAsciiLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                    bool isAsciiDigit = (c >= '0' && c <= '9');
                    if (!isAsciiLetter && !isAsciiDigit && c != '_' && c != '-')
                    {
                        // Security: Do not include raw invalid characters in the exception message to prevent Log Injection
                        throw new ArgumentException("ActuatorId contains an invalid character. Only ASCII alphanumeric, underscore, and hyphen are allowed.", nameof(ActuatorId));
                    }
                }

                _actuatorId = value;
            }
        }

        private readonly double _value;
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

        public ControlCommand(string actuatorId, double value, DateTime timestamp)
        {
            if (string.IsNullOrWhiteSpace(actuatorId))
            {
                throw new ArgumentException("ActuatorId cannot be null or whitespace.", nameof(actuatorId));
            }

            if (actuatorId.Length > 50)
            {
                throw new ArgumentException("ActuatorId cannot exceed 50 characters.", nameof(actuatorId));
            }

            foreach (char c in actuatorId)
            {
                bool isAsciiLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                bool isAsciiDigit = (c >= '0' && c <= '9');
                if (!isAsciiLetter && !isAsciiDigit && c != '_' && c != '-')
                {
                    throw new ArgumentException("ActuatorId contains an invalid character. Only ASCII alphanumeric, underscore, and hyphen are allowed.", nameof(actuatorId));
                }
            }

            if (!double.IsFinite(value))
            {
                throw new ArgumentException("Value must be a finite number.", nameof(value));
            }

            if (value < 0.0 || value > 1.0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Value must be between 0.0 and 1.0.");
            }

            _actuatorId = actuatorId;
            _value = value;
            Timestamp = timestamp;
        }
    }
}
