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
    public class SerialTelemetryProviderDoubleParseTests
    {
        private readonly Mock<ILogger<SerialTelemetryProvider>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SerialTelemetryProvider _provider;

        public SerialTelemetryProviderDoubleParseTests()
        {
            _mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

            _provider = new SerialTelemetryProvider(_mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task ProcessStreamAsync_InvalidData_TriggersWarningAndDelay()
        {
            // Simulate invalid data followed by valid data
            string invalidLine = "INVALID,DATA,0,0\n";
            string validLine = "100.0,50.0,0.5,0.1\n";
            byte[] data = Encoding.UTF8.GetBytes(invalidLine + validLine);

            using var stream = new MemoryStream(data);
            var cts = new CancellationTokenSource();

            // We want to process at least the invalid line and verify warning.
            // The valid line might be delayed.

            var packets = new List<TelemetryPacket>();

            try
            {
                // Consume stream but cancel after some time if it hangs (due to delay)
                cts.CancelAfter(2000);

                await foreach (var packet in _provider.ProcessStreamAsync(stream, cts.Token))
                {
                    packets.Add(packet);
                    if (packets.Count >= 1) break; // We expect at least the valid packet eventually?
                    // Actually, if invalid packet triggers delay, we might get valid packet after delay.
                }
            }
            catch (TaskCanceledException) { }

            // Verify warning was logged for the invalid line
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse telemetry line")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce);

            // Verify we got the valid packet (proving we recovered)
            Assert.Contains(packets, p => p.Altitude == 100.0);
        }
    }
}
