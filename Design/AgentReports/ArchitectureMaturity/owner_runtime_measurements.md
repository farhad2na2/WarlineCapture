# Architecture Maturity Owner Runtime Measurements

- Baseline commit: `91aaf6db3f44dc6f6ed44ecdaa8b7a702f0e29e9`
- Baseline tree: `65cfc22224729c10c3a149af4869ba79075dae6c`
- Unity: `6000.5.2f1`
- Result: both focused validations passed with zero compiler errors and zero measured current-thread allocation.

| Owner | Average | P95 | Max | Warmup / measured | Result |
|---|---:|---:|---:|---:|---|
| `Assets/Game/Scripts/Systems/GroundMissileLauncherSystems.cs` | 0.924 ms | 0.963 ms | 1.229 ms total | 16 / 64 scenarios | Passed |
| `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs` | 0.253 ms | 0.271 ms | 3.272 ms total | 16 / 64 scenarios | Passed |

The JSON artifact binds each measurement to the exact production source, focused harness, project version, package lock, command, commit, and tree. A source or harness hash mismatch makes the row ineligible for AM-009 selection.
