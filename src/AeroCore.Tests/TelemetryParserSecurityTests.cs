using System;
using System.Text;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class TelemetryParserSecurityTests
    {
        [Fact]
        public void Parse_ByteSpan_ShouldRejectTrailingGarbage()
        {
            // "10,20,30,40" is valid
            // "10,20,30,40garbage" should be invalid
            string input = "10,20,30,40garbage";
            byte[] bytes = Encoding.UTF8.GetBytes(input);

            var result = TelemetryParser.Parse(new ReadOnlySpan<byte>(bytes));

            Assert.Null(result); // Should be null if strictly parsed
        }

        [Fact]
        public void Parse_CharSpan_ShouldRejectTrailingGarbage()
        {
            string input = "10,20,30,40garbage";
            var result = TelemetryParser.Parse(input.AsSpan());

            Assert.Null(result);
        }

        [Fact]
        public void Parse_CharSpan_ShouldRejectTrailingFields()
        {
            string input = "10,20,30,40,extra";
            var result = TelemetryParser.Parse(input.AsSpan());

            Assert.Null(result);
        }

        [Fact]
        public void Parse_CharSpan_ShouldRejectTrailingComma()
        {
            string input = "10,20,30,40,";
            var result = TelemetryParser.Parse(input.AsSpan());

            Assert.Null(result);
        }

        [Fact]
        public void Parse_CharSpan_ShouldAcceptTrailingWhitespace()
        {
            string input = "10,20,30,40   ";
            var result = TelemetryParser.Parse(input.AsSpan());

            Assert.NotNull(result);
            Assert.Equal(40, result.Value.Roll);
        }
    }
}
