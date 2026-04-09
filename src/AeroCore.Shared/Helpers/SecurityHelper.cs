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

            // Optimization: The strict allowlist below inherently rejects all control characters.
            // Removed the redundant O(N) loop that explicitly checked char.IsControl(c).

            // Strict allowlist for characters: ASCII A-Z, a-z, 0-9, ., _, -, /, \
            // This prevents injection characters and Unicode homoglyph/normalization issues.
            foreach (char c in portName)
            {
                bool isAsciiLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                bool isAsciiDigit = (c >= '0' && c <= '9');
                if (!isAsciiLetter && !isAsciiDigit && c != '.' && c != '_' && c != '-' && c != '/' && c != '\\')
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
                    return portName.Length > 3 && IsDigitsOnly(portName.AsSpan(3));
                }
                if (portName.StartsWith(@"\\.\COM", StringComparison.OrdinalIgnoreCase))
                {
                    return portName.Length > 7 && IsDigitsOnly(portName.AsSpan(7));
                }
                return false;
            }
            else
            {
                // Linux/Mac: Strict allowlist of prefixes to prevent arbitrary file read (e.g. /dev/mem, /dev/sda).
                // Allowed: /dev/tty*, /dev/cu*, /dev/serial*, /dev/pts*, /dev/rfcomm*
                // Also require the prefix to be followed by alphanumeric characters or dots/hyphens,
                // and explicitly reject exactly /dev/tty etc. if there's no suffix.
                int prefixLen = 0;
                if (portName.StartsWith("/dev/tty")) prefixLen = 8;
                else if (portName.StartsWith("/dev/cu.")) prefixLen = 8;
                else if (portName.StartsWith("/dev/serial/")) prefixLen = 12;
                else if (portName.StartsWith("/dev/pts/")) prefixLen = 9;
                else if (portName.StartsWith("/dev/rfcomm")) prefixLen = 11;

                if (prefixLen == 0) return false;
                if (portName.Length <= prefixLen) return false;

                // Security: Strictly allowlist suffix characters to alphanumeric, ., _, - to prevent arbitrary file access.
                // Note: / and \ are rejected in the suffix to prevent path traversal inside the valid prefix.
                for (int i = prefixLen; i < portName.Length; i++)
                {
                    char c = portName[i];
                    bool isAsciiLetter = (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
                    bool isAsciiDigit = (c >= '0' && c <= '9');
                    if (!isAsciiLetter && !isAsciiDigit && c != '.' && c != '_' && c != '-')
                    {
                        return false;
                    }
                }

                return true;
            }
        }

        private static bool IsDigitsOnly(ReadOnlySpan<char> span)
        {
            if (span.IsEmpty) return false;
            foreach (char c in span)
            {
                if (c < '0' || c > '9') return false;
            }
            return true;
        }

        public static string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            if (input.Length > MaxLogLength) return SanitizeForLog(input.AsSpan());

            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                if (c < 128)
                {
                    if (c < 32 || c == 127) return SanitizeForLog(input.AsSpan());
                }
                else
                {
                    var category = char.GetUnicodeCategory(c);
                    if (category == UnicodeCategory.Control ||
                        category == UnicodeCategory.Format ||
                        category == UnicodeCategory.LineSeparator ||
                        category == UnicodeCategory.ParagraphSeparator)
                    {
                        return SanitizeForLog(input.AsSpan());
                    }
                }
            }
            return input;
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
                bool isControlOrFormat = false;

                // Optimization: Avoid char.GetUnicodeCategory() for ASCII characters.
                if (c < 128)
                {
                    // Fast path for ASCII: 0-31 and 127 are control characters.
                    isControlOrFormat = (c < 32 || c == 127);
                }
                else
                {
                    var category = char.GetUnicodeCategory(c);

                    // Replace control characters (Cc) and format characters (Cf) with underscores.
                    // We also strip LineSeparator (Zl) and ParagraphSeparator (Zp) to prevent Log Forging.
                    // We avoid \p{C} because it includes Surrogates (Cs), which would break Emojis.
                    isControlOrFormat = (category == UnicodeCategory.Control ||
                                         category == UnicodeCategory.Format ||
                                         category == UnicodeCategory.LineSeparator ||
                                         category == UnicodeCategory.ParagraphSeparator);
                }

                if (isControlOrFormat)
                {
                    buffer[bufferPos++] = '_';

                    // Collapse consecutive control/format characters
                    while (i + 1 < lengthToProcess)
                    {
                        char nextC = input[i + 1];
                        bool nextIsControlOrFormat = false;

                        if (nextC < 128)
                        {
                            nextIsControlOrFormat = (nextC < 32 || nextC == 127);
                        }
                        else
                        {
                            var nextCategory = char.GetUnicodeCategory(nextC);
                            nextIsControlOrFormat = (nextCategory == UnicodeCategory.Control ||
                                                     nextCategory == UnicodeCategory.Format ||
                                                     nextCategory == UnicodeCategory.LineSeparator ||
                                                     nextCategory == UnicodeCategory.ParagraphSeparator);
                        }

                        if (nextIsControlOrFormat)
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
