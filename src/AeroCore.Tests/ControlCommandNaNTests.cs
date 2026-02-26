using System;
using AeroCore.Shared.Models;
using Xunit;

namespace AeroCore.Tests
{
    public class ControlCommandNaNTests
    {
        [Fact]
        public void ControlCommand_NaNValue_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = "TEST_ACTUATOR",
                    Value = double.NaN,
                    Timestamp = DateTime.UtcNow
                };
            });
        }

        [Fact]
        public void ControlCommand_InfinityValue_ThrowsArgumentException()
        {
             // Infinity is technically caught by > 1.0, but we want to be explicit about it being an ArgumentException (or ArgumentOutOfRange)
             // Ideally we want to prevent non-finite numbers explicitly.
             // Currently > 1.0 throws ArgumentOutOfRangeException.
             // Let's see what happens.
            Assert.ThrowsAny<ArgumentException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = "TEST_ACTUATOR",
                    Value = double.PositiveInfinity,
                    Timestamp = DateTime.UtcNow
                };
            });
        }
    }
}
