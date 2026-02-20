using System;
using AeroCore.Shared.Models;
using Xunit;

namespace AeroCore.Tests
{
    public class ControlCommandSecurityTests
    {
        [Fact]
        public void ControlCommand_ActuatorIdTooLong_ThrowsArgumentException()
        {
            // DoS Protection: Ensure ActuatorId cannot be excessively long (e.g. > 50 chars)
            // A long ActuatorId could be used to flood logs or exhaust memory if not bounded.
            string longId = new string('A', 51);

            var ex = Assert.Throws<ArgumentException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = longId,
                    Value = 0.5
                };
            });

            Assert.Contains("ActuatorId cannot exceed 50 characters", ex.Message);
        }
    }
}
