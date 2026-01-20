## 2026-01-19 - Log Injection Risk in Serial Telemetry
**Vulnerability:** Telemetry inputs from serial ports were logged directly upon parsing failure without sanitization, allowing for Log Injection (Log Forging).
**Learning:** Even internal or hardware-based inputs (like serial ports) should be treated as untrusted, especially when data integrity is critical.
**Prevention:** Sanitize all inputs before logging, specifically removing or escaping newline characters.

## 2026-01-20 - Broadening Log Sanitization Scope
**Vulnerability:** Sanitization focused only on newlines allows other control characters (e.g., Bell, Backspace, ANSI Escape codes) to manipulate terminal output or obfuscate logs. Also, unbounded string logging enables DoS via disk exhaustion.
**Learning:** `\p{C}` in Regex captures all Unicode control characters, providing broader protection than `[\r\n]`. Truncation is a necessary Defense-in-Depth against resource exhaustion.
**Prevention:** Use `\p{C}+` for replacement and enforce a strict length limit (e.g., 500 chars) on loggable inputs.
