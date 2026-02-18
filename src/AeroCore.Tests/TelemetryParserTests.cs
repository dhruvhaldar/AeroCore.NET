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
            Assert.Equal(1000.5, packet.Value.Altitude);
            Assert.Equal(250.2, packet.Value.Velocity);
            Assert.Equal(0.5, packet.Value.Pitch);
            Assert.Equal(-0.1, packet.Value.Roll);
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

        [Fact]
        public void Parse_Bytes_ValidData_ReturnsPacket()
        {
            string line = "1000.5,250.2,0.5,-0.1";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line);
            var packet = TelemetryParser.Parse(bytes.AsSpan());

            Assert.NotNull(packet);
            Assert.Equal(1000.5, packet.Value.Altitude);
            Assert.Equal(250.2, packet.Value.Velocity);
            Assert.Equal(0.5, packet.Value.Pitch);
            Assert.Equal(-0.1, packet.Value.Roll);
        }

        [Fact]
        public void Parse_Bytes_WithWhitespace_ReturnsPacket()
        {
            // Utf8Parser doesn't handle whitespace by default, so this tests our Trim implementation
            string line = " 1000.5 , 250.2 , 0.5 , -0.1 ";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line);
            var packet = TelemetryParser.Parse(bytes.AsSpan());

            Assert.NotNull(packet);
            Assert.Equal(1000.5, packet.Value.Altitude);
            Assert.Equal(250.2, packet.Value.Velocity);
            Assert.Equal(0.5, packet.Value.Pitch);
            Assert.Equal(-0.1, packet.Value.Roll);
        }

        [Fact]
        public void Parse_Bytes_InvalidData_ReturnsNull()
        {
            string line = "Not,A,Valid,Csv";
            byte[] bytes = System.Text.Encoding.UTF8.GetBytes(line);
            var packet = TelemetryParser.Parse(bytes.AsSpan());

            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_LineTooLong_ReturnsNull()
        {
            // Max is 1024
            string longLine = new string('1', 1025);
            var packet = TelemetryParser.ParseFromCsv(longLine);

            Assert.Null(packet);
        }

        [Fact]
        public void Parse_SpanTooLong_ReturnsNull()
        {
            // Max is 1024
            byte[] longBytes = new byte[1025];
            var packet = TelemetryParser.Parse(longBytes.AsSpan());

            Assert.Null(packet);
        }
    }
}
