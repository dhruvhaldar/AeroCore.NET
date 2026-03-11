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

## 2025-03-09 - [Peripheral Visibility of Critical Status in CLI Dashboards]
**Learning:** In dense console telemetry dashboards, placing critical status indicators only at the end of a long, dynamically changing string forces users to constantly scan back and forth to assess system health, increasing cognitive load and reaction time.
**Action:** Always provide peripheral visual cues for critical status changes (e.g., dynamically color-coding the leftmost active spinner or prefix character) to ensure immediate visibility without requiring the user to read the entire data line.
## 2025-03-09 - Semantic Unicode Arrows for CLI Dashboards
**Learning:** Using ASCII characters (like `^^` or `v `) for trend indicators in dense CLI dashboards can be visually noisy and less intuitive. Semantic Unicode arrows (`↑↑`, `↑ `, `↓ `, `↓↓`) significantly improve scannability and visual polish, while providing better meaning.
**Action:** When designing or updating CLI dashboards that indicate directional trends, prefer Unicode arrows over ASCII approximations. Always ensure `Console.OutputEncoding = System.Text.Encoding.UTF8;` is set to guarantee proper rendering across different terminals.

## 2025-03-10 - Smooth Braille Spinners for CLI Animations
**Learning:** Using basic ASCII characters (like `|`, `/`, `-`, `\`) for loading animations or spinners in CLI applications can feel clunky and visually outdated, especially in modern UTF-8 supported terminals.
**Action:** Always prefer Braille block characters (e.g., `⠋`, `⠙`, `⠹`, `⠸`, `⠼`, `⠴`, `⠦`, `⠧`, `⠇`, `⠏`) for CLI spinners to provide a significantly smoother, more visually polished, and modern loading animation when `Console.OutputEncoding = System.Text.Encoding.UTF8;` is enabled.

## 2025-03-11 - Semantic Unicode Arrows for CLI Dashboards (Stable)
**Learning:** Using ASCII characters (like `- `) for trend indicators in dense CLI dashboards can be visually noisy and less intuitive. Semantic Unicode arrows (`→ `) significantly improve scannability and visual polish, while providing better meaning.
**Action:** When designing or updating CLI dashboards that indicate directional trends, prefer Unicode arrows over ASCII approximations. Always ensure `Console.OutputEncoding = System.Text.Encoding.UTF8;` is set to guarantee proper rendering across different terminals.
