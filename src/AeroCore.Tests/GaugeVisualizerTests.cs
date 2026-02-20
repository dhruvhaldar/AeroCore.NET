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
            int fill = GaugeVisualizer.Fill(buffer, 0, 100);

            Assert.Equal(0, fill);
            Assert.Equal("   + | +   ", buffer.ToString());
        }

        [Fact]
        public void Fill_PositiveMax_ShowsRightArrow()
        {
            Span<char> buffer = stackalloc char[11];
            int fill = GaugeVisualizer.Fill(buffer, 100, 100);

            Assert.Equal(5, fill);
            Assert.Equal("   + |=+==>", buffer.ToString());
        }

        [Fact]
        public void Fill_NegativeMax_ShowsLeftArrow()
        {
            Span<char> buffer = stackalloc char[11];
            int fill = GaugeVisualizer.Fill(buffer, -100, 100);

            Assert.Equal(5, fill);
            Assert.Equal("<==+=| +   ", buffer.ToString());
        }

        [Fact]
        public void Fill_HalfPositive_ShowsHalfBar()
        {
            Span<char> buffer = stackalloc char[11];
            int fill = GaugeVisualizer.Fill(buffer, 50, 100);

            // 0.5 * 5 = 2.5 -> Round to Even -> 2
            Assert.Equal(2, fill);
            Assert.Equal("   + |=>   ", buffer.ToString());
        }

        [Fact]
        public void Fill_SmallBuffer_HandlesBounds()
        {
            Span<char> buffer = stackalloc char[3];
            int fill = GaugeVisualizer.Fill(buffer, 100, 100);

            // width 3, center 1. fill = 1.
            Assert.Equal(1, fill);
            Assert.Equal(" |>", buffer.ToString());
        }

        [Fact]
        public void Fill_EvenWidth_HandlesBounds()
        {
            Span<char> buffer = stackalloc char[4];
            int fill = GaugeVisualizer.Fill(buffer, 100, 100);

            // width 4, center 2. fill = 2.
            Assert.Equal(2, fill);
            Assert.Equal(" +|=", buffer.ToString());
        }
    }
}
