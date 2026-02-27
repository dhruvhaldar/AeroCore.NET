using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using AeroCore.Shared.Interfaces;
using AeroCore.Shared.Models;
using AeroCore.Shared.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace AeroCore.Shared.Services
{
    public class SerialTelemetryProvider : ITelemetryProvider
    {
        private static readonly SearchValues<byte> _lineSeparators = SearchValues.Create(new byte[] { (byte)'\r', (byte)'\n' });

        private readonly ILogger<SerialTelemetryProvider> _logger;
        private readonly IConfiguration _config;
        private SerialPort? _serialPort;
        private string _portName = string.Empty;
        private int _baudRate;

        // Rate limiter for error logs to prevent DoS via log flooding
        private DateTime _lastErrorLog = DateTime.MinValue;

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
                // Security: Validate port name to prevent path traversal and command injection risks.
                if (!SecurityHelper.IsValidSerialPortName(_portName))
                {
                    // We throw here to be caught by the block below, ensuring we don't proceed with invalid config.
                    // Security: Sanitize the invalid input before logging it in the exception to prevent Log Injection.
                    string safePortName = SecurityHelper.SanitizeForLog(_portName.AsSpan());
                    throw new ArgumentException($"Invalid serial port name format: {safePortName}");
                }

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
            byte[] lineBuffer = new byte[1024];
            int linePos = 0;
            int totalLineBytes = 0;
            bool isDiscarding = false;

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

                // Optimization: Capture timestamp once per chunk read to avoid repetitive DateTime.UtcNow calls (syscalls).
                // This reduces CPU overhead significantly in high-frequency loops.
                DateTime chunkTimestamp = DateTime.UtcNow;

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
                        ref isDiscarding,
                        chunkTimestamp,
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
            byte[] lineBuffer,
            ref int linePos,
            ref int totalLineBytes,
            List<TelemetryPacket> packets,
            ref bool isDiscarding,
            DateTime timestamp,
            out int consumedBytes,
            out bool requiresDelay)
        {
            consumedBytes = 0;
            requiresDelay = false;

            while (!bufferSpan.IsEmpty)
            {
                // Find first occurrence of either \r or \n
                int idx = bufferSpan.IndexOfAny(_lineSeparators);

                if (isDiscarding)
                {
                    if (idx == -1)
                    {
                        // Still inside a discarded line, consume everything and continue
                        consumedBytes += bufferSpan.Length;
                        // Return to fetch next chunk, as we consumed everything available
                        return;
                    }
                    else
                    {
                        // Found the end of the discarded line at idx.
                        // Consume up to and including delimiter.
                        consumedBytes += idx + 1;

                        // Advance past delimiter
                        byte delimiter = bufferSpan[idx];
                        bufferSpan = bufferSpan.Slice(idx + 1);

                        // Handle potential CRLF split even when discarding
                        if (delimiter == (byte)'\r' && !bufferSpan.IsEmpty && bufferSpan[0] == (byte)'\n')
                        {
                            bufferSpan = bufferSpan.Slice(1);
                            consumedBytes++;
                        }

                        // Reset discarding state and prepare for next line
                        isDiscarding = false;
                        linePos = 0;
                        totalLineBytes = 0;

                        // Continue loop to process any remaining data in buffer as new line
                        continue;
                    }
                }

                if (idx == -1)
                {
                    // No newline or CR found in the remaining buffer.
                    // Copy everything to lineBuffer if it fits.
                    int lengthToCopy = bufferSpan.Length;

                    // DoS Protection: Check total bytes consumed for this line, not just output buffer length.
                    if (totalLineBytes + lengthToCopy > lineBuffer.Length)
                    {
                        var now = timestamp; // Optimization: Use pre-captured timestamp to avoid syscall
                        if ((now - _lastErrorLog).TotalMilliseconds >= 1000)
                        {
                            _logger.LogWarning($"Telemetry line exceeded length limit of {lineBuffer.Length}. Resetting.");
                            _lastErrorLog = now;
                        }

                        // Enter discarding state to ignore the rest of this overly long line
                        isDiscarding = true;

                        linePos = 0;
                        totalLineBytes = 0;
                        requiresDelay = true;
                        consumedBytes += bufferSpan.Length; // Consume/Drop the rest
                        return;
                    }

                    bufferSpan.CopyTo(lineBuffer.AsSpan(linePos));
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
                        var now = timestamp; // Optimization: Use pre-captured timestamp to avoid syscall
                        if ((now - _lastErrorLog).TotalMilliseconds >= 1000)
                        {
                            _logger.LogWarning($"Telemetry line exceeded length limit of {lineBuffer.Length}. Resetting.");
                            _lastErrorLog = now;
                        }

                        // We found the end of the line, so we discard only this line.
                        // We do NOT set isDiscarding=true because we are done with the long line.

                        linePos = 0;
                        totalLineBytes = 0;
                        requiresDelay = true;
                        consumedBytes += idx + 1; // Consume up to and including delimiter
                        return; // Return to allow delay
                    }

                    byte delimiter = bufferSpan[idx];

                    // Optimization: Parse directly from bufferSpan if we haven't buffered anything yet (complete line in this chunk).
                    // This avoids copying to lineBuffer and converting to chars.
                    if (linePos == 0)
                    {
                        // Pass the captured timestamp to avoid syscall
                        if (TelemetryParser.TryParse(bufferSpan.Slice(0, idx), out var packet, timestamp))
                        {
                            packets.Add(packet);

                            // Advance past delimiter
                            bufferSpan = bufferSpan.Slice(idx + 1);
                            consumedBytes += idx + 1;
                            totalLineBytes = 0; // Reset as we processed a line

                            // Handle CRLF
                            if (delimiter == (byte)'\r' && !bufferSpan.IsEmpty && bufferSpan[0] == (byte)'\n')
                            {
                                bufferSpan = bufferSpan.Slice(1);
                                consumedBytes++;
                            }

                            continue; // Continue to next line
                        }
                        else
                        {
                            // Optimization: If parsing failed and we have a complete line (ending in \n or \r\n),
                            // we can handle the failure immediately without copying to lineBuffer and re-parsing.
                            bool isTargetDelimiter = (delimiter == (byte)'\n');
                            if (!isTargetDelimiter && delimiter == (byte)'\r')
                            {
                                // Check if followed by \n (CRLF) in the current buffer
                                if (idx + 1 < bufferSpan.Length && bufferSpan[idx + 1] == (byte)'\n')
                                {
                                    isTargetDelimiter = true;
                                }
                            }

                            if (isTargetDelimiter)
                            {
                                // Consume the invalid line and delimiter
                                bufferSpan = bufferSpan.Slice(idx + 1);
                                consumedBytes += idx + 1;
                                totalLineBytes = 0;

                                // Handle CRLF
                                if (delimiter == (byte)'\r' && !bufferSpan.IsEmpty && bufferSpan[0] == (byte)'\n')
                                {
                                    bufferSpan = bufferSpan.Slice(1);
                                    consumedBytes++;
                                }

                                // Security: Do not log raw content to prevent sensitive data leakage and DoS.
                                var now = timestamp; // Optimization: Use pre-captured timestamp to avoid syscall
                                if ((now - _lastErrorLog).TotalMilliseconds >= 1000)
                                {
                                    _logger.LogWarning("Failed to parse telemetry line. (Length: {Length})", idx);
                                    _lastErrorLog = now;
                                }
                                requiresDelay = true;
                                linePos = 0;
                                return; // Return to allow delay
                            }
                        }
                    }

                    // Copy valid part
                    bufferSpan.Slice(0, lengthToCopy).CopyTo(lineBuffer.AsSpan(linePos));
                    linePos += lengthToCopy;

                    // Advance buffer past the delimiter
                    bufferSpan = bufferSpan.Slice(idx + 1);
                    consumedBytes += idx + 1;
                    totalLineBytes += idx + 1;

                    // Optimization: Handle CRLF as a single delimiter to avoid extra loop iteration
                    if (delimiter == (byte)'\r' && !bufferSpan.IsEmpty && bufferSpan[0] == (byte)'\n')
                    {
                        bufferSpan = bufferSpan.Slice(1);
                        consumedBytes++;
                        totalLineBytes++;
                        delimiter = (byte)'\n';
                    }

                    if (delimiter == (byte)'\n')
                    {
                        // End of line. Process it.
                        if (linePos > 0)
                        {
                            if (ParseBuffer(lineBuffer, linePos, timestamp, out var packet))
                            {
                                packets.Add(packet);
                            }
                            else
                            {
                                // Security: Do not log raw content to prevent sensitive data leakage and DoS.
                                var now = timestamp; // Optimization: Use pre-captured timestamp to avoid syscall
                                if ((now - _lastErrorLog).TotalMilliseconds >= 1000)
                                {
                                    _logger.LogWarning("Failed to parse telemetry line. (Length: {Length})", linePos);
                                    _lastErrorLog = now;
                                }
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

        private static bool ParseBuffer(byte[] buffer, int length, DateTime timestamp, out TelemetryPacket packet)
        {
            return TelemetryParser.TryParse(new ReadOnlySpan<byte>(buffer, 0, length), out packet, timestamp);
        }
    }
}
