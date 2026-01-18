# AeroCore.NET

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![.NET Version](https://img.shields.io/badge/.NET-8.0%2F9.0-blue)
![License](https://img.shields.io/badge/license-MIT-blue)

A cross-platform aerospace module architecture using .NET, implementing a **Hexagonal Architecture (Ports and Adapters)** for modularity and testability. Designed for deterministic flight logic and separation of concerns.

## 🚀 Overview

**AeroCore.NET** is designed to act as the foundational architecture for an Avionics System. It separates the core business logic (Flight Control) from the infrastructure (Sensors, Actuators, OS).

### Key Features
*   **Hexagonal Architecture:** Core domain logic is isolated from external dependencies.
*   **Cross-Platform:** Runs on Linux (e.g., Raspberry Pi, Jetson) and Windows.
*   **Dependency Injection:** Modular design using .NET Generic Host.
*   **Worker Service:** Runs as a daemon/service.
*   **Telemetry Streaming:** Supports both Mock (generated) and Serial (hardware) data streams.

## 📂 Project Structure

*   `src/AeroCore.Shared`: Common Models, Interfaces, and shared Services.
*   `src/AeroCore.FlightComputer`: The main flight control application (the "Brain").
*   `src/AeroCore.GroundStation`: A ground control application to visualize telemetry.
*   `src/AeroCore.Tests`: Unit and integration tests.

## 🛠️ Getting Started

### Prerequisites

*   [.NET 8.0 SDK](https://dotnet.microsoft.com/download) or higher.

### Running the Flight Computer

1.  Navigate to the Flight Computer directory:
    ```bash
    cd src/AeroCore.FlightComputer
    ```

2.  Run with **Mock Telemetry** (Default):
    ```bash
    dotnet run
    ```
    This will start the flight computer using a simulated sensor that generates random telemetry data.

3.  Run with **Serial Telemetry** (Real Hardware):
    ```bash
    dotnet run -- UseSerial=true Serial:PortName=/dev/ttyUSB0 Serial:BaudRate=57600
    ```
    *   `UseSerial=true`: Switches the provider to `SerialTelemetryProvider`.
    *   `Serial:PortName`: The serial port to listen on (e.g., `COM3` on Windows, `/dev/ttyUSB0` on Linux).
    *   `Serial:BaudRate`: Communication speed (default 9600).

### Running the Ground Station

1.  Navigate to the Ground Station directory:
    ```bash
    cd src/AeroCore.GroundStation
    ```

2.  Run the application:
    ```bash
    dotnet run
    ```
    Similar to the Flight Computer, you can configure it to use a Serial connection to listen for incoming telemetry from a radio link:
    ```bash
    dotnet run -- UseSerial=true Serial:PortName=COM4
    ```

## 🏗️ Architecture Details

### Ports & Adapters
*   **Ports (Interfaces):** `ITelemetryProvider`, `IFlightComputer`. Defined in `AeroCore.Shared`.
*   **Adapters (Implementations):**
    *   `MockTelemetryProvider`: Generates fake data for testing/dev.
    *   `SerialTelemetryProvider`: Reads CSV data from a serial port.
    *   `FlightControlUnit`: The domain logic that processes telemetry and issues commands.

### Data Flow
1.  `ITelemetryProvider` streams `TelemetryPacket`s asynchronously.
2.  `IFlightComputer` consumes the stream.
3.  `FlightControlUnit` analyzes the packet (e.g., check Pitch).
4.  If correction is needed, a `ControlCommand` is queued (simulated).

## 🧪 Testing

Run the included unit tests:
```bash
dotnet test src/AeroCore.Tests/AeroCore.Tests.csproj
```

## 📜 License

MIT License
