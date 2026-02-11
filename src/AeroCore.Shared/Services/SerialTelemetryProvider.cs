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

            // Use BaseStream to read async chunks directly, avoiding Task.Run overhead and single-byte reads.
            System.IO.Stream stream = _serialPort.BaseStream;

            await foreach (var packet in ProcessStreamAsync(stream, ct, () => _serialPort.IsOpen))
            {
                yield return packet;
            }

            if (_serialPort.IsOpen) _serialPort.Close();
        }

        /// <summary>
        /// Processes a telemetry stream and yields parsed packets.
        /// Public for testing purposes.
        /// </summary>
        public async IAsyncEnumerable<TelemetryPacket> ProcessStreamAsync(
            System.IO.Stream stream,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken ct,
            Func<bool>? isConnected = null)
        {
            byte[] rawBuffer = new byte[4096];
            char[] lineBuffer = new char[1024];
            int linePos = 0;
            int totalLineBytes = 0;
            // Reused list to avoid allocation per read
            List<TelemetryPacket> packets = new List<TelemetryPacket>();

            while (!ct.IsCancellationRequested && (isConnected?.Invoke() ?? true))
            {
                int bytesRead = 0;
                try
                {
                    bytesRead = await stream.ReadAsync(rawBuffer, 0, rawBuffer.Length, ct);
                    if (bytesRead == 0)
                    {
                        await Task.Delay(10, ct);
                        continue;
                    }
                }
                catch (TaskCanceledException)
                {
                    yield break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error reading from stream.");
                    await Task.Delay(1000, ct);
                    linePos = 0;
                    continue;
                }

                // Optimization: Use Span-based processing in a separate method to avoid "ref struct in async" error.
                // We process the buffer in chunks. If a delay is required (due to error/overflow),
                // we pause and then resume processing the rest of the buffer.
                int bufferOffset = 0;

                while (bufferOffset < bytesRead)
                {
                    int consumed = 0;
                    bool requiresDelay = false;
                    packets.Clear();

                    ProcessChunk(
                        rawBuffer.AsSpan(bufferOffset, bytesRead - bufferOffset),
                        lineBuffer,
                        ref linePos,
                        ref totalLineBytes,
                        packets,
                        out consumed,
                        out requiresDelay);

                    bufferOffset += consumed;

                    foreach (var packet in packets)
                    {
                        yield return packet;
                    }

                    if (requiresDelay)
                    {
                        await Task.Delay(100, ct);
                    }
                }
            }
        }

        private void ProcessChunk(
            ReadOnlySpan<byte> bufferSpan,
            char[] lineBuffer,
            ref int linePos,
            ref int totalLineBytes,
            List<TelemetryPacket> packets,
            out int consumedBytes,
            out bool requiresDelay)
        {
            consumedBytes = 0;
            requiresDelay = false;

            while (!bufferSpan.IsEmpty)
            {
                // Find first occurrence of either \r or \n
                int idx = bufferSpan.IndexOfAny((byte)'\r', (byte)'\n');

                if (idx == -1)
                {
                    // No newline or CR found in the remaining buffer.
                    // Copy everything to lineBuffer if it fits.
                    int lengthToCopy = bufferSpan.Length;

                    // DoS Protection: Check total bytes consumed for this line, not just output buffer length.
                    if (totalLineBytes + lengthToCopy > lineBuffer.Length)
                    {
                        _logger.LogWarning($"Telemetry line exceeded length limit of {lineBuffer.Length}. Resetting.");
                        linePos = 0;
                        totalLineBytes = 0;
                        requiresDelay = true;
                        consumedBytes += bufferSpan.Length; // Consume/Drop the rest
                        return;
                    }

                    System.Text.Encoding.Latin1.GetChars(bufferSpan, lineBuffer.AsSpan(linePos));
                    linePos += lengthToCopy;
                    totalLineBytes += lengthToCopy;
                    consumedBytes += bufferSpan.Length;
                    break; // Need more data
                }
                else
                {
                    // Found a delimiter at idx.
                    int lengthToCopy = idx;

                    // DoS Protection: Check total bytes consumed for this line (including this chunk and delimiter).
                    // idx + 1 includes the delimiter.
                    if (totalLineBytes + idx + 1 > lineBuffer.Length)
                    {
                        _logger.LogWarning($"Telemetry line exceeded length limit of {lineBuffer.Length}. Resetting.");
                        linePos = 0;
                        totalLineBytes = 0;
                        requiresDelay = true;
                        consumedBytes += idx + 1; // Consume up to and including delimiter
                        return; // Return to allow delay
                    }

                    // Copy valid part
                    System.Text.Encoding.Latin1.GetChars(bufferSpan.Slice(0, lengthToCopy), lineBuffer.AsSpan(linePos));
                    linePos += lengthToCopy;

                    byte delimiter = bufferSpan[idx];

                    // Advance buffer past the delimiter
                    bufferSpan = bufferSpan.Slice(idx + 1);
                    consumedBytes += idx + 1;
                    totalLineBytes += idx + 1;

                    if (delimiter == (byte)'\n')
                    {
                        // End of line. Process it.
                        if (linePos > 0)
                        {
                            var packet = ParseBuffer(lineBuffer, linePos);
                            if (packet != null)
                            {
                                packets.Add(packet.Value);
                            }
                            else
                            {
                                _logger.LogWarning($"Failed to parse telemetry line: '{SecurityHelper.SanitizeForLog(new ReadOnlySpan<char>(lineBuffer, 0, linePos))}'");
                                requiresDelay = true;
                                linePos = 0;
                                totalLineBytes = 0;
                                return; // Return to allow delay
                            }
                        }
                        linePos = 0;
                        totalLineBytes = 0;
                    }
                    // If delimiter is \r, we just skipped it, loop continues.
                }
            }
        }

        private static TelemetryPacket? ParseBuffer(char[] buffer, int length)
        {
            return TelemetryParser.Parse(new ReadOnlySpan<char>(buffer, 0, length));
        }
    }
}
