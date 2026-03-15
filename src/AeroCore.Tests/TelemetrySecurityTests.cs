using System;
using AeroCore.Shared.Helpers;
using AeroCore.Shared.Models;
using Xunit;

namespace AeroCore.Tests
{
    public class TelemetrySecurityTests
    {
        [Fact]
        public void ParseFromCsv_NaNValues_ShouldBeRejected()
        {
            // This test ensures that NaN values are rejected to protect control logic.
            string line = "NaN,NaN,NaN,NaN";
            var packet = TelemetryParser.ParseFromCsv(line);

            // Sentinel wants secure default: invalid numbers should be rejected.
            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_InfinityValues_ShouldBeRejected()
        {
            string line = "Infinity,Infinity,Infinity,Infinity";
            var packet = TelemetryParser.ParseFromCsv(line);

            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_ExcessiveValues_ShouldBeRejected()
        {
            // Test massive numbers
            string line = "1e309,1e309,1e309,1e309"; // larger than double.MaxValue -> Infinity
            var packet = TelemetryParser.ParseFromCsv(line);

            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_OutOfBoundsAngles_ShouldBeRejected()
        {
            // Security: Prevent data injection causing out of bounds behavior
            string line1 = "1000,250,200,0"; // Pitch > 180
            string line2 = "1000,250,0,-181"; // Roll < -180

            Assert.Null(TelemetryParser.ParseFromCsv(line1));
            Assert.Null(TelemetryParser.ParseFromCsv(line2));
        }

        [Fact]
        public void TelemetryPacket_OutOfBoundsAngles_ShouldThrow()
        {
            // Security: Enforce domain physical bounds constraints
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryPacket { Altitude = 1000, Velocity = 250, Pitch = 200, Roll = 0 });
            Assert.Throws<ArgumentOutOfRangeException>(() => new TelemetryPacket { Altitude = 1000, Velocity = 250, Pitch = 0, Roll = -181 });
        }
    }
}
