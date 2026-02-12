using System;
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
    public class SerialTelemetryProviderLeakTest
    {
        private readonly Mock<ILogger<SerialTelemetryProvider>> _mockLogger;
        private readonly Mock<IConfiguration> _mockConfig;
        private readonly SerialTelemetryProvider _provider;

        public SerialTelemetryProviderLeakTest()
        {
            _mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();
            _mockConfig = new Mock<IConfiguration>();
            _mockConfig.Setup(c => c.GetSection(It.IsAny<string>())).Returns(new Mock<IConfigurationSection>().Object);

            _provider = new SerialTelemetryProvider(_mockLogger.Object, _mockConfig.Object);
        }

        [Fact]
        public async Task ProcessStreamAsync_InvalidData_DoesNotLogSensitiveContent()
        {
            // Simulate sensitive data in the stream that fails parsing
            string sensitiveData = "password=SuperSecret123!";
            byte[] data = Encoding.UTF8.GetBytes(sensitiveData + "\n");

            using var stream = new MemoryStream(data);
            var cts = new CancellationTokenSource();

            // Allow enough time to process
            cts.CancelAfter(500);

            try
            {
                await foreach (var packet in _provider.ProcessStreamAsync(stream, cts.Token))
                {
                    // No packets expected
                }
            }
            catch (TaskCanceledException) { }
            catch (Exception ex)
            {
                // Ignore other exceptions
            }

            // Verify that sensitive data is NOT logged
            // Currently, it IS logged, so this test should FAIL if the vulnerability exists.
            _mockLogger.Verify(
                x => x.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("SuperSecret123")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never,
                "Sensitive data was found in logs!");
        }
    }
}
