using System;
using System.Threading;
using System.Threading.Channels;
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

        // Channel for thread-safe, bounded command dispatching (DoS prevention)
        private readonly Channel<ControlCommand> _commandChannel;

        private static readonly Action<ILogger, double, Exception?> _logPitchCorrection = LoggerMessage.Define<double>(
            LogLevel.Warning,
            new EventId(1, "PitchCorrection"),
            "[CORRECTION] Pitch High: {Pitch:F2}. Adjusting Elevators.");

        private static readonly Action<ILogger, double, double, double, Exception?> _logStatus = LoggerMessage.Define<double, double, double>(
            LogLevel.Information,
            new EventId(2, "StatusUpdate"),
            "[STATUS] Alt: {Altitude:F1}ft | Vel: {Velocity:F1}kts | Pitch: {Pitch:F2}");

        private static readonly Action<ILogger, string, double, Exception?> _logCommandExecution = LoggerMessage.Define<string, double>(
            LogLevel.Information,
            new EventId(3, "CommandExec"),
            "[EXEC] Executing Command: {ActuatorId} = {Value:F2}");

        public FlightControlUnit(
            ITelemetryProvider telemetry,
            ILogger<FlightControlUnit> logger)
        {
            _telemetry = telemetry;
            _logger = logger;

            // Use bounded channel to prevent memory exhaustion (DoS)
            _commandChannel = Channel.CreateBounded<ControlCommand>(new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.DropOldest
            });
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

            // Start the command processor
            var commandTask = Task.Run(() => ProcessCommandsAsync(ct), ct);

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
            finally
            {
                // Ensure we wait for the processor to stop if we exit the loop
                try { await commandTask; } catch (OperationCanceledException) { }
            }
        }

        private async Task ProcessCommandsAsync(CancellationToken ct)
        {
            _logger.LogInformation("FCU: Command Processor Started.");
            try
            {
                await foreach (var cmd in _commandChannel.Reader.ReadAllAsync(ct))
                {
                    // Simulate execution time
                    _logCommandExecution(_logger, cmd.ActuatorId, cmd.Value, null);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected on shutdown
            }
            _logger.LogInformation("FCU: Command Processor Stopped.");
        }

        private void AnalyzeAndReact(TelemetryPacket packet)
        {
            // Simple PID-like logic (simulated)
            if (packet.Pitch > 0.5)
            {
                _logPitchCorrection(_logger, packet.Pitch, null);

                var cmd = new ControlCommand
                {
                    ActuatorId = "ELEVATOR_DOWN",
                    Value = 0.15,
                    Timestamp = DateTime.UtcNow
                };

                // Non-blocking write, drops oldest if full
                _commandChannel.Writer.TryWrite(cmd);
            }
            else
            {
                _logStatus(_logger, packet.Altitude, packet.Velocity, packet.Pitch, null);
            }
        }
    }
}
