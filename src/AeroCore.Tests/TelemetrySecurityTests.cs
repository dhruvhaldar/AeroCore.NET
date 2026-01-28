using System;
using AeroCore.Shared.Helpers;
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
    }
}
