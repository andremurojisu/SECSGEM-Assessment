# SECS/GEM Machine Simulator and Middleware

## Overview

This project was developed as part of a SECS/GEM technical assessment. It contains two machine simulators and one middleware application.

- **Machine Simulator A** follows the supplied MG22 communication flow and uses the equipment-specific report, variable, and event identifiers from the provided scope.
- **Machine Simulator B** uses a simplified generic SECS/GEM-style flow with symbolic identifiers such as `RPTID1`, `SVID1`, and `CEID1`.
- **Middleware** connects to both simulators in parallel, performs the setup sequences, receives `S6F11` events, returns `S6F12` acknowledgements, maps selected events to JSON telemetry, and sends the payloads through HTTP POST.

## Project Structure

```text
SECSGEM-Assessment/
├── MachineSimulatorA/
│   ├── Program.cs
│   └── MachineSimulatorA.csproj
├── MachineSimulatorB/
│   ├── Program.cs
│   └── MachineSimulatorB.csproj
├── Middleware/
│   ├── Program.cs
│   └── Middleware.csproj
├── Documentation/
│   └── SECS_GEM_Assessment_Documentation_Report.pdf
├── SECSGEM-Assessment.sln
└── README.md
```

## Requirements

- Windows
- Visual Studio
- .NET Framework supported by the solution
- Network access to the HTTP endpoint when telemetry delivery is being tested

## Configuration

The simulators use the following local addresses:

| Component | Address |
|---|---|
| Machine Simulator A | `127.0.0.1:5001` |
| Machine Simulator B | `127.0.0.1:5002` |

The telemetry endpoint is intentionally left empty in the repository:

```csharp
static string telemetryEndpoint = "";
```

Set the endpoint locally in `Middleware/Program.cs` before testing HTTP telemetry delivery.

## How to Run

1. Open `SECSGEM-Assessment.sln` in Visual Studio.
2. Build the solution.
3. Start `MachineSimulatorA`.
4. Start `MachineSimulatorB`.
5. Start `Middleware`.

The middleware runs independent workers for both machine connections, so Machine A and Machine B operate in parallel.

## Expected Communication

### Machine A

```text
S1F13 / S1F14
S2F33 / S2F34
S2F35 / S2F36
S2F37 / S2F38
S1F3  / S1F4
S2F41 / S2F42  (REMOTE)
S1F3  / S1F4   (ControlState)
S2F41 / S2F42  (PPSELECT)
S6F11 / S6F12
```

Machine A uses the equipment-specific report and event configuration from the supplied communication scope. The implemented demonstration generates `CEID 13` (`processRecipeSelected`) after `PPSELECT`.

### Machine B

Machine B keeps the same general report/event setup pattern but uses generic symbolic identifiers.

```text
RPTID1 -> SVID1, SVID2
RPTID2 -> SVID3

CEID1 -> RPTID1
CEID2 -> RPTID2
```

Its communication flow is:

```text
S1F13 / S1F14
S2F33 / S2F34
S2F35 / S2F36
S2F37 / S2F38
S1F3  / S1F4
S6F11 / S6F12
```

`CEID1` is used as `EVENT1` in the implemented telemetry demonstration.

## Telemetry

When an implemented `S6F11` event is received, the middleware:

1. Sends `S6F12` acknowledgement.
2. Identifies the event.
3. Builds a JSON payload.
4. Sends the payload to the configured HTTP endpoint using `POST`.

Example Machine A payload:

```json
{
  "machine": "MachineA",
  "timestamp": "2026-08-26T13:43:54+07:00",
  "ceid": 13,
  "event": "processRecipeSelected"
}
```

Example Machine B payload:

```json
{
  "machine": "MachineB",
  "timestamp": "2026-08-26T13:44:04+07:00",
  "ceid": "CEID1",
  "event": "EVENT1"
}
```

## Notes and Limitations

- Messages are represented as readable SML-style text over TCP.
- `<END>` is used as a custom message delimiter for this prototype.
- The implementation is not a full binary HSMS/SECS-II stack.
- Machine B identifiers are symbolic simulation identifiers and are not presented as equipment-specific numeric IDs.
- The implementation focuses on the communication sequence, event handling, telemetry mapping, and HTTP transmission required for the assessment.
- Production use would require a proper SECS/GEM/HSMS library, protocol timers, structured configuration, stronger validation, logging, testing, and security controls.

## Documentation

The complete assessment report is included in the `Documentation` folder.

It contains the detailed system architecture, machine simulator configuration, SECS/GEM communication flows, middleware implementation, telemetry mapping, verification results, setup instructions, and implementation notes.

[View the SECS/GEM Assessment Documentation & Report](Documentation/SECS_GEM_Assessment_Documentation_Report.pdf)
