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
