using Xunit;
using AeroCore.Shared.Helpers;

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
    }
}
