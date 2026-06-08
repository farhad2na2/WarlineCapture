Lane: Support/FTUE
Task: Implemented the first M01 assistant recommendation service/provider slice with typed recommendation DTOs, in-session tutorial state, read-only M01 recommendation production, and focused EditMode validation.
Files changed:
- `Assets/Game/Scripts/Tutorial.meta`
- `Assets/Game/Scripts/Tutorial/Assistant.meta`
- `Assets/Game/Scripts/Tutorial/Assistant/WarlineCaptureAssistantService.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/WarlineCaptureAssistantService.cs.meta`
- `Assets/Game/Scripts/Tutorial/Data.meta`
- `Assets/Game/Scripts/Tutorial/Data/TutorialSessionState.cs`
- `Assets/Game/Scripts/Tutorial/Data/TutorialSessionState.cs.meta`
- `Assets/Game/Scripts/Tutorial/Recommendations.meta`
- `Assets/Game/Scripts/Tutorial/Recommendations/AssistantRuntimeModels.cs`
- `Assets/Game/Scripts/Tutorial/Recommendations/AssistantRuntimeModels.cs.meta`
- `Assets/Game/Scripts/Tutorial/Recommendations/M01AssistantRecommendationProvider.cs`
- `Assets/Game/Scripts/Tutorial/Recommendations/M01AssistantRecommendationProvider.cs.meta`
- `Assets/Tests/Editor/Tutorial.meta`
- `Assets/Tests/Editor/Tutorial/M01AssistantRuntimeTests.cs`
- `Assets/Tests/Editor/Tutorial/M01AssistantRuntimeTests.cs.meta`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`
- `Design/AgentReports/2026-05-07_support-ftue_assistant-service-slice.md`
Contracts touched: `WarlineCaptureAssistantService`, `M01AssistantRecommendationProvider`, `AssistantContext`, `AssistantIntent`, `AssistantRecommendation`, `TutorialSessionState`, `M01AssistantIds`, `AssistantPanelPresentationData` conversion, M01 recommendation ids, M01 FTUE step ids, `TacticalCommandReasonCode` recovery mapping, `AssistantContext.TypedCommandHooksAvailable`.
User-visible behavior: No direct gameplay execution or visible UI behavior changed yet. The runtime can now produce contract-safe M01 assistant recommendations and panel presentation data for objectives intro, select squad, move to cover, attack patrol, invalid command recovery, and result explanation.
Validation run:
- Source/meta sanity: checked new Unity `.meta` GUID lengths and `git diff --check`.
- First Unity EditMode run: `M01AssistantRuntimeTests` passed 9/9 but Unity exited with a cleanup error because `Assets/Game/Scripts/Tutorial/Data.meta` had a malformed 31-character GUID.
- Fixed `Assets/Game/Scripts/Tutorial/Data.meta`.
- Required Unity EditMode rerun: `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter M01AssistantRuntimeTests -testResults /private/tmp/warlinecapture-m01-assistant-runtime-results.xml -logFile /private/tmp/warlinecapture-m01-assistant-runtime.log`
Validation result: Passed. Clean rerun exited code 0 with `M01AssistantRuntimeTests` 9/9 passed in `/private/tmp/warlinecapture-m01-assistant-runtime-results.xml`.
Known gaps: The provider uses explicit `AssistantContext` snapshots; live `AssistantContextProvider` wiring from mission/session/objective/selection/bridge state is still needed. `Do It` stays disabled unless `AssistantContext.TypedCommandHooksAvailable` is true, so this Support/FTUE slice does not execute gameplay. Integrated PlayMode/HCI validation is still blocked until UI mounts service data and gameplay command wrappers are connected.
Cross-lane impacts: UI can replace placeholder assistant panel content by binding `WarlineCaptureAssistantService.CreatePresentationData()` into `AssistantPanelController`. Gameplay remains owner of typed command execution for select/move/attack. PM should track the remaining checklist in `AssistantRuntime_M01_Wiring_Plan.md`; I also observed untracked assistant command-runtime files in the shared workspace and left them untouched because they appear to belong to the gameplay wrapper lane.
Next recommended task: Support/FTUE should implement `AssistantContextProvider` live-state wiring after gameplay confirms typed command hooks and UI confirms the mounted assistant panel service entry point.
