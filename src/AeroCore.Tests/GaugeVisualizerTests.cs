using System;
using AeroCore.Shared.Helpers;
using Xunit;

namespace AeroCore.Tests
{
    public class GaugeVisualizerTests
    {
        [Fact]
        public void Fill_ZeroValue_ShowsCenter()
        {
            Span<char> buffer = stackalloc char[11];
            GaugeVisualizer.Fill(buffer, 0, 100);

            // Expected: "     |     "
            Assert.Equal("     |     ", buffer.ToString());
        }

        [Fact]
        public void Fill_PositiveMax_ShowsRightArrow()
        {
            Span<char> buffer = stackalloc char[11];
            GaugeVisualizer.Fill(buffer, 100, 100);

            // Expected: "     |====>"
            Assert.Equal("     |====>", buffer.ToString());
        }

        [Fact]
        public void Fill_NegativeMax_ShowsLeftArrow()
        {
            Span<char> buffer = stackalloc char[11];
            GaugeVisualizer.Fill(buffer, -100, 100);

            // Expected: "<====|     "
            Assert.Equal("<====|     ", buffer.ToString());
        }

        [Fact]
        public void Fill_HalfPositive_ShowsHalfBar()
        {
            Span<char> buffer = stackalloc char[11];
            GaugeVisualizer.Fill(buffer, 50, 100);

            // 0.5 * 5 = 2.5 -> Round to Even -> 2
            // fill = 2.
            // i=1: '=', i=2: '='. buffer[center+2] = '>' (Overwrites)
            // Result: "     |=>   "
            Assert.Equal("     |=>   ", buffer.ToString());
        }

        [Fact]
        public void Fill_SmallBuffer_HandlesBounds()
        {
            Span<char> buffer = stackalloc char[3];
            // Center index 1.
            // Fill = 1 * 1 = 1.
            // Center + 1 = 2.
            GaugeVisualizer.Fill(buffer, 100, 100);
            // Expected: " |>"
            Assert.Equal(" |>", buffer.ToString());
        }

        [Fact]
        public void Fill_EvenWidth_HandlesBounds()
        {
            Span<char> buffer = stackalloc char[4];
            // Center index 2.
            // Range 100, Value 100. Normalized 1.
            // Fill = 1 * 2 = 2.
            // Center + 2 = 4 (Out of bounds).
            // Code checks bounds: if (center + fill < width)
            // So it won't write '>', but it might write '='.
            // i=1: center+1=3. buffer[3]='='.
            // i=2: center+2=4. Out of bounds.
            // Result: "  |="
            GaugeVisualizer.Fill(buffer, 100, 100);

            Assert.Equal("  |=", buffer.ToString());
        }
    }
}
