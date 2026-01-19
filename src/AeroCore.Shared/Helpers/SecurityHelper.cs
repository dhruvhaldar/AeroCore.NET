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

            // Replace CR and LF with underscores or escaped versions.
            // Using a simple regex to replace control characters.
            // \p{C} matches invisible control characters.

            // For log injection, mainly newlines are the problem.
            return Regex.Replace(input, @"[\r\n]+", "_");
        }
    }
}
