using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
using Microsoft.Extensions.Logging;

namespace AeroCore.FlightComputer.Services
{
    // The core logic engine
    public class FlightControlUnit : IFlightComputer
    {
        private readonly ITelemetryProvider _telemetry;
        private readonly ILogger<FlightControlUnit> _logger;

        // Queue for thread-safe command dispatching
        private readonly ConcurrentQueue<ControlCommand> _commandQueue = new();

        // Cached LoggerMessage delegates for high-frequency logging to avoid allocation
        private static readonly Action<ILogger, double, double, double, Exception?> _logStatus =
            LoggerMessage.Define<double, double, double>(
                LogLevel.Information,
                new EventId(1, nameof(AnalyzeAndReact)),
                "[STATUS] Alt: {Altitude:F1}ft | Vel: {Velocity:F1}kts | Pitch: {Pitch:F2}");

        private static readonly Action<ILogger, double, Exception?> _logCorrection =
            LoggerMessage.Define<double>(
                LogLevel.Warning,
                new EventId(2, nameof(AnalyzeAndReact)),
                "[CORRECTION] Pitch High: {Pitch:F2}. Adjusting Elevators.");

        public FlightControlUnit(
            ITelemetryProvider telemetry,
            ILogger<FlightControlUnit> logger)
        {
            _telemetry = telemetry;
            _logger = logger;
        }

        public Task InitializeAsync(CancellationToken ct)
        {
            _logger.LogInformation("FCU Initialized.");
            return Task.CompletedTask;
        }

        public async Task ProcessLoopAsync(CancellationToken ct)
        {
            _logger.LogInformation("FCU: Starting Main Control Loop...");
            
            // Ensure provider is initialized
            await _telemetry.InitializeAsync(ct);

            try
            {
                // Consuming the async stream from the provider
                await foreach (var packet in _telemetry.StreamTelemetryAsync(ct))
                {
                    AnalyzeAndReact(packet);
                    
                    if (ct.IsCancellationRequested) break;
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("FCU: Control Loop Cancelled.");
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "FCU: CRITICAL FAILURE");
                // In real aerospace, this would trigger a hardware watchdog reset
            }
        }

        private void AnalyzeAndReact(TelemetryPacket packet)
        {
            // Simple PID-like logic (simulated)
            if (packet.Pitch > 0.5)
            {
                // Use cached delegate to avoid string interpolation allocation
                _logCorrection(_logger, packet.Pitch, null);
                
                var cmd = new ControlCommand 
                { 
                    ActuatorId = "ELEVATOR_DOWN", 
                    Value = 0.15,
                    Timestamp = DateTime.UtcNow 
                };
                _commandQueue.Enqueue(cmd);
            }
            else
            {
                // Use cached delegate to avoid string interpolation allocation
                _logStatus(_logger, packet.Altitude, packet.Velocity, packet.Pitch, null);
            }
        }
    }
}
