using Xunit;
using System.Runtime.InteropServices;
using AeroCore.Shared.Helpers;

namespace AeroCore.Tests
{
    public class SecurityHelperTests
    {
        [Fact]
        public void IsValidSerialPortName_ValidatesCorrectly()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.True(SecurityHelper.IsValidSerialPortName("COM1"));
                Assert.True(SecurityHelper.IsValidSerialPortName("COM10"));
                Assert.True(SecurityHelper.IsValidSerialPortName(@"\\.\COM12"));

                Assert.False(SecurityHelper.IsValidSerialPortName("LPT1"));
                Assert.False(SecurityHelper.IsValidSerialPortName("/dev/ttyUSB0"));
            }
            else
            {
                Assert.True(SecurityHelper.IsValidSerialPortName("/dev/ttyUSB0"));
                Assert.True(SecurityHelper.IsValidSerialPortName("/dev/pts/1"));

                Assert.False(SecurityHelper.IsValidSerialPortName("COM1"));
            }

            // Common Invalid
            Assert.False(SecurityHelper.IsValidSerialPortName(null!));
            Assert.False(SecurityHelper.IsValidSerialPortName(""));
            Assert.False(SecurityHelper.IsValidSerialPortName("  "));
            Assert.False(SecurityHelper.IsValidSerialPortName("../etc/passwd"));
            Assert.False(SecurityHelper.IsValidSerialPortName("/dev/../etc/passwd")); // Path traversal inside valid prefix
            Assert.False(SecurityHelper.IsValidSerialPortName("COM1; rm -rf /")); // Injection
        }

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
        public void SanitizeForLog_RemovesOtherControlCharacters()
        {
            // \t is tab, \0 is null, \u001B is escape
            string input = "Data\twith\0control\u001Bchars";
            string sanitized = SecurityHelper.SanitizeForLog(input);

            // Expect each control char to be replaced by _
            // Data_with_control_chars
            Assert.Equal("Data_with_control_chars", sanitized);
        }

        [Fact]
        public void SanitizeForLog_TruncatesLongInput()
        {
            string input = new string('A', 600);
            string sanitized = SecurityHelper.SanitizeForLog(input);

            // 500 chars + "...[TRUNCATED]" (14 chars) = 514
            Assert.Equal(514, sanitized.Length);
            Assert.Contains("...[TRUNCATED]", sanitized);
            Assert.StartsWith(new string('A', 500), sanitized);
        }

        [Fact]
        public void SanitizeForLog_PreservesEmojis()
        {
            // Verify that Surrogates (Cs) are NOT replaced.
            string input = "Hello 🛡️ World 🚀";
            string sanitized = SecurityHelper.SanitizeForLog(input);

            Assert.Equal("Hello 🛡️ World 🚀", sanitized);
        }
    }
}
