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
    public class SerialTelemetryProviderDoSTests
    {
        private readonly Mock<ILogger<SerialTelemetryProvider>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SerialTelemetryProvider _provider;

        public SerialTelemetryProviderDoSTests()
        {
            _mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

            _provider = new SerialTelemetryProvider(_mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task DoS_InfiniteCR_TriggersLineLimit()
        {
            // Simulate 2000 carriage returns (\r)
            int size = 2000;
            byte[] data = new byte[size];
            for (int i = 0; i < size; i++) data[i] = (byte)'\r';

            using var stream = new MemoryStream(data);
            var cts = new CancellationTokenSource();
            cts.CancelAfter(500); // Should be enough to process 2000 bytes

            try
            {
                await foreach (var packet in _provider.ProcessStreamAsync(stream, cts.Token))
                {
                    // No packets expected
                }
            }
            catch (TaskCanceledException) { }

            // Verify that LogWarning was called due to line limit exceeded.
            // The limit is 1024. Sending 2000 \r without \n should trigger it if we count them.
            _mockLogger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("exceeded length limit")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
