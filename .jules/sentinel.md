## 2026-01-19 - Log Injection Risk in Serial Telemetry
**Vulnerability:** Telemetry inputs from serial ports were logged directly upon parsing failure without sanitization, allowing for Log Injection (Log Forging).
**Learning:** Even internal or hardware-based inputs (like serial ports) should be treated as untrusted, especially when data integrity is critical.
**Prevention:** Sanitize all inputs before logging, specifically removing or escaping newline characters.

## 2026-01-21 - Regex Sanitization and Unicode Surrogates
**Vulnerability:** Using the broad `\p{C}` regex category for sanitization strips Surrogate characters (`Cs`), breaking valid Unicode text like Emojis.
**Learning:** .NET's `\p{C}` ("Other") category includes `Cs` (Surrogates), `Cf` (Format), `cn` (Not Assigned), `Co` (Private Use), and `Cc` (Control). For security sanitization of text that may contain international characters or emojis, this is too aggressive.
**Prevention:** Use specific categories like `[\p{Cc}\p{Cf}]` (Control and Format) to target invisible control characters without corrupting multi-byte Unicode characters.

## 2026-01-24 - Serial Port DoS via Memory Exhaustion
**Vulnerability:** `SerialPort.ReadLine()` is unbounded and reads until a newline is found. An attacker controlling the serial input could send a continuous stream of characters without a newline, causing the application to buffer indefinitely until an OutOfMemoryException occurs (DoS).
**Learning:** Standard .NET `ReadLine` methods on streams/ports are often unsafe for untrusted input because they lack length limits.
**Prevention:** Use a bounded reader (like `BoundedStreamReader`) that imposes a maximum line length when reading from untrusted streams.
