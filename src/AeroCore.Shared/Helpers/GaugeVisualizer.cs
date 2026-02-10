using System;

namespace AeroCore.Shared.Helpers
{
    public static class GaugeVisualizer
    {
        /// <summary>
        /// Fills the provided span with an ASCII analog gauge visualization.
        /// Zero allocation.
        /// </summary>
        /// <param name="buffer">The buffer to fill. Length determines gauge width.</param>
        /// <param name="value">The current value to visualize.</param>
        /// <param name="range">The full scale range (e.g. 45.0 for +/- 45 degrees).</param>
        public static void Fill(Span<char> buffer, double value, double range)
        {
            int width = buffer.Length;
            if (width == 0) return;

            int center = width / 2;

            // Initialize with spaces
            buffer.Fill(' ');

            // Set scale markers (at ~50% of each side)
            int quarter = center / 2;
            if (quarter > 0)
            {
                if (center - quarter >= 0) buffer[center - quarter] = '.';
                if (center + quarter < width) buffer[center + quarter] = '.';
            }

            // Set center marker
            if (center < width)
            {
                buffer[center] = '|';
            }

            double normalized = Math.Clamp(value / range, -1, 1);
            int fill = (int)Math.Round(Math.Abs(normalized) * center);

            if (fill > 0)
            {
                if (normalized > 0)
                {
                    // Positive direction (Right)
                    for (int i = 1; i <= fill; i++)
                    {
                        if (center + i < width) buffer[center + i] = '=';
                    }
                    if (center + fill < width) buffer[center + fill] = '>';
                }
                else
                {
                    // Negative direction (Left)
                    for (int i = 1; i <= fill; i++)
                    {
                        if (center - i >= 0) buffer[center - i] = '=';
                    }
                    if (center - fill >= 0) buffer[center - fill] = '<';
                }
            }
        }
    }
}
