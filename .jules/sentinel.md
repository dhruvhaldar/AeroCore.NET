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

## 2026-06-03 - Denial of Service via Infinite Loop on Ignored Characters
**Vulnerability:** A `BoundedStreamReader` implementation correctly checked buffer limits but contained a logic flaw where ignored characters (like `\r`) triggered a `continue` statement without incrementing a read counter. This allowed an attacker to send an infinite stream of ignored characters, causing an infinite CPU loop without triggering length limits.
**Learning:** Resource limits (like `maxLength`) must apply to the *input consumed* (bytes read from the stream), not just the *output produced* (buffer size). Input validation loops must ensure progress or termination on every iteration.
**Prevention:** Track total characters read from the stream and enforce limits on this total count, ensuring that even "ignored" characters contribute to the resource quota.

## 2026-05-25 - Denial of Service via Unbounded Command Queue
**Vulnerability:** The Flight Control Unit enqueued commands into a `ConcurrentQueue` without a consumer, leading to infinite memory growth (DoS) during high-activity states.
**Learning:** Producer-Consumer patterns must always have an active consumer or a bounded queue size. Disconnected or "fire-and-forget" producers are memory leaks in disguise.
**Prevention:** Implement active consumers for all queues or enforce `BoundedCapacity` and drop/reject policies when the queue is full.

## 2026-06-10 - Log Injection via Configuration Injection
**Vulnerability:** The Serial Telemetry Provider logged the configured port name directly without sanitization, allowing an attacker with control over the environment (e.g., via `Serial__PortName` environment variable) to inject malicious log entries.
**Learning:** Configuration values (like environment variables, appsettings) are external inputs and must be treated as untrusted, especially when logging them at startup.
**Prevention:** Always sanitize configuration values before logging them, using the same sanitization routines applied to user input.

## 2026-06-15 - Arbitrary File Read via Loose Serial Port Validation
**Vulnerability:** The `IsValidSerialPortName` validation allowed arbitrary file paths (e.g., `/dev/mem`, `Common/foo.txt`) because it only checked for forbidden characters and loose prefixes, enabling attackers to read sensitive files if they could control the configuration.
**Learning:** `SerialPort` APIs on both Windows and Linux treat file paths as valid ports. Validating only "safe characters" is insufficient; strict allowlisting of platform-specific device namespaces (like `/dev/tty` or `COM`) is required.
**Prevention:** Implement strict prefix allowlisting for serial ports (e.g., `/dev/tty`, `/dev/cu`, `COM` followed by digits) and reject all other paths, even if they contain "safe" characters.

## 2026-06-25 - CPU Denial of Service via Unbounded Parsing
**Vulnerability:** The `TelemetryParser.Parse` methods accepted unbounded inputs (strings or spans), allowing an attacker to cause CPU exhaustion (DoS) by sending excessively long strings that triggered linear scans (`IndexOf`).
**Learning:** Parsing logic, especially for string processing (like `IndexOf` or `Split`), often has O(N) complexity. Without input length limits, this becomes a DoS vector.
**Prevention:** Always enforce strict maximum length limits on inputs to parsing functions, especially those exposed publicly or used in high-frequency loops.

## 2026-06-25 - Log Flooding via Rate-Limited Errors
**Vulnerability:** Although a `Task.Delay` throttled the CPU usage of error loops, it still allowed ~10 logs/second, which can accumulate to ~36,000 logs/hour, filling disk space and masking real issues.
**Learning:** CPU throttling (delays) does not equal Log throttling. Logs should be rate-limited independently based on time (e.g., max 1 per second) to prevent storage exhaustion.
**Prevention:** Implement explicit time-based rate limiting for repetitive error logs, independent of the processing loop delay.

## 2026-06-25 - Denial of Service via Serial Port Write Blocking
**Vulnerability:** A missing `WriteTimeout` on the `SerialPort` class instance could allow the serial port driver to block the execution thread indefinitely if writing to the port and the hardware buffer fills up or halts.
**Learning:** Default configuration for hardware interfaces like serial ports often lacks bounds or timeouts for basic operations. In high-reliability applications, infinite blocking leads to thread exhaustion.
**Prevention:** Always configure `WriteTimeout` along with `ReadTimeout` for I/O bounds to prevent denial of service through hardware stalls.
## 2026-03-07 - DoS via Unbounded String Scanning
**Vulnerability:** `string.IsNullOrWhiteSpace` performs an O(N) scan. Passing a massive string (e.g. gigabytes of spaces) to `TelemetryParser.ParseFromCsv` could trigger CPU exhaustion (DoS) before any length limits in the downstream `Parse(ReadOnlySpan<char>)` were applied.
**Learning:** Even simple standard library string checks like `IsNullOrWhiteSpace` can be vectors for Denial of Service if the input length is unbounded.
**Prevention:** Always enforce strict length limits (e.g. `line.Length > 1024`) *before* executing any O(N) operations on untrusted input strings.
## 2024-05-24 - Strict Serial Port Name Validation
**Vulnerability:** The `SecurityHelper.IsValidSerialPortName` allowed device prefixes without a suffix, and didn't enforce a length limit, allowing DoS via CPU/Memory exhaustion and opening base devices like `/dev/tty` or `COM`. It also allowed `/dev/mem` or `/dev/sda`. It also allowed Windows to open files like `Common/foo.txt` because it only checked `StartsWith("COM")`.
**Learning:** Checking `StartsWith` is insufficient for validating device paths if the suffix is not checked for existence or content.
**Prevention:** Ensure device paths have a specific allowed prefix, a minimum length (prefix + suffix), and that the suffix matches expected patterns (e.g., digits for Windows COM ports).

## 2026-06-25 - Data Injection via Unbounded Angles
**Vulnerability:** The telemetry parser verified that numerical values (e.g., `Pitch`, `Roll`) were finite to prevent logic crashes, but failed to enforce specific mathematical and physical bounds (e.g., `[-180.0, 180.0]` for degrees). This exposed the system to maliciously crafted telemetry that could destabilize downstream state controllers.
**Learning:** Enforcing mathematically finite input is necessary but not sufficient for safety-critical logic. Domain logic bounding ("Physical Bounds Checking") is critical to preventing exploitation through unexpected edge states.
**Prevention:** Apply strict value range validation in parsers and models immediately upon instantiating telemetry input structures, rejecting logic-breaking edge cases safely.
## 2024-05-24 - Resource Exhaustion DoS via Unclosed Serial Ports in IAsyncEnumerable
**Vulnerability:** A `SerialPort` could remain open if the `await foreach` loop consuming its stream throws an exception (e.g., `TaskCanceledException`) or gets canceled before completing normally. This prevents subsequent connections, causing a resource exhaustion Denial of Service (DoS).
**Learning:** Returning from an `IAsyncEnumerable` (`yield return`) does not guarantee that code placed *after* the `await foreach` loop will execute if the loop is broken or canceled abruptly.
**Prevention:** Always wrap iteration loops (`await foreach`) over resources (like serial ports, network streams, files) in a `try...finally` block, placing the resource cleanup/close logic in the `finally` block to ensure reliable execution regardless of how the loop exits.
