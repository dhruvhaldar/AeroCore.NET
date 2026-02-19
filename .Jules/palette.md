## 2026-01-19 - Console UX Matters
**Learning:** Console applications are user interfaces too. Using ANSI colors and formatted text dramatically improves readability for operators monitoring data streams.
**Action:** When working on CLI tools, always look for opportunities to add color coding for status and critical values.

## 2026-01-21 - Explicit Units Reduce Cognitive Load
**Learning:** Adding explicit units (ft, kts, deg) to telemetry data eliminates ambiguity and prevents "mental mapping" errors for operators.
**Action:** Always include units of measurement next to numerical values in data displays.

## 2026-01-24 - Timing of Console Banners
**Learning:** In .NET Generic Host applications, standard startup logs can bury custom UI banners.
**Action:** Output the banner *after* the host has fully started (e.g., in `ExecuteAsync` with a small delay) to ensure it acts as a clear visual separator.

## 2026-05-21 - Visual Analog Indicators in CLI
**Learning:** Rapidly changing numeric data is hard to process. Adding simple ASCII visual indicators (like arrows `^`, `v`, `>`, `<`) helps operators instinctively grasp trends and orientation without reading digits.
**Action:** Supplement dense numeric streams with visual symbols representing direction or magnitude.

## 2026-05-22 - Analog Gauges Provide Magnitude
**Learning:** Simple arrows (`^`/`v`) only show direction. ASCII bar gauges (`[<==|  ]`) provide richer context by showing relative intensity, allowing operators to judge "how much" at a glance.
**Action:** Prefer analog bar gauges over simple directional indicators when screen width permits.

## 2026-05-24 - Velocity Trend Indicators
**Learning:** For critical flight parameters like Velocity, knowing the *rate of change* (acceleration/deceleration) is as important as the value itself. Simple directional arrows (`^`/`v`) provide this instant context.
**Action:** Implement trend indicators for all dynamic telemetry values where directionality matters.

## 2026-05-25 - Self-Documenting Interfaces
**Learning:** Even simple console apps benefit immensely from a "Legend" or "Help" section on startup. It democratizes access to the tool for non-experts.
**Action:** Include a static Legend block in startup banners for all user-facing CLI tools.

## 2026-05-26 - Consistent Critical State Coloring
**Learning:** When displaying data both numerically and visually (e.g., gauges), ensure both representations react to critical thresholds (e.g., turning Red). Inconsistency (Red text vs Green gauge) confuses the operator about the severity.
**Action:** Sync color logic between text and visual indicators for the same metric.

## 2026-05-27 - Tri-State Warning Indicators
**Learning:** Binary states (Safe/Critical) are insufficient for monitoring. Warning states (Yellow) bridge the gap, reducing cognitive shock and allowing proactive operator response.
**Action:** Implement tri-state (Safe/Warning/Critical) logic for all gauge-based indicators where values approach limits.

## 2026-05-28 - Show, Don't Just Tell (In Legends)
**Learning:** Text-based legends describing color codes ("Green/Yel/Red") are abstract. Using the actual colors in the legend itself instantly trains the user's eye and reduces cognitive load.
**Action:** When explaining visual indicators in a CLI, render the explanation using the same visual style (colors, symbols) as the data it describes.

## 2026-05-30 - Actionable Hints in Console Apps
**Learning:** Console applications often trap users who don't know the standard exit commands (like Ctrl+C). Explicitly stating "Press Ctrl+C to exit" reduces anxiety and friction for new users.
**Action:** Always include an exit instruction in the startup banner or footer of long-running console processes.

## 2026-06-03 - Status Summaries for Rapid Scanning
**Learning:** Dense telemetry streams with color coding are good, but require scanning multiple values to determine system health. A single, explicit "Status" column (`[OK]`, `[WARN]`, `[CRIT]`) allows operators to instantly filter noise and focus only on problematic frames.
**Action:** Aggregate complex state into a high-level summary indicator at the end of log lines or data rows.

## 2026-06-03 - Contextualize Warning Indicators
**Learning:** A generic "[WARN]" or "[CRIT]" indicator alerts the user but forces them to scan dense data to find the cause. Explicitly listing the contributing factors (e.g., `[WARN] (VEL)`) reduces cognitive load and reaction time.
**Action:** Always append the specific source or reason to high-level status indicators when multiple factors could be the cause.

## 2026-06-15 - Activity Spinners for Stream Liveness
**Learning:** In high-frequency data streams, static timestamps can be hard to parse quickly to confirm liveness. A simple rotating character (spinner) provides instant, subconscious confirmation that the data link is active.
**Action:** Add an ASCII spinner to the start of telemetry log lines.

## 2026-06-18 - Honest Legends
**Learning:** A legend that misrepresents the application's visual language (e.g., wrong colors or shapes) creates confusion and distrust. Ensuring the legend is a pixel-perfect (or character-perfect) representation of the live data builds user confidence.
**Action:** When creating a legend, use the exact same characters and colors as the actual data visualization, including structural elements like brackets.

## 2026-02-19 - Contrast for Scale Markers
**Learning:** Scale markers in ASCII gauges are only useful if they are visible. When printed in the same color as the empty background (DarkGray), they disappear, losing their value as reference points.
**Action:** Always render significant markers (like scale ticks or center lines) in a distinct, higher-contrast color than the empty background fill.
