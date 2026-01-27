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
