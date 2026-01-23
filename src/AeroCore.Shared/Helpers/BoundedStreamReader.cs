using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace AeroCore.Shared.Helpers
{
    public static class BoundedStreamReader
    {
        /// <summary>
        /// Reads a line from the stream asynchronously with a maximum length limit.
        /// This prevents memory exhaustion (DoS) from unbounded lines.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="maxLength">The maximum number of characters to read.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>The line read, or null if end of stream reached.</returns>
        public static async Task<string?> ReadSafeLineAsync(Stream stream, int maxLength, CancellationToken ct)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (maxLength <= 0) throw new ArgumentOutOfRangeException(nameof(maxLength));

            var sb = new StringBuilder();
            byte[] buffer = new byte[1];

            while (sb.Length < maxLength && !ct.IsCancellationRequested)
            {
                int bytesRead;
                try
                {
                    bytesRead = await stream.ReadAsync(buffer, 0, 1, ct);
                }
                catch
                {
                    // If reading fails, we return what we have or null if empty
                    if (sb.Length > 0) return sb.ToString();
                    return null;
                }

                if (bytesRead == 0)
                {
                    // End of stream
                    return sb.Length > 0 ? sb.ToString() : null;
                }

                char c = (char)buffer[0];

                // Check for newline
                if (c == '\n')
                {
                    return sb.ToString();
                }

                // Append if not CR (we treat \r\n as just \n for line ending purposes, stripping \r)
                if (c != '\r')
                {
                    sb.Append(c);
                }
            }

            // If we hit the limit, return what we have so far.
            // The next read will continue from where we left off, effectively splitting the long line.
            return sb.ToString();
        }
    }
}
