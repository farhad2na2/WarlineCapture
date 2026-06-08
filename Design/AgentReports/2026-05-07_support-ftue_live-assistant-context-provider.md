Lane: Support/FTUE
Task: Implemented live M01 `AssistantContextProvider` runtime mapping for assistant recommendations and typed command readiness.
Files changed:
- `Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs.meta`
- `Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs`
- `Assets/Game/Scripts/UI/Shell/WarlineCaptureMatchResultFlow.cs`
- `Assets/Tests/Editor/Tutorial/AssistantContextProviderTests.cs`
- `Assets/Tests/Editor/Tutorial/AssistantContextProviderTests.cs.meta`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`
Contracts touched:
- `AssistantContextProvider.BuildContext(TutorialSessionState)` now maps live M01 mission ids, route/match state, objective visibility, ECS runtime entities, selected squad state, move anchor availability, enemy patrol state, latest command result, session move/attack completion, and typed-command readiness.
- `BattleHudGameplayBridge` now exposes `CurrentCommandMode`, `LastCommandResult`, and `HasLastCommandResult` while preserving existing tactical feedback behavior.
- `WarlineCaptureMatchResultFlow` now exposes `HasActivePopup` so result visibility can be read without popup child hierarchy inspection.
- M01 runtime wiring plan now marks live context-provider mapping as implemented.
User-visible behavior:
- ARIA recommendations can now be driven from live runtime state instead of test-authored snapshots.
- `Do It` availability follows actual M01 readiness: active mission, live command squad, selected/commandable state, valid cover anchor, live patrol state, and command hook availability.
- Invalid-command recovery can use the latest accepted/rejected tactical command result from the gameplay bridge.
- Result explanation can detect an active mission result popup through a typed runtime property.
Validation run:
- `git diff --check -- Assets/Game/Scripts/Tutorial Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs Assets/Game/Scripts/UI/Shell/WarlineCaptureMatchResultFlow.cs Assets/Tests/Editor/Tutorial Design/AssistantRuntime_M01_Wiring_Plan.md`
- `rg -n "\.Find\(|GetComponentInChildren|Screen\.|mousePosition|anchoredPosition|NameText|SelectedEntityPanel|Button" Assets/Game/Scripts/Tutorial/Assistant/AssistantContextProvider.cs Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs`
- Unity EditMode `AssistantContextProviderTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`, results `/private/tmp/warlinecapture-assistant-context-provider-results.xml`
- Unity EditMode `M01AssistantRuntimeTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`, results `/private/tmp/warlinecapture-m01-assistant-runtime-results.xml`
- Unity EditMode `CommandIntentExecutorTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`, results `/private/tmp/warlinecapture-command-intent-executor-results.xml`
- Unity EditMode `BattleHudGameplayBridgeConnectionTests` in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`, results `/private/tmp/warlinecapture-battlehud-bridge-results.xml`
Validation result:
- Primary Unity workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity` was locked by another Unity instance, so validation used the authorized second workspace.
- `git diff --check` passed.
- UI hierarchy/screen-coordinate grep returned no matches.
- `AssistantContextProviderTests`: 7/7 passed.
- `M01AssistantRuntimeTests`: 9/9 passed.
- `CommandIntentExecutorTests`: 14/14 passed.
- `BattleHudGameplayBridgeConnectionTests`: 6/6 passed.
Known gaps:
- UI still needs to bind the mounted `AssistantPanelController` to `WarlineCaptureAssistantService`, `AssistantContextProvider`, and `CommandIntentExecutor`.
- Visible assistant ownership/takeover UX is still pending UI lane work.
- Result popup explanation is detected, but this pass does not implement result-popup close/acknowledge behavior.
Cross-lane impacts:
- UI can now request a live context snapshot instead of building test-only `AssistantContext` objects.
- UI should keep `Do It` button execution behind `CommandIntentExecutor`; the context provider only reads state.
- QA/HCI can use the live context provider as evidence for M01 assistant recommendation smoke once the panel is mounted.
Next recommended task:
- Wire `AssistantPanelController` runtime binding so the assistant button/panel evaluates `AssistantContextProvider.BuildContext(...)`, displays `WarlineCaptureAssistantService.CreatePresentationData()`, executes `Do It` through `CommandIntentExecutor`, and keeps `Show Me`/`Stop` bounded to assistant-owned state.
