using System;
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
    public class TelemetryParsingTests
    {
        // We can't easily test SerialTelemetryProvider reading from a port without a real port or an interface abstraction wrapper around SerialPort.
        // But we can test the ParsePacket logic if we expose it or use a "Testable" version.
        // Since ParsePacket is private, we can't test it directly easily.
        // However, we can test the "MockTelemetryProvider" to ensure it yields data.

        [Fact]
        public async Task MockTelemetryProvider_ShouldYieldData()
        {
            var loggerMock = new Mock<ILogger<MockTelemetryProvider>>();
            var provider = new MockTelemetryProvider(loggerMock.Object);

            var cts = new CancellationTokenSource();
            cts.CancelAfter(2000); // Run for 2 seconds

            int count = 0;
            try
            {
                await foreach (var packet in provider.StreamTelemetryAsync(cts.Token))
                {
                    count++;
                    Assert.InRange(packet.Altitude, 10000, 10100);
                    if (count >= 3) break;
                }
            }
            catch (OperationCanceledException) { }

            Assert.True(count > 0, "Mock provider should yield at least one packet");
        }
    }
}
