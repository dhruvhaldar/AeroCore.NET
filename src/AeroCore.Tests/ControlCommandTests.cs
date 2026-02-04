using System;
using AeroCore.Shared.Models;
using Xunit;

namespace AeroCore.Tests
{
    public class ControlCommandTests
    {
        [Fact]
        public void ControlCommand_ValidData_CreatesInstance()
        {
            var command = new ControlCommand
            {
                ActuatorId = "RUDDER",
                Value = 0.5,
                Timestamp = DateTime.UtcNow
            };

            Assert.Equal("RUDDER", command.ActuatorId);
            Assert.Equal(0.5, command.Value);
        }

        [Fact]
        public void ControlCommand_DefaultConstructor_HasSafeDefaults()
        {
            var command = new ControlCommand();

            Assert.Equal("UNKNOWN", command.ActuatorId);
            Assert.Equal(0.0, command.Value);
        }

        [Theory]
        [InlineData(-0.1)]
        [InlineData(1.1)]
        [InlineData(-100.0)]
        [InlineData(100.0)]
        public void ControlCommand_InvalidValue_ThrowsArgumentOutOfRangeException(double invalidValue)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = "TEST",
                    Value = invalidValue
                };
            });
        }

        [Fact]
        public void ControlCommand_NullActuatorId_ThrowsArgumentException()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = null! // Force null to test validation
                };
            });
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void ControlCommand_EmptyOrWhitespaceActuatorId_ThrowsArgumentException(string invalidId)
        {
            Assert.Throws<ArgumentException>(() =>
            {
                new ControlCommand
                {
                    ActuatorId = invalidId
                };
            });
        }

        [Fact]
        public void ControlCommand_RecordWith_ValidatesNewValue()
        {
            var command = new ControlCommand
            {
                ActuatorId = "RUDDER",
                Value = 0.5
            };

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                // This uses the init accessor
                var badCommand = command with { Value = 1.5 };
            });
        }
    }
}
