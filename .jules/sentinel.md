## 2026-01-19 - Log Injection Risk in Serial Telemetry
**Vulnerability:** Telemetry inputs from serial ports were logged directly upon parsing failure without sanitization, allowing for Log Injection (Log Forging).
**Learning:** Even internal or hardware-based inputs (like serial ports) should be treated as untrusted, especially when data integrity is critical.
**Prevention:** Sanitize all inputs before logging, specifically removing or escaping newline characters.

## 2026-01-21 - Regex Sanitization and Unicode Surrogates
**Vulnerability:** Using the broad `\p{C}` regex category for sanitization strips Surrogate characters (`Cs`), breaking valid Unicode text like Emojis.
**Learning:** .NET's `\p{C}` ("Other") category includes `Cs` (Surrogates), `Cf` (Format), `cn` (Not Assigned), `Co` (Private Use), and `Cc` (Control). For security sanitization of text that may contain international characters or emojis, this is too aggressive.
**Prevention:** Use specific categories like `[\p{Cc}\p{Cf}]` (Control and Format) to target invisible control characters without corrupting multi-byte Unicode characters.

## 2026-01-22 - Denial of Service via Unbounded Serial Reads
**Vulnerability:** Using `SerialPort.ReadLine()` without input length limits allows an attacker (or malfunctioning hardware) to cause a Denial of Service (DoS) via memory exhaustion by sending an endless stream of characters without a newline.
**Learning:** Standard .NET `ReadLine()` methods on streams and ports are often unbounded. In high-reliability or security-critical contexts, always enforce explicit buffer limits when reading from I/O.
**Prevention:** Use a bounded reader helper (e.g., `BoundedStreamReader`) that throws or stops reading when a configured maximum length is exceeded.

## 2026-01-24 - DoS via Log Flooding in Serial Loops
**Vulnerability:** Rapid failure in processing loops (e.g., invalid input from serial port) resulted in infinite logging loops, consuming CPU and disk space due to lack of throttling.
**Learning:** High-frequency processing loops must defend against "spam" input. Sanitizing logs protects against injection but not against resource exhaustion from the volume of logs.
**Prevention:** Implement rate limiting or a simple backoff delay (e.g., `Task.Delay`) in error handling paths within tight loops to throttle the response to invalid input.

## 2026-05-20 - Enforcing Invariants in C# Records
**Vulnerability:** DTOs (Data Transfer Objects) defined as `record` types often lack internal validation, relying on consumers to verify data integrity, which leads to "Shotgun Parsing" and potential state corruption.
**Learning:** `record` types with auto-properties (`{ get; init; }`) bypass constructor validation when using object initializers.
**Prevention:** Use explicit backing fields with validation logic in the `init` accessor to enforce invariants during object creation and mutation (via `with` expressions).

## 2026-01-28 - Control System Instability via Non-Finite Numbers
**Vulnerability:** Telemetry parsers using `double.Parse` accepted `NaN` and `Infinity`, which propagated to control loops (PID), potentially causing undefined behavior or crashes.
**Learning:** `double.Parse` and `double.TryParse` allow `NaN` and `Infinity` by default. In control systems or financial apps, these values are often as dangerous as injection attacks.
**Prevention:** Explicitly validate numeric inputs with `double.IsFinite()` after parsing to ensure they represent real-world values.
