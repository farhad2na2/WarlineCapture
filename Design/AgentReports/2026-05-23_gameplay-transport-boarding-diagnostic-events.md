Lane
Gameplay

Task
Migrate remaining direct `Debug.Log*` transport boarding diagnostics into the ECS diagnostics boundary.

Files changed
- `Assets/Game/Scripts/Components/TransportBoardingDiagnosticLogComponents.cs`
- `Assets/Game/Scripts/Components/TransportBoardingDiagnosticLogComponents.cs.meta`
- `Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs`
- `Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs.meta`
- `Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs`
- `Assets/Game/Scripts/UI/RTSSelectionSystem.cs`
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/AgentReports/2026-05-23_gameplay-transport-boarding-diagnostic-events.md`

Contracts touched
- Added `TransportBoardingDiagnosticLogQueueComponent` and `TransportBoardingDiagnosticLogComponent` as the ECS transport diagnostic event queue.
- Added `TransportBoardingDiagnosticLogFlushSystem` as the shell logging boundary for queued transport boarding diagnostics.
- `UnitTransportBoardingSystem` no longer calls direct `Debug.Log` or `RuntimeDiagnostics.ShouldLogTransportBoarding`; it reads `RuntimeDiagnosticsStateComponent`, gates formatting, queues transport diagnostic events, and keeps periodic wait logging behind the existing frame interval.
- `RTSSelectionSystem` transport boarding command diagnostics now read `RuntimeDiagnosticsStateComponent`, gate formatting before entity/pathing descriptions, and queue transport diagnostic events.
- Architecture tests now block direct `[TransportBoard]` `Debug.Log` and `RuntimeDiagnostics.ShouldLogTransportBoarding` usage in the migrated transport boarding files.

User-visible behavior
- No intended gameplay behavior change.
- Transport boarding and selection diagnostics keep the existing `[TransportBoard]` and `[Selection]` message prefixes when diagnostics are enabled.
- Disabled transport diagnostics now avoid formatting the expensive entity/pathing detail strings in the migrated transport boarding command/execution paths.

Validation run
- `rg -n "Debug\\.Log\\(\\$?\\\"\\[TransportBoard\\]|RuntimeDiagnostics\\.ShouldLogTransportBoarding|LogTransportBoarding\\(" Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs Assets/Game/Scripts/UI/RTSSelectionSystem.cs Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs`
- `git diff --check -- Assets/Game/Scripts/Components/TransportBoardingDiagnosticLogComponents.cs Assets/Game/Scripts/Components/TransportBoardingDiagnosticLogComponents.cs.meta Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs Assets/Game/Scripts/Systems/TransportBoardingDiagnosticLogFlushSystem.cs.meta Assets/Game/Scripts/Systems/UnitTransportBoardingSystem.cs Assets/Game/Scripts/UI/RTSSelectionSystem.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/rts_selection_system_responsibility_audit.md`
- Unity EditMode `GameplayArchitectureContractTests`
- Unity EditMode `Transport`

Validation result
- No direct `[TransportBoard]` `Debug.Log`, `RuntimeDiagnostics.ShouldLogTransportBoarding`, or `LogTransportBoarding` usage remains in the migrated production files.
- Diff whitespace check passed.
- `GameplayArchitectureContractTests`: passed 54/54.
- Unity EditMode `Transport`: passed 29/29.

Known gaps
- Existing non-transport direct `Debug.Log*` gameplay diagnostics remain and should continue migrating by domain slice.
- `RuntimeDiagnosticsSystem` still mirrors legacy `InitialUnitsRuntimeState` diagnostics flags into `RuntimeDiagnosticsStateComponent`; removing that compatibility mirror is a separate migration.
- `RTSSelectionSystem` still has non-transport move-order debug diagnostics behind disabled constants; those are outside this transport boarding slice.

Cross-lane impacts
- QA/debug transport boarding output should now come from `TransportBoardingDiagnosticLogFlushSystem` rather than direct command/execution call sites.
- The architecture contract now prevents new direct transport boarding Unity log calls in the migrated files.

Next recommended task
Continue migrating remaining direct gameplay `Debug.Log*` diagnostics by domain slice, with move-order/group-move diagnostics in `RTSSelectionSystem` as a narrow next candidate.
