using System;
using System.Globalization;
using System.Runtime.InteropServices;

namespace AeroCore.Shared.Helpers
{
    public static class SecurityHelper
    {
        private const int MaxLogLength = 500;

        /// <summary>
        /// Sanitizes input strings for safe logging by removing or replacing control characters
        /// that could be used for log injection / forging.
        /// Truncates input to 500 characters to prevent log flooding.
        /// </summary>
        public static bool IsValidSerialPortName(string portName)
        {
            if (string.IsNullOrWhiteSpace(portName)) return false;

            // Security: Prevent CPU/Memory exhaustion DoS via excessively long input
            if (portName.Length > 100) return false;

            // Check for path traversal attempts
            if (portName.Contains("..")) return false;

            // Check for control characters
            foreach (char c in portName)
            {
                if (char.IsControl(c)) return false;
            }

            // Strict allowlist for characters: A-Z, a-z, 0-9, ., _, -, /, \
            // This prevents injection characters like ';', '&', '|', '>', '<', ' '
            foreach (char c in portName)
            {
                if (!char.IsLetterOrDigit(c) && c != '.' && c != '_' && c != '-' && c != '/' && c != '\\')
                {
                    return false;
                }
            }

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Strict check: Must start with COM or \\.\COM followed immediately by digits.
                // This prevents opening files like "Common/foo.txt" or "Command.log".
                if (portName.StartsWith("COM", StringComparison.OrdinalIgnoreCase))
                {
                    return IsDigitsOnly(portName.AsSpan(3));
                }
                if (portName.StartsWith(@"\\.\COM", StringComparison.OrdinalIgnoreCase))
                {
                    return IsDigitsOnly(portName.AsSpan(7));
                }
                return false;
            }
            else
            {
                // Linux/Mac: Strict allowlist of prefixes to prevent arbitrary file read (e.g. /dev/mem, /dev/sda).
                // Allowed: /dev/tty*, /dev/cu*, /dev/serial*, /dev/pts*, /dev/rfcomm*
                return portName.StartsWith("/dev/tty") ||
                       portName.StartsWith("/dev/cu.") ||
                       portName.StartsWith("/dev/serial/") ||
                       portName.StartsWith("/dev/pts/") ||
                       portName.StartsWith("/dev/rfcomm");
            }
        }

        private static bool IsDigitsOnly(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return false;
            foreach (char c in span)
            {
                if (!char.IsDigit(c)) return false;
            }
            return true;
        }

        public static string SanitizeForLog(ReadOnlySpan<char> input)
        {
            if (input.IsEmpty)
            {
                return string.Empty;
            }

            const string Suffix = "...[TRUNCATED]";

            // Stackalloc buffer for zero-allocation processing (before final string creation)
            // Size: MaxLogLength + Suffix length. ~1KB on stack.
            Span<char> buffer = stackalloc char[MaxLogLength + Suffix.Length];
            int bufferPos = 0;

            bool truncated = false;
            int lengthToProcess = input.Length;
            if (lengthToProcess > MaxLogLength)
            {
                lengthToProcess = MaxLogLength;
                truncated = true;
            }

            for (int i = 0; i < lengthToProcess; i++)
            {
                char c = input[i];
                var category = char.GetUnicodeCategory(c);

                // Replace control characters (Cc) and format characters (Cf) with underscores.
                // We also strip LineSeparator (Zl) and ParagraphSeparator (Zp) to prevent Log Forging.
                // We avoid \p{C} because it includes Surrogates (Cs), which would break Emojis.
                if (category == UnicodeCategory.Control ||
                    category == UnicodeCategory.Format ||
                    category == UnicodeCategory.LineSeparator ||
                    category == UnicodeCategory.ParagraphSeparator)
                {
                    buffer[bufferPos++] = '_';

                    // Collapse consecutive control/format characters
                    while (i + 1 < lengthToProcess)
                    {
                        char nextC = input[i + 1];
                        var nextCategory = char.GetUnicodeCategory(nextC);
                        if (nextCategory == UnicodeCategory.Control ||
                            nextCategory == UnicodeCategory.Format ||
                            nextCategory == UnicodeCategory.LineSeparator ||
                            nextCategory == UnicodeCategory.ParagraphSeparator)
                        {
                            i++;
                        }
                        else
                        {
                            break;
                        }
                    }
                }
                else
                {
                    buffer[bufferPos++] = c;
                }
            }

            if (truncated)
            {
                ReadOnlySpan<char> suffixSpan = Suffix.AsSpan();
                suffixSpan.CopyTo(buffer.Slice(bufferPos));
                bufferPos += suffixSpan.Length;
            }

            return buffer.Slice(0, bufferPos).ToString();
        }
    }
}
