using System;
using System.Globalization;

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
                // We avoid \p{C} because it includes Surrogates (Cs), which would break Emojis.
                if (category == UnicodeCategory.Control || category == UnicodeCategory.Format)
                {
                    buffer[bufferPos++] = '_';

                    // Collapse consecutive control/format characters
                    while (i + 1 < lengthToProcess)
                    {
                        char nextC = input[i + 1];
                        var nextCategory = char.GetUnicodeCategory(nextC);
                        if (nextCategory == UnicodeCategory.Control || nextCategory == UnicodeCategory.Format)
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
