using System;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class TelemetryParserTests
    {
        [Fact]
        public void ParseFromCsv_ValidData_ReturnsPacket()
        {
            string line = "1000.5,250.2,0.5,-0.1";
            var packet = TelemetryParser.ParseFromCsv(line);

            Assert.NotNull(packet);
            Assert.Equal(1000.5, packet.Altitude);
            Assert.Equal(250.2, packet.Velocity);
            Assert.Equal(0.5, packet.Pitch);
            Assert.Equal(-0.1, packet.Roll);
        }

        [Fact]
        public void ParseFromCsv_InvalidData_ReturnsNull()
        {
            string line = "Not,A,Valid,Csv";
            var packet = TelemetryParser.ParseFromCsv(line);

            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_EmptyString_ReturnsNull()
        {
            var packet = TelemetryParser.ParseFromCsv("");
            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_IncompleteData_ReturnsNull()
        {
            string line = "1000.5,250.2";
            var packet = TelemetryParser.ParseFromCsv(line);

            Assert.Null(packet);
        }
    }
}
