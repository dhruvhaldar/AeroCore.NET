## 2026-01-19 - String Allocation in Telemetry Hot Path
**Learning:** The `TelemetryParser.ParseFromCsv` was using `string.Split`, allocating arrays and strings for every packet. In high-frequency telemetry (Serial/Mock), this creates significant GC pressure.
**Action:** Use `ReadOnlySpan<char>` and `double.Parse(span)` for all future text-based protocol parsers.
