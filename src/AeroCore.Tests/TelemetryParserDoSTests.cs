using System;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class TelemetryParserDoSTests
    {
        private const int MaxAllowedLength = 1024;

        [Fact]
        public void ParseFromCsv_ExceedsMaxLength_ShouldBeRejected()
        {
            // Create a string that exceeds the limit
            // Use valid-ish CSV content to ensure it's the length check rejecting it,
            // not the parser failing on "Infinity" or invalid format.
            // "1,1,1,1," repeated is safe.
            string chunk = "1,1,1,1,";
            int repetitions = (MaxAllowedLength / chunk.Length) + 5;
            string longLine = string.Concat(System.Linq.Enumerable.Repeat(chunk, repetitions));

            var packet = TelemetryParser.ParseFromCsv(longLine);

            // Sentinel wants secure default: excessively long inputs should be rejected.
            Assert.Null(packet);
        }

        [Fact]
        public void Parse_Bytes_ExceedsMaxLength_ShouldBeRejected()
        {
            // Create a buffer that exceeds the limit with valid-ish content
            string chunk = "1,1,1,1,";
            int repetitions = (MaxAllowedLength / chunk.Length) + 5;
            string longLine = string.Concat(System.Linq.Enumerable.Repeat(chunk, repetitions));
            byte[] longBuffer = System.Text.Encoding.UTF8.GetBytes(longLine);

            var packet = TelemetryParser.Parse(new ReadOnlySpan<byte>(longBuffer));

            Assert.Null(packet);
        }

        [Fact]
        public void Parse_Chars_ExceedsMaxLength_ShouldBeRejected()
        {
            // Create a char span that exceeds the limit
            string chunk = "1,1,1,1,";
            int repetitions = (MaxAllowedLength / chunk.Length) + 5;
            string longLine = string.Concat(System.Linq.Enumerable.Repeat(chunk, repetitions));

            var packet = TelemetryParser.Parse(new ReadOnlySpan<char>(longLine.ToCharArray()));

            Assert.Null(packet);
        }

        [Fact]
        public void ParseFromCsv_WithinLimit_ShouldBeProcessed()
        {
            // Ensure valid inputs within limit are still processed.
            string validLine = "100.0,20.0,5.0,1.0";
            var packet = TelemetryParser.ParseFromCsv(validLine);

            Assert.NotNull(packet);
        }
    }
}
