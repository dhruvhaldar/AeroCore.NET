## 2026-01-19 - String Allocation in Telemetry Hot Path
**Learning:** The `TelemetryParser.ParseFromCsv` was using `string.Split`, allocating arrays and strings for every packet. In high-frequency telemetry (Serial/Mock), this creates significant GC pressure.
**Action:** Use `ReadOnlySpan<char>` and `double.Parse(span)` for all future text-based protocol parsers.

## 2026-01-21 - Logging Allocations in Hot Paths
**Learning:** Standard logging with string interpolation (e.g., `LogInformation($"...")`) allocates strings and boxes value types on every call. In high-frequency loops (like `ProcessLoopAsync`), this creates excessive GC pressure.
**Action:** Use `LoggerMessage.Define` to create cached delegates for high-frequency log messages.
