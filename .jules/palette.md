# Palette's Journal
## 2025-02-28 - [Accessible Console Legends]
**Learning:** Console UI status legends that rely purely on color names (e.g., `Green/Yel/Red`) are an accessibility failure for colorblind users and screen readers.
**Action:** Always use explicit text tags (e.g., `[ OK ] / [WARN] / [CRIT]`) alongside colors to convey status meaning in CLI applications.

## 2025-03-02 - [Clean CLI Shutdowns with In-Place Updates]
**Learning:** Console applications that use carriage returns (`\r`) for in-place UI updates risk having asynchronous system shutdown logs (like "Application is shutting down...") appended to the middle of the active data line, looking messy and causing awkward text wrapping.
**Action:** Always register a cancellation callback on the application's stopping token (e.g., `stoppingToken.Register(() => Console.WriteLine())`) to ensure a clean newline is printed immediately before host shutdown logs begin.
