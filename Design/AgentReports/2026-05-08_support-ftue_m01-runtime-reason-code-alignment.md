Lane:
Support/FTUE

Task:
M01 runtime reason-code alignment after QA/HCI reported QAHCI-G4-012.

Files changed:
- Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs
- Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs
- Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRuntime.cs
- Assets/Game/Scripts/Tutorial/Assistant/WarlineCaptureAssistantService.cs
- Assets/Game/Scripts/Tutorial/Recommendations/M01AssistantRecommendationProvider.cs
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantRuntimeBindingTests.cs
- Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs
- Assets/Tests/Editor/Tutorial/CommandIntentExecutorTests.cs
- Assets/Tests/Editor/Tutorial/M01AssistantCommandRuntimeTests.cs
- Design/AgentReports/2026-05-08_support-ftue_m01-runtime-reason-code-alignment.md

Contracts touched:
- Replaced the runtime `TacticalCommandReasonCode` aliases flagged by QA/HCI with the canonical M01 contract names: `TargetOutOfBounds`, `TargetBlocked`, `TargetUnreachable`, `TargetNotEnemy`, `TargetNotAttackable`, `CommandUnavailable`, `MissionDoesNotAllowBuild`, `CameraJumpUnavailable`, and `NoSelection`.
- Updated assistant recovery routing to consume the canonical reason codes.
- Updated command executor, M01 assistant command runtime, assistant service, RTS selection/HUD rejection emissions, and focused tests to stop emitting/asserting the legacy aliases.
- No assistant ownership, `Stop`, `Show Me`, command-boundary, route id, mission id, or UI hierarchy contract was changed.

User-visible behavior:
Invalid-command feedback now comes from canonical M01 reason-code names and updated player-facing strings, including `Route is blocked.`, `Target is unreachable.`, `Select a hostile target.`, `Target cannot be attacked.`, `Command unavailable.`, and `Camera focus unavailable.` The assistant still recovers through the existing M01 recommendation flow.

Validation run:
- Static legacy enum scan: `rg -n "InvalidTarget|BlockedRoute|OutOfRange|BuildModeUnavailable|InsufficientResources|AbilityOnCooldown|TransportUnavailable" Assets/Game/Scripts Assets/Tests -g '*.cs'`
- Scoped diff hygiene: `git diff --check --` across touched runtime/test files.
- Focused Unity EditMode attempt: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testResults /private/tmp/warlinecapture-support-reason-code-results.xml -logFile /private/tmp/warlinecapture-support-reason-code.log`
- Focused Unity EditMode retry: `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter WarlineCaptureUiAssistantRuntimeBindingTests -testResults /private/tmp/warlinecapture-support-reason-code-assistant-results.xml -logFile /private/tmp/warlinecapture-support-reason-code-assistant.log`
- Final focused Unity EditMode validation in `/Users/farhad/Projects/WarlineCapture-CodexUnity3`:
  - `WarlineCaptureUiAssistantRuntimeBindingTests`, results `/private/tmp/warlinecapture-support-reason-code-final-results.xml`, log `/private/tmp/warlinecapture-support-reason-code-final.log`
  - `WarlineCaptureUiMatchOverlayTests`, results `/private/tmp/warlinecapture-support-reason-code-matchoverlay-results.xml`, log `/private/tmp/warlinecapture-support-reason-code-matchoverlay.log`
  - `CommandIntentExecutorTests`, results `/private/tmp/warlinecapture-support-reason-code-executor-results.xml`, log `/private/tmp/warlinecapture-support-reason-code-executor.log`
  - `M01AssistantCommandRuntimeTests`, results `/private/tmp/warlinecapture-support-reason-code-command-runtime-results.xml`, log `/private/tmp/warlinecapture-support-reason-code-command-runtime.log`

Validation result:
Static validation passed for the scoped runtime/test cleanup: no `TacticalCommandReasonCode` legacy aliases remain in `Assets/Game/Scripts` or `Assets/Tests`; the only broad-text `OutOfRange` hit is the unrelated framework exception type `ArgumentOutOfRangeException` in `Assets/Game/Scripts/UI/MenuView.cs`. `git diff --check` passed. Initial Unity EditMode validation could not complete in `WarlineCapture-CodexUnity` because Unity entered repeated licensing reconnect/unsupported-protocol loops before tests started; the stuck process was stopped with `pkill -f warlinecapture-support-reason-code.log`. A retry in `WarlineCapture-CodexUnity2` also did not reach tests: Unity aborted with compile errors caused by missing package/assembly references including `Unity.Entities`, `Unity.Mathematics`, TextMeshPro/UI, and InputSystem. Final focused validation in `WarlineCapture-CodexUnity3` passed: `WarlineCaptureUiAssistantRuntimeBindingTests` 7/7, `WarlineCaptureUiMatchOverlayTests` 18/18, `CommandIntentExecutorTests` 14/14, and `M01AssistantCommandRuntimeTests` 10/10.

Known gaps:
The current cleanup uses canonical names but keeps most previous generic invalid-target emissions mapped to `TargetNotAttackable`; deeper semantic splitting into `TargetOutOfBounds`, `TargetNotEnemy`, or `TargetUnreachable` should only be done if Gameplay/UI provides more granular runtime failure data.

Cross-lane impacts:
Gameplay/UI should review whether any generic `TargetNotAttackable` emissions should be refined to more specific canonical reasons from runtime context. QA/HCI can rerun QAHCI-G4-012 from this validated handoff. UI remains owner only if later public-launch or device validation regresses safe-area behavior, and art/design or the implementing lane remains owner of QAHCI-G4-014 marker/VFX readiness.

Next recommended task:
QA/HCI should recheck QAHCI-G4-012 using this validated reason-code handoff. Gameplay/UI still own public M01 launch-path closure and any future semantic refinement of generic `TargetNotAttackable` emissions.
