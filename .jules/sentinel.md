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
