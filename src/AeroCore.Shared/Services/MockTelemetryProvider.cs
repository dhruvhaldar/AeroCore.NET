using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AeroCore.Shared.Services
{
    // Simulates reading from hardware sensors (e.g., I2C/SPI)
    public class MockTelemetryProvider : ITelemetryProvider
    {
        private readonly ILogger<MockTelemetryProvider> _logger;
        private readonly Random _rng = new Random();

        public MockTelemetryProvider(ILogger<MockTelemetryProvider> logger)
        {
            _logger = logger;
        }

        public Task InitializeAsync(CancellationToken ct)
        {
            _logger.LogInformation("Mock Telemetry Sensor Initialized.");
            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<TelemetryPacket> StreamTelemetryAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                // Simulate sensor jitter and processing delay
                await Task.Delay(500, ct);

                var data = new TelemetryPacket
                {
                    Altitude = 10000 + (_rng.NextDouble() * 100),
                    Velocity = 250 + (_rng.NextDouble() * 10),
                    Pitch = (_rng.NextDouble() * 2) - 1, // +/- 1 degree
                    Roll = (_rng.NextDouble() * 2) - 1,
                    Timestamp = DateTime.UtcNow
                };

                // Yield return allows streaming data without buffering the whole set
                yield return data;
            }
        }
    }
}
