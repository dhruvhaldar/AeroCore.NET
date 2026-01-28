using System;
using System.IO;
using System.Collections.Generic;
using Xunit;
using AeroCore.Shared.Helpers;

namespace AeroCore.Tests
{
    public class BoundedStreamReaderTests
    {
        [Fact]
        public void ReadSafeLine_ReturnsLine_WhenShortEnough()
        {
            string input = "Hello World\n";
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            // Reader returns -1 when queue empty
            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;

            string? result = BoundedStreamReader.ReadSafeLine(reader, 100);
            Assert.Equal("Hello World", result);
        }

        [Fact]
        public void ReadSafeLine_Throws_WhenTooLong()
        {
            // Input: "ABCDEFG\n" (8 chars), Limit: 5
            string input = "ABCDEFG\n";
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;

            Assert.Throws<InvalidDataException>(() =>
                BoundedStreamReader.ReadSafeLine(reader, 5)
            );
        }

        [Fact]
        public void ReadSafeLine_HandlesEOF()
        {
            string input = "PartialLine"; // No newline
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;

            string? result = BoundedStreamReader.ReadSafeLine(reader, 100);
            Assert.Equal("PartialLine", result);
        }

        [Fact]
        public void ReadSafeLine_IgnoresCR()
        {
            string input = "Line\r\n";
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;

            string? result = BoundedStreamReader.ReadSafeLine(reader, 100);
            Assert.Equal("Line", result);
        }

        [Fact]
        public void ReadSafeLine_ChecksLimit_BeforeReading()
        {
            // If we are at limit, we should not read next char, just throw.
            // Or we read char then throw?
            // My implementation: checks `sb.Length >= maxLength` at start of loop.

            Func<int> reader = () => 'A'; // Infinite stream of 'A'

            // Max length 5.
            // Loop 0: len=0 < 5. Read 'A'. Append. len=1.
            // ...
            // Loop 4: len=4 < 5. Read 'A'. Append. len=5.
            // Loop 5: len=5 >= 5. Throw.

            Assert.Throws<InvalidDataException>(() =>
               BoundedStreamReader.ReadSafeLine(reader, 5)
            );
        }

        [Fact]
        public void ReadSafeLine_Span_ReturnsCharsRead_WhenShortEnough()
        {
            string input = "Hello World\n";
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;
            char[] buffer = new char[100];

            int charsRead = BoundedStreamReader.ReadSafeLine(reader, buffer);
            Assert.Equal(11, charsRead); // "Hello World".Length is 11.
            Assert.Equal("Hello World", new string(buffer, 0, charsRead));
        }

        [Fact]
        public void ReadSafeLine_Span_Throws_WhenTooLong()
        {
            // Input: "ABCDEFG\n" (8 chars), Buffer: 5
            string input = "ABCDEFG\n";
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;
            char[] buffer = new char[5];

            Assert.Throws<InvalidDataException>(() =>
                BoundedStreamReader.ReadSafeLine(reader, buffer)
            );
        }

        [Fact]
        public void ReadSafeLine_Span_HandlesEOF()
        {
            string input = "PartialLine"; // No newline
            var queue = new Queue<int>();
            foreach (char c in input) queue.Enqueue(c);

            Func<int> reader = () => queue.Count > 0 ? queue.Dequeue() : -1;
            char[] buffer = new char[100];

            int charsRead = BoundedStreamReader.ReadSafeLine(reader, buffer);
            Assert.Equal(11, charsRead);
            Assert.Equal("PartialLine", new string(buffer, 0, charsRead));
        }

        [Fact]
        public void ReadSafeLine_Span_ReturnsMinusOne_OnImmediateEOF()
        {
            Func<int> reader = () => -1;
            char[] buffer = new char[100];

            int charsRead = BoundedStreamReader.ReadSafeLine(reader, buffer);
            Assert.Equal(-1, charsRead);
        }
    }
}
