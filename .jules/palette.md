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

## 2025-03-07 - [Screen Reader Friendly Symbol Grouping]
**Learning:** Even when 1-to-1 mappings are provided, grouped symbol descriptions separated by slashes (e.g. `^^ / ^ / - : Fast Rise / Slow Rise / Stable`) are confusing to screen readers, which will read all symbols first and then all descriptions, losing the association.
**Action:** Always interleave the symbol and its description explicitly (e.g. `^^ : Fast Rise | ^ : Slow Rise | - : Stable`) to ensure screen readers read the symbol and its corresponding meaning together.

## 2025-03-05 - [Visual Polish] Formatting Width on Dynamic TUI Elements
**Learning:** In in-place updating console CLI dashboards, insufficient fixed formatting widths for numeric values can cause horizontal jitter when value sizes change (e.g. crossing zero into negative values or expanding digit counts).
**Action:** When working on CLI dashboards, carefully set and review `WriteFormatted` widths to adequately account for negative signs, decimal points, and maximum expected digits to guarantee consistent column alignment.

## 2025-03-08 - [Dynamic String Artifacting in In-Place CLI Updates]
**Learning:** In-place updating console dashboards using carriage returns (`\r`) do not clear the end of the line. If a dynamic-length string (e.g., a comma-separated list of error reasons) transitions to a shorter state, the trailing characters of the previous string remain visible as visual garbage, causing severe horizontal UI jitter.
**Action:** Always enforce fixed-width output for dynamic strings in in-place updating CLIs by explicitly padding them with trailing spaces to match or exceed the maximum possible length of the string, ensuring previous data is fully overwritten.
