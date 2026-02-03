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
