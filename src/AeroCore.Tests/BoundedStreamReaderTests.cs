using System;
using System.IO;
using System.Text;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class BoundedStreamReaderTests
    {
        [Fact]
        public void ReadSafeLine_ReadsNormalLine()
        {
            var input = "Hello World\n";
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));

            var result = BoundedStreamReader.ReadSafeLine(stream);

            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ReadSafeLine_HandlesCarriageReturn()
        {
            var input = "Line1\r\nLine2";
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));

            var result = BoundedStreamReader.ReadSafeLine(stream);

            Assert.Equal("Line1", result);
        }

        [Fact]
        public void ReadSafeLine_ThrowsOnTooLongLine()
        {
            var input = new string('A', 20);
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));

            // Limit to 10 chars
            Assert.Throws<InvalidOperationException>(() =>
                BoundedStreamReader.ReadSafeLine(stream, 10));
        }

        [Fact]
        public void ReadSafeLine_ReturnsNullOnEmptyStream()
        {
            using var stream = new MemoryStream(new byte[0]);
            var result = BoundedStreamReader.ReadSafeLine(stream);
            Assert.Null(result);
        }

        [Fact]
        public void ReadSafeLine_ReturnsPartialOnEndOfStream()
        {
            var input = "PartialLine";
            using var stream = new MemoryStream(Encoding.ASCII.GetBytes(input));

            var result = BoundedStreamReader.ReadSafeLine(stream);

            Assert.Equal("PartialLine", result);
        }
    }
}
