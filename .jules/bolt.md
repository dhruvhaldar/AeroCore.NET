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

## 2026-01-29 - Async Serial I/O Buffering
**Learning:** Using `Task.Run(() => SerialPort.ReadLine())` or `SerialPort.ReadChar()` in a loop creates excessive thread pool pressure (1 task per line) and syscall overhead. `SerialPort.BaseStream.ReadAsync` with a large byte buffer eliminates this overhead but requires manual line parsing.
**Action:** For high-frequency serial I/O, use `BaseStream.ReadAsync` with a persistent `byte[]` buffer and manual parsing logic, ensuring DoS protection (line length limits) is reimplemented manually.

## 2026-01-30 - Record Validation Bypass with `with` Expression
**Learning:** Creating `ControlCommand` records in a loop (e.g., `new ControlCommand { ... }`) triggers expensive validation logic (loops, char checks) in the `init` accessor for every instance, even for constant values.
**Action:** Use a `static readonly` template instance for common commands and the `with` expression (e.g., `_template with { Timestamp = ... }`) to create copies. This uses the copy constructor, copying the validated backing fields directly and bypassing the `init` property validation.

## 2026-02-02 - Console System Call Overhead
**Learning:** Even when using zero-allocation `Console.Out.Write(Span)`, multiple calls for a single logical field (e.g., padding + value) double the system call overhead and lock contention. In high-frequency loops (like real-time telemetry display), this adds up.
**Action:** Combine logically related output segments (like padding and value) into a single `stackalloc` buffer to perform one `Console.Out.Write` call instead of multiple.

## 2026-05-22 - Optimizing Telemetry Ingestion Loop
**Learning:** In high-frequency serial streams with CRLF line endings, treating `\r` and `\n` as separate delimiters causes two loop iterations per line, doubling the overhead of `IndexOfAny` and `GetChars`.
**Action:** Detect `\r\n` sequence and consume both in a single pass to halve the loop overhead for standard telemetry streams.

## 2026-05-23 - Avoiding SIMD Overhead for Clean Data
**Learning:** `IndexOfAnyExcept` (SIMD) is extremely fast but still has initialization/call overhead. In high-frequency parsing of clean CSV data (no whitespace), calling it repeatedly (e.g. 8 times per packet) is slower than a simple scalar check `if (span[0] > 32)`.
**Action:** Add a fast-path check for the common case (clean data) before invoking SIMD search helpers to avoid unnecessary overhead in hot loops.

## 2026-05-24 - Inlining Hot Path Checks
**Learning:** Even with a fast-path inside a helper method (`TrimWhitespace`), the method call overhead itself becomes measurable in extremely tight loops (parsing millions of fields).
**Action:** Inline simple checks (like `span[0] <= 32` or `span[0] == ','`) into the caller to avoid the method call entirely for the happy path (compact CSV).

## 2025-02-12 - Wall-clock Throttling in Hot Loops (Avoiding DateTime.UtcNow)
**Learning:** Repeated calls to `DateTime.UtcNow` within high-frequency loops (like telemetry processing or UI updates) introduce significant system call overhead. However, replacing it with domain time (`packet.Timestamp`) for system I/O throttling is a critical mistake: during data bursts (e.g., catching up after lag), domain time advances rapidly while wall-clock time barely moves, completely bypassing the throttle and causing massive I/O blocking.
**Action:** To rate-limit or throttle system operations (like Console I/O or network sends) inside a hot loop, use `Environment.TickCount64`. It accurately measures wall-clock elapsed time with near-zero overhead compared to `DateTime.UtcNow`. For operations that only care about causal sequence or batch timings, take a snapshot of `DateTime.UtcNow` exactly once at the beginning of the batch.

## 2026-05-25 - StringBuilder Allocation in High-Frequency UI Loops
**Learning:** `StringBuilder` instantiation within high-frequency UI update loops (like the ~20Hz Ground Station terminal loop) creates excessive and unnecessary allocations for short, predictable strings.
**Action:** Use `stackalloc char[]` or `Span<char>` with direct character indexing/copying when building small, deterministic strings in hot loops to eliminate heap allocations and reduce GC pressure.
