using System;
using System.IO;
using System.Text;

namespace AeroCore.Shared.Helpers
{
    public static class BoundedStreamReader
    {
        /// <summary>
        /// Reads a line from the stream with a maximum length limit to prevent DoS.
        /// Throws InvalidOperationException if the line exceeds maxChars.
        /// </summary>
        public static string? ReadSafeLine(Stream stream, int maxChars = 1024)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (!stream.CanRead) throw new ArgumentException("Stream must be readable", nameof(stream));

            var buffer = new StringBuilder();
            int b;

            while (buffer.Length < maxChars)
            {
                try
                {
                    b = stream.ReadByte();
                }
                catch (TimeoutException)
                {
                    // Propagate timeout so caller can handle it (e.g., in serial comms)
                    throw;
                }

                if (b == -1)
                {
                    // End of stream. If we have data, return it.
                    return buffer.Length > 0 ? buffer.ToString() : null;
                }

                char c = (char)b; // Assumes ASCII/single-byte encoding which is standard for telemetry

                if (c == '\n')
                {
                    return buffer.ToString();
                }

                if (c == '\r')
                {
                    continue; // Ignore CR
                }

                buffer.Append(c);
            }

            // If we reached here, the line is too long.
            // We return what we have but logged/marked as truncated?
            // Or throw?
            // Throwing is safer to indicate something is wrong (attack or misconfig).
            throw new InvalidOperationException($"Line length exceeded maximum limit of {maxChars} characters.");
        }
    }
}
