using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.FlightComputer.Services;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AeroCore.Tests
{
    public class FlightControlUnitTests
    {
        [Fact]
        public async Task ProcessLoopAsync_HighPitch_GeneratesAndExecutesCommands()
        {
            // Arrange
            var mockLogger = new Mock<ILogger<FlightControlUnit>>();
            mockLogger.Setup(x => x.IsEnabled(It.IsAny<LogLevel>())).Returns(true);

            var mockTelemetry = new Mock<ITelemetryProvider>();

            mockTelemetry.Setup(t => t.InitializeAsync(It.IsAny<CancellationToken>()))
                         .Returns(Task.CompletedTask);

            // Setup telemetry stream
            async IAsyncEnumerable<TelemetryPacket> GetTestTelemetry([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
            {
                // Yield a few packets with high pitch
                for (int i = 0; i < 10; i++)
                {
                    if (ct.IsCancellationRequested) break;
                    yield return new TelemetryPacket
                    {
                        Pitch = 10.0, // High pitch triggers command
                        Altitude = 1000,
                        Velocity = 200,
                        Timestamp = DateTime.UtcNow
                    };
                    await Task.Delay(10);
                }
            }

            mockTelemetry.Setup(t => t.StreamTelemetryAsync(It.IsAny<CancellationToken>()))
                         .Returns((CancellationToken ct) => GetTestTelemetry(ct));

            var fcu = new FlightControlUnit(mockTelemetry.Object, mockLogger.Object);
            // Use a timeout to ensure we don't hang, but let the stream finish naturally
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

            // Act
            await fcu.ProcessLoopAsync(cts.Token);

            // Allow a small grace period for the consumer task to process remaining items
            await Task.Delay(100);

            // Assert
            // Verify that "Executing Command" was logged.
            mockLogger.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Executing Command")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce,
                "Commands should be executed (dequeued) when Pitch is high.");
        }
    }
}
