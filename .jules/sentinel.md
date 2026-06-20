## 2025-02-23 - Prevent Silent DoS in Background Processors
**Vulnerability:** The main control loop (`ProcessLoopAsync`) and command processor (`ProcessCommandsAsync`) in `FlightControlUnit.cs` consumed items via `await foreach` without an inner `try-catch` block.
**Learning:** In long-running background tasks, an unhandled exception thrown during the processing of a single item (e.g., malformed telemetry packet or actuator delay failure) will crash the entire `IAsyncEnumerable` or `ChannelReader` loop, causing a silent Denial of Service (DoS) for all subsequent events while the host application remains running.
**Prevention:** Always wrap the core item-processing logic inside background `await foreach` loops with a `try-catch` block. Explicitly catch `OperationCanceledException` to rethrow for graceful shutdowns, and log other exceptions securely to maintain system availability and resilience.
## 2024-06-13 - [Prevent Log Flooding DoS]
**Vulnerability:** Unbounded `_logger.LogError` calls inside high-frequency `await foreach` stream processing loops in `FlightControlUnit.cs`.
**Learning:** Continuous transient failures or malformed data streams could trigger continuous exception logging, rapidly exhausting disk space and CPU resources, leading to a Denial of Service.
**Prevention:** Implement time-based rate limiting (e.g., via `Environment.TickCount64`) for error logs in hot loops, mirroring existing status logging patterns.

## 2026-06-14 - [Prevent Ground Station Log Flooding DoS]
**Vulnerability:** Unbounded `_logger.LogError` calls inside the high-frequency `await foreach` stream processing loop in `GroundStationWorker.cs`.
**Learning:** Just like the `FlightControlUnit`, the `GroundStationWorker` consumes the telemetry stream in a fast loop. Unrestricted error logging on transient rendering failures can exhaust disk space and CPU.
**Prevention:** Apply the same time-based rate limiting (e.g., via `Environment.TickCount64`) for error logs in all stream consumption loops, not just the core processing unit.

## 2025-02-23 - [Prevent Serial Telemetry Provider Log Flooding DoS]
**Vulnerability:** Unbounded `_logger.LogError` calls inside the high-frequency `stream.ReadAsync` catch block in `SerialTelemetryProvider.cs`.
**Learning:** Even when reading from the underlying stream directly (instead of processing streams with `await foreach`), continuous transient hardware failures or stream read errors can trigger continuous exception logging, rapidly exhausting disk space and CPU resources, leading to a Denial of Service.
**Prevention:** Apply the time-based rate limiting (e.g., via `Environment.TickCount64`) for error logs in any continuous data reading loop, such as stream read loops.

## 2025-02-23 - Prevent Audit Log Evasion via Shared State Concurrency
**Vulnerability:** A single shared `_lastErrorLog` variable was used to rate-limit error logs across two distinct, concurrent tasks (`ProcessLoopAsync` for telemetry and `ProcessCommandsAsync` for commands) in `FlightControlUnit.cs`.
**Learning:** In a multi-threaded or concurrent async environment, sharing a single rate-limiting state variable allows a flood of errors in one system (e.g. malformed telemetry inputs) to continuously update the timestamp, thereby silently suppressing critical error logs from the other system (e.g. actuator command failures). An attacker could exploit this to mask malicious activity or hardware failure.
**Prevention:** Always maintain independent, isolated state variables for rate-limiting operations in distinct concurrent tasks, ensuring that one noisy process cannot suppress the audit trail of another.
