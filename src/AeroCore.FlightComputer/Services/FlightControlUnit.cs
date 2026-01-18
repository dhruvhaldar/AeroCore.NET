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
                _logger.LogWarning($"[CORRECTION] Pitch High: {packet.Pitch:F2}. Adjusting Elevators.");
                
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
                _logger.LogInformation($"[STATUS] Alt: {packet.Altitude:F1}ft | Vel: {packet.Velocity:F1}kts | Pitch: {packet.Pitch:F2}");
            }
        }
    }
}
