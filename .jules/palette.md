# Palette's Journal
## 2025-02-28 - [Accessible Console Legends]
**Learning:** Console UI status legends that rely purely on color names (e.g., `Green/Yel/Red`) are an accessibility failure for colorblind users and screen readers.
**Action:** Always use explicit text tags (e.g., `[ OK ] / [WARN] / [CRIT]`) alongside colors to convey status meaning in CLI applications.

## 2025-03-02 - [Clean CLI Shutdowns with In-Place Updates]
**Learning:** Console applications that use carriage returns (`\r`) for in-place UI updates risk having asynchronous system shutdown logs (like "Application is shutting down...") appended to the middle of the active data line, looking messy and causing awkward text wrapping.
**Action:** Always register a cancellation callback on the application's stopping token (e.g., `stoppingToken.Register(() => Console.WriteLine())`) to ensure a clean newline is printed immediately before host shutdown logs begin.

## 2025-03-06 - [Explicit Legend Mappings for CLI Apps]
**Learning:** Symbolic UI legends in console applications must provide explicit 1-to-1 text mappings to prevent cognitive and screen-reader accessibility gaps. Merely listing symbols alongside a grouped description (e.g., "Fast / Slow / Stable Trend") leaves users guessing which symbol maps to which state.
**Action:** Always ensure a 1-to-1 text mapping is provided for each symbol in a UI legend (e.g., "Fast Rise / Slow Rise / Stable / Slow Fall / Fast Fall").
