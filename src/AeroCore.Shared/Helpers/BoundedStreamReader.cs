using System;
using System.Text;
using System.IO;

namespace AeroCore.Shared.Helpers
{
    public static class BoundedStreamReader
    {
        /// <summary>
        /// Reads a line of characters using the provided readChar function.
        /// Throws InvalidDataException if the line length exceeds maxLength.
        /// </summary>
        /// <param name="readChar">Function that returns the next character as an integer, or -1 if EOF.</param>
        /// <param name="maxLength">Maximum number of characters allowed in the line.</param>
        /// <returns>The line string, or null if EOF is reached before any characters.</returns>
        public static string? ReadSafeLine(Func<int> readChar, int maxLength)
        {
            if (readChar == null) throw new ArgumentNullException(nameof(readChar));

            StringBuilder sb = new StringBuilder();

            while (true)
            {
                // Check limit before reading next char to be safe
                if (sb.Length >= maxLength)
                {
                    throw new InvalidDataException($"Input line exceeded maximum length of {maxLength} characters.");
                }

                int cVal = readChar();

                if (cVal == -1)
                {
                    // EOF
                    return sb.Length > 0 ? sb.ToString() : null;
                }

                char c = (char)cVal;

                if (c == '\n')
                {
                    return sb.ToString();
                }
                else if (c == '\r')
                {
                    // Ignore carriage return, assuming standard CRLF or LF line endings
                    continue;
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        /// <summary>
        /// Reads a line of characters into the provided buffer using the provided readChar function.
        /// Throws InvalidDataException if the line length exceeds the buffer length.
        /// </summary>
        /// <param name="readChar">Function that returns the next character as an integer, or -1 if EOF.</param>
        /// <param name="buffer">Buffer to write characters into.</param>
        /// <returns>The number of characters read, or -1 if EOF is reached before any characters.</returns>
        public static int ReadSafeLine(Func<int> readChar, Span<char> buffer)
        {
            if (readChar == null) throw new ArgumentNullException(nameof(readChar));

            int pos = 0;
            int maxLength = buffer.Length;

            while (true)
            {
                // Check limit before reading next char to be safe
                if (pos >= maxLength)
                {
                    throw new InvalidDataException($"Input line exceeded maximum length of {maxLength} characters.");
                }

                int cVal = readChar();

                if (cVal == -1)
                {
                    // EOF
                    return pos > 0 ? pos : -1;
                }

                char c = (char)cVal;

                if (c == '\n')
                {
                    return pos;
                }
                else if (c == '\r')
                {
                    // Ignore carriage return, assuming standard CRLF or LF line endings
                    continue;
                }
                else
                {
                    buffer[pos++] = c;
                }
            }
        }
    }
}
