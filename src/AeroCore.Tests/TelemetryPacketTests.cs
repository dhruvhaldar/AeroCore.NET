using System;
using AeroCore.Shared.Models;
using Xunit;

namespace AeroCore.Tests
{
    public class TelemetryPacketTests
    {
        [Fact]
        public void Init_WithNaNAltitude_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new TelemetryPacket { Altitude = double.NaN });
        }

        [Fact]
        public void Init_WithInfinityVelocity_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new TelemetryPacket { Velocity = double.PositiveInfinity });
        }

        [Fact]
        public void Init_WithNaNPitch_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new TelemetryPacket { Pitch = double.NaN });
        }

        [Fact]
        public void Init_WithInfinityRoll_ShouldThrow()
        {
            Assert.Throws<ArgumentException>(() => new TelemetryPacket { Roll = double.NegativeInfinity });
        }

        [Fact]
        public void Init_WithValidValues_ShouldSucceed()
        {
            var packet = new TelemetryPacket
            {
                Altitude = 100.0,
                Velocity = 50.0,
                Pitch = 10.0,
                Roll = -5.0,
                Timestamp = DateTime.UtcNow
            };

            Assert.Equal(100.0, packet.Altitude);
            Assert.Equal(50.0, packet.Velocity);
            Assert.Equal(10.0, packet.Pitch);
            Assert.Equal(-5.0, packet.Roll);
        }
    }
}
