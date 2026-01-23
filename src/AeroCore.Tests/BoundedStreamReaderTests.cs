using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class BoundedStreamReaderTests
    {
        [Fact]
        public async Task ReadSafeLineAsync_ReturnsLine_WhenWithinLimit()
        {
            // Arrange
            string content = "Hello World\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            string? result = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);

            // Assert
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public async Task ReadSafeLineAsync_ReturnsTruncatedLine_WhenExceedsLimit()
        {
            // Arrange
            string content = "This line is too long";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            int limit = 10;

            // Act
            string? result = await BoundedStreamReader.ReadSafeLineAsync(ms, limit, CancellationToken.None);

            // Assert
            Assert.Equal("This line ", result);
        }

        [Fact]
        public async Task ReadSafeLineAsync_HandlesMultipleLines()
        {
            // Arrange
            string content = "Line1\nLine2\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            string? line1 = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);
            string? line2 = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);

            // Assert
            Assert.Equal("Line1", line1);
            Assert.Equal("Line2", line2);
        }

         [Fact]
        public async Task ReadSafeLineAsync_HandlesCRLF()
        {
            // Arrange
            string content = "Line1\r\nLine2";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));

            // Act
            string? line1 = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);
            string? line2 = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);

            // Assert
            Assert.Equal("Line1", line1);
            Assert.Equal("Line2", line2);
        }

        [Fact]
        public async Task ReadSafeLineAsync_ReturnsNull_AtEndOfStream()
        {
            // Arrange
            using var ms = new MemoryStream();

            // Act
            string? result = await BoundedStreamReader.ReadSafeLineAsync(ms, 100, CancellationToken.None);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task ReadSafeLineAsync_ContinuesReading_AfterTruncation()
        {
            // Arrange
            string content = "1234567890\n";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(content));
            int limit = 5;

            // Act
            // First read should hit limit
            string? part1 = await BoundedStreamReader.ReadSafeLineAsync(ms, limit, CancellationToken.None);
            // Second read should continue
            string? part2 = await BoundedStreamReader.ReadSafeLineAsync(ms, limit, CancellationToken.None);
            // Third read should find newline (or rest)
            string? part3 = await BoundedStreamReader.ReadSafeLineAsync(ms, limit, CancellationToken.None);

            // Assert
            Assert.Equal("12345", part1);
            Assert.Equal("67890", part2);
            Assert.Equal("", part3);
        }
    }
}
