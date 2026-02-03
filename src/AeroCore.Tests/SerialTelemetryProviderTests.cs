using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AeroCore.Tests
{
    public class SerialTelemetryProviderTests
    {
        [Fact]
        public async Task InitializeAsync_SanitizesPortNameInLogs_WhenConfigurationHasNewlines()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();

            // Setup in-memory configuration
            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Serial:PortName", "COM1\nINJECTED_LOG"},
                {"Serial:BaudRate", "9600"}
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var provider = new SerialTelemetryProvider(mockLogger.Object, config);

            // Act
            await provider.InitializeAsync(CancellationToken.None);

            // Assert
            // We verify that the LogInformation call contained the SANITIZED string ("COM1_INJECTED_LOG")
            // and NOT the original newline ("\n").

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("COM1_INJECTED_LOG") && !v.ToString()!.Contains('\n')),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "The port name should be sanitized in the logs to prevent log injection.");
        }
    }
}
