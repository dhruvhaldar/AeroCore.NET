using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Models;
using AeroCore.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AeroCore.Tests
{
    public class SerialTelemetryProviderInjectionTests
    {
        private readonly Mock<ILogger<SerialTelemetryProvider>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SerialTelemetryProvider _provider;

        public SerialTelemetryProviderInjectionTests()
        {
            _mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

            _provider = new SerialTelemetryProvider(_mockLogger.Object, _mockConfig.Object);
        }

        private class ChunkedMemoryStream : MemoryStream
        {
            private readonly int _chunkSize;
            public ChunkedMemoryStream(byte[] buffer, int chunkSize) : base(buffer)
            {
                _chunkSize = chunkSize;
            }

            public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            {
                // Force partial reads to simulate network/serial splitting
                int toRead = Math.Min(count, _chunkSize);
                return await base.ReadAsync(buffer, offset, toRead, cancellationToken);
            }

            // Override synchronous Read as well just in case, though ProcessStreamAsync uses ReadAsync
            public override int Read(byte[] buffer, int offset, int count)
            {
                 int toRead = Math.Min(count, _chunkSize);
                 return base.Read(buffer, offset, toRead);
            }
        }

        [Fact]
        public async Task ProcessStreamAsync_LongLineSplitting_ShouldNotInjectPacket()
        {
            // Scenario:
            // 1. Send 1025 bytes of garbage (no newline).
            //    We use ChunkedMemoryStream to ensure this comes in a SINGLE read.
            //    This fills the line buffer (1024) and triggers the reset logic because idx == -1.
            // 2. The next read contains a valid packet.
            //    Because the previous logic reset linePos to 0, this packet is treated as the start of a new line.
            // Expected: The valid-looking packet should be discarded because it is the tail of a too-long line.

            int garbageSize = 1025;
            byte[] garbage = new byte[garbageSize];
            for (int i = 0; i < garbageSize; i++) garbage[i] = (byte)'A';

            string validPacket = "100,200,30,40\n";
            byte[] packetBytes = Encoding.ASCII.GetBytes(validPacket);

            byte[] combined = new byte[garbageSize + packetBytes.Length];
            Array.Copy(garbage, 0, combined, 0, garbageSize);
            Array.Copy(packetBytes, 0, combined, garbageSize, packetBytes.Length);

            // Set chunk size to exactly garbageSize so the first read gets only the garbage
            using var stream = new ChunkedMemoryStream(combined, garbageSize);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(500);

            var receivedPackets = new List<TelemetryPacket>();

            try
            {
                await foreach (var packet in _provider.ProcessStreamAsync(stream, cts.Token))
                {
                    receivedPackets.Add(packet);
                }
            }
            catch (TaskCanceledException) { }

            // If vulnerable, receivedPackets will contain 1 packet.
            Assert.Empty(receivedPackets);
        }
    }
}
