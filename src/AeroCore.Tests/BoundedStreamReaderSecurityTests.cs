using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;
using AeroCore.Shared.Helpers;

namespace AeroCore.Tests
{
    public class BoundedStreamReaderSecurityTests
    {
        [Fact]
        public void ReadSafeLine_DoS_InfiniteCR_ThrowsInvalidDataException()
        {
            // Simulate an infinite stream of '\r' characters
            Func<int> infiniteCrReader = () => '\r';

            // Should throw InvalidDataException immediately (after 10 chars)
            Assert.Throws<InvalidDataException>(() =>
                BoundedStreamReader.ReadSafeLine(infiniteCrReader, 10)
            );
        }

        [Fact]
        public void ReadSafeLine_Span_DoS_InfiniteCR_ThrowsInvalidDataException()
        {
            // Simulate an infinite stream of '\r' characters
            Func<int> infiniteCrReader = () => '\r';
            char[] buffer = new char[10];

            // Should throw InvalidDataException immediately
            Assert.Throws<InvalidDataException>(() =>
                BoundedStreamReader.ReadSafeLine(infiniteCrReader, buffer)
            );
        }
    }
}
