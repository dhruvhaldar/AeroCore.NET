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

            _logger.LogInformation($"Initializing Serial Telemetry on {SecurityHelper.SanitizeForLog(_portName.AsSpan())} at {_baudRate} baud.");

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

            byte[] rawBuffer = new byte[4096];
            char[] lineBuffer = new char[1024];
            int linePos = 0;
            int totalLineChars = 0; // Tracks total chars including ignored ones for DoS protection

            // Use BaseStream to read async chunks directly, avoiding Task.Run overhead and single-byte reads.
            System.IO.Stream stream = _serialPort.BaseStream;

            while (!ct.IsCancellationRequested && _serialPort.IsOpen)
            {
                int bytesRead = 0;
                try
                {
                    // Read a chunk of bytes asynchronously
                    bytesRead = await stream.ReadAsync(rawBuffer, 0, rawBuffer.Length, ct);
                    if (bytesRead == 0)
                    {
                        // EOF - Should not happen on open serial port usually, but handle it
                        await Task.Delay(10, ct); // Prevent tight loop if stream is weird
                        continue;
                    }
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from serial port.");
                    await Task.Delay(1000, ct);
                    linePos = 0;
                    totalLineChars = 0;
                    continue;
                }

                for (int i = 0; i < bytesRead; i++)
                {
                    // Simple ASCII decoding.
                    char c = (char)rawBuffer[i];
                    totalLineChars++;

                    // DoS Protection: Check line length limit
                    if (totalLineChars > lineBuffer.Length)
                    {
                        _logger.LogWarning($"Telemetry line exceeded length limit of {lineBuffer.Length}. Resetting.");
                        linePos = 0;
                        totalLineChars = 0;
                        // Delay to prevent flooding
                        await Task.Delay(100, ct);
                        continue;
                    }

                    if (c == '\n')
                    {
                        if (linePos > 0)
                        {
                            var packet = ParseBuffer(lineBuffer, linePos);
                            if (packet != null)
                            {
                                yield return packet.Value;
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to parse telemetry line: '{SecurityHelper.SanitizeForLog(new ReadOnlySpan<char>(lineBuffer, 0, linePos))}'");
                                // DoS Prevention: Delay to prevent log flooding from rapid invalid inputs
                                await Task.Delay(100, ct);
                            }
                        }
                        linePos = 0;
                        totalLineChars = 0;
                    }
                    else if (c == '\r')
                    {
                        // Ignore CR but count towards totalLineChars (already done)
                        continue;
                    }
                    else
                    {
                        lineBuffer[linePos++] = c;
                    }
                }
            }

            if (_serialPort.IsOpen) _serialPort.Close();
        }

        private static TelemetryPacket? ParseBuffer(char[] buffer, int length)
        {
            return TelemetryParser.Parse(new ReadOnlySpan<char>(buffer, 0, length));
        }
    }
}
