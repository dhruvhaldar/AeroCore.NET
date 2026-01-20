using System;
using System.Text.RegularExpressions;

namespace AeroCore.Shared.Helpers
{
    public static class SecurityHelper
    {
        /// <summary>
        /// Sanitizes input strings for safe logging by removing or replacing control characters
        /// that could be used for log injection / forging.
        /// </summary>
        public static string SanitizeForLog(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            // Defense in Depth: Truncate long inputs to prevent DoS (disk exhaustion/memory)
            const int MaxLogLength = 500;
            if (input.Length > MaxLogLength)
            {
                input = input.Substring(0, MaxLogLength);
            }

            // Replace all control characters (including newlines, tabs, bells, etc.) with underscore
            // \p{C} matches invisible control characters.
            return Regex.Replace(input, @"\p{C}+", "_");
        }
    }
}
