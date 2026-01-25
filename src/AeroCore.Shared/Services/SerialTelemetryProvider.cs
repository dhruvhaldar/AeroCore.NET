using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
using AeroCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AeroCore.Shared.Services
{
    public class SerialTelemetryProvider : ITelemetryProvider
    {
        private readonly ILogger<SerialTelemetryProvider> _logger;
        private readonly IConfiguration _config;
        private SerialPort? _serialPort;
        private string _portName = string.Empty;
        private int _baudRate;

        public SerialTelemetryProvider(ILogger<SerialTelemetryProvider> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        public Task InitializeAsync(CancellationToken ct)
        {
            var defaultPort = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "COM3" : "/dev/ttyUSB0";
            _portName = _config.GetValue<string>("Serial:PortName") ?? defaultPort;
            _baudRate = _config.GetValue<int>("Serial:BaudRate", 9600);

            _logger.LogInformation($"Initializing Serial Telemetry on {_portName} at {_baudRate} baud.");

            try
            {
                // In a real scenario, we might retry or fail fast.
                // For now we setup the object but don't open until streaming starts or now.
                // It's safer to open when needed or keep open.
                // Let's create the SerialPort instance.
                _serialPort = new SerialPort(_portName, _baudRate, Parity.None, 8, StopBits.One);
                _serialPort.ReadTimeout = 1000;
                // Note: SerialPort.Open() might fail if port doesn't exist.
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize serial port configuration.");
            }

            return Task.CompletedTask;
        }

        public async IAsyncEnumerable<TelemetryPacket> StreamTelemetryAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct)
        {
            if (_serialPort == null) yield break;

            try
            {
                if (!_serialPort.IsOpen)
                {
                    _logger.LogInformation("Opening serial port...");
                    _serialPort.Open();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not open serial port.");
                yield break;
            }

            _logger.LogInformation("Serial Telemetry Stream Started.");

            while (!ct.IsCancellationRequested && _serialPort.IsOpen)
            {
                string? line = null;
                try
                {
                    // Avoid blocking the thread pool with ReadLine by wrapping in Task.Run
                    // This is still not ideal compared to pipelines or async read, but vastly better than blocking.
                    line = await Task.Run(() =>
                    {
                        try
                        {
                            // Use BoundedStreamReader to prevent DoS via massive lines.
                            return BoundedStreamReader.ReadSafeLine(() => _serialPort.ReadChar(), 1024);
                        }
                        catch (TimeoutException)
                        {
                            return null;
                        }
                    }, ct);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (System.IO.InvalidDataException ex)
                {
                    _logger.LogWarning($"Telemetry line exceeded length limit: {ex.Message}");
                    continue;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from serial port.");
                    await Task.Delay(1000, ct);
                    continue;
                }

                if (!string.IsNullOrWhiteSpace(line))
                {
                    var packet = TelemetryParser.ParseFromCsv(line);
                    if (packet != null)
                    {
                        yield return packet;
                    }
                    else
                    {
                        // Sanitize input to prevent Log Injection
                        _logger.LogWarning($"Failed to parse telemetry line: '{SecurityHelper.SanitizeForLog(line)}'");
                    }
                }
            }

            if (_serialPort.IsOpen) _serialPort.Close();
        }
    }
}
