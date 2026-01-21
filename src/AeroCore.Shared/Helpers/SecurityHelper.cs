using System;
using System.Text.RegularExpressions;

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
        public static string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Truncate to prevent DoS via massive logs
            string processed = input;
            if (processed.Length > MaxLogLength)
            {
                processed = processed.Substring(0, MaxLogLength) + "...[TRUNCATED]";
            }

            // Replace control characters (Cc) and format characters (Cf) with underscores.
            // We avoid \p{C} because it includes Surrogates (Cs), which would break Emojis.
            return Regex.Replace(processed, @"[\p{Cc}\p{Cf}]+", "_");
        }
    }
}
