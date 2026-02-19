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
    public class SerialTelemetryProviderLogInjectionTests
    {
        [Fact]
        public async Task InitializeAsync_DoesNotLogRawControlCharactersInExceptionMessage_WhenPortNameIsInvalid()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<SerialTelemetryProvider>>();

            // Malicious port name containing newline (Log Injection)
            string maliciousPortName = "COM1\nINJECTED_LOG";

            var inMemorySettings = new Dictionary<string, string?>
            {
                {"Serial:PortName", maliciousPortName},
                {"Serial:BaudRate", "9600"}
            };

            IConfiguration config = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var provider = new SerialTelemetryProvider(mockLogger.Object, config);

            // Act
            await provider.InitializeAsync(CancellationToken.None);

            // Assert
            // Verify that LogError was called with an ArgumentException whose message does NOT contain '\n'.
            // The current vulnerable implementation will log the raw message containing '\n'.
            // The fix should sanitize it to something like 'COM1_INJECTED_LOG'.

            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.Is<Exception>(ex =>
                        ex is ArgumentException &&
                        !ex.Message.Contains('\n') &&
                        !ex.Message.Contains('\r')
                    ),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once,
                "The exception message logged via LogError contained control characters (Log Injection Vulnerability).");
        }
    }
}
