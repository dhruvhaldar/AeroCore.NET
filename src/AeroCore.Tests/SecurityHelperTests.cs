using Xunit;
using AeroCore.Shared.Helpers;
using System;

namespace AeroCore.Tests
{
    public class SecurityHelperTests
    {
        [Fact]
        public void SanitizeForLog_RemovesNewlines()
        {
            string input = "BadData\n[INFO] Forged Log";
            string sanitized = SecurityHelper.SanitizeForLog(input);

            Assert.DoesNotContain("\n", sanitized);
            Assert.DoesNotContain("\r", sanitized);
            Assert.Equal("BadData_[INFO] Forged Log", sanitized);
        }

        [Fact]
        public void SanitizeForLog_TruncatesLongStrings()
        {
            string input = new string('a', 1000);
            string sanitized = SecurityHelper.SanitizeForLog(input);

            Assert.True(sanitized.Length <= 500, $"Expected length <= 500, but got {sanitized.Length}");
        }

        [Fact]
        public void SanitizeForLog_RemovesAllControlCharacters()
        {
            // \x07 is Bell, \b is Backspace, \t is Tab
            // Note: Use \u001B for Escape to avoid consuming 'C' from "Chars" as hex digit
            string input = "User\x07Input\bWith\tControl\u001BChars";
            string sanitized = SecurityHelper.SanitizeForLog(input);

            // Check specific replacement pattern
            // "User_Input_With_Control_Chars"
            Assert.Equal("User_Input_With_Control_Chars", sanitized);
        }
    }
}
