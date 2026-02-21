using System;
using System.Runtime.InteropServices;
using Xunit;
using AeroCore.Shared.Helpers;

namespace AeroCore.Tests
{
    public class SecurityHelperEnhancementTests
    {
        [Fact]
        public void IsValidSerialPortName_RejectsExcessivelyLongNames()
        {
            // Security: Prevent DoS via excessive CPU/Memory consumption during validation.
            // A typical serial port name is very short (e.g. "COM3", "/dev/ttyUSB0").
            // Names exceeding 100 characters are almost certainly malicious or misconfigured.

            string longName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // COM + 98 digits = 101 chars
                longName = "COM" + new string('1', 98);
            }
            else
            {
                // /dev/tty + 93 'a's = 101 chars
                // Note: IsValidSerialPortName only checks prefix on Linux, so any suffix is valid as long as chars are allowed.
                longName = "/dev/tty" + new string('a', 93);
            }

            // This should return false due to length restriction (once implemented)
            // Currently it returns true because length is not checked.
            bool isValid = SecurityHelper.IsValidSerialPortName(longName);

            Assert.False(isValid, "SecurityHelper.IsValidSerialPortName should reject names > 100 chars to prevent DoS.");
        }
    }
}
