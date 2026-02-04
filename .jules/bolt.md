## 2026-01-19 - String Allocation in Telemetry Hot Path
**Learning:** The `TelemetryParser.ParseFromCsv` was using `string.Split`, allocating arrays and strings for every packet. In high-frequency telemetry (Serial/Mock), this creates significant GC pressure.
**Action:** Use `ReadOnlySpan<char>` and `double.Parse(span)` for all future text-based protocol parsers.

## 2026-01-21 - Logging Allocations in Hot Paths
**Learning:** Standard logging with string interpolation (e.g., `LogInformation($"...")`) allocates strings and boxes value types on every call. In high-frequency loops (like `ProcessLoopAsync`), this creates excessive GC pressure.
**Action:** Use `LoggerMessage.Define` to create cached delegates for high-frequency log messages.

## 2026-01-23 - Struct Conversion for DTOs
**Learning:** Converting `TelemetryPacket` (hot path DTO) from `record` to `readonly record struct` eliminated heap allocations per packet but required updating consumers to handle `Nullable<TelemetryPacket>` (using `.Value`). Attempts to convert `ControlCommand` failed because default struct initialization (`default(T)`) bypasses property initializers, leaving non-nullable reference types (strings) as `null`.
**Action:** Convert DTOs to `readonly record struct` ONLY if they are small, immutable, used in hot paths, and robust against zero-initialization (or contain no reference types).

## 2026-01-24 - Zero-Allocation Stream Reading
**Learning:** `BoundedStreamReader.ReadSafeLine` returned a `string`, causing allocation for every telemetry line. `Span<char>` could not be used directly in the `async` `StreamTelemetryAsync` method due to CS9202 (ref structs in async state machines).
**Action:** Implemented `ReadSafeLine` overload accepting a `Span<char>` buffer. Extracted the span parsing logic into a synchronous helper method `ParseBuffer` to bypass the async restriction.

## 2026-01-26 - Console Output Allocations
**Learning:** Console.Write with string interpolation allocates strings for every call. In high-frequency UI loops (like Ground Station), this creates significant GC pressure.
**Action:** Use `stackalloc char[]`, `TryFormat`, and `Console.Out.Write(ReadOnlySpan<char>)` for zero-allocation console output.

## 2026-01-27 - Serial Telemetry Delegate Overhead
**Learning:** Usage of `BoundedStreamReader.ReadSafeLine` with a lambda `() => _serialPort.ReadChar()` inside the high-frequency telemetry loop created a new delegate and closure allocation (or at least invocation overhead) for every single character read.
**Action:** Implemented a specialized `ReadSafeLine(SerialPort, Span<char>)` overload to bypass the delegate and call `SerialPort.ReadChar()` directly.
