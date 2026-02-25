using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AeroCore.Tests
{
    public class SerialTelemetryProviderLogFloodingTests
    {
        private readonly Mock<ILogger<SerialTelemetryProvider>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SerialTelemetryProvider _provider;

        public SerialTelemetryProviderLogFloodingTests()
        {
            _mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

            _provider = new SerialTelemetryProvider(_mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task ProcessStreamAsync_MultipleLongLines_ShouldRateLimitLogs()
        {
            // Simulate 5 long lines sent rapidly
            int lineLength = 1100; // > 1024 limit
            int lineCount = 5;
            using var ms = new MemoryStream();

            for (int i = 0; i < lineCount; i++)
            {
                byte[] data = new byte[lineLength];
                Array.Fill(data, (byte)'A');
                ms.Write(data, 0, data.Length);
                ms.WriteByte((byte)'\n');
            }
            ms.Position = 0;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(2000);

            try
            {
                await foreach (var packet in _provider.ProcessStreamAsync(ms, cts.Token))
                {
                    // No valid packets
                }
            }
            catch (TaskCanceledException) { }

            // Without rate limiting, this would be called 5 times.
            // With rate limiting (e.g. 1 sec), it should be called once (or maybe twice if test runs slow).
            // We assert strictly < 5 to prove rate limiting is active.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("exceeded length limit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtMost(2));
        }

        [Fact]
        public async Task ProcessStreamAsync_MultipleInvalidLines_ShouldRateLimitLogs()
        {
            // Simulate 100 short invalid lines sent rapidly
            int lineCount = 100;
            using var ms = new MemoryStream();

            for (int i = 0; i < lineCount; i++)
            {
                byte[] data = System.Text.Encoding.ASCII.GetBytes("INVALID_DATA");
                ms.Write(data, 0, data.Length);
                ms.WriteByte((byte)'\n');
            }
            ms.Position = 0;

            var cts = new CancellationTokenSource();
            cts.CancelAfter(2000);

            try
            {
                await foreach (var packet in _provider.ProcessStreamAsync(ms, cts.Token))
                {
                    // No valid packets
                }
            }
            catch (TaskCanceledException) { }

            // Without rate limiting, this would be called 100 times!
            // With rate limiting, should be very few.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Failed to parse")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtMost(5));
        }
    }
}
