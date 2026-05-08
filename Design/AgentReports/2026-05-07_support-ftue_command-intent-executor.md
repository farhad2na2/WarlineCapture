Lane: Support/FTUE
Task: Connected M01 assistant `Do It` actions through a typed `CommandIntentExecutor` boundary to the accepted gameplay command hooks.
Files changed:
- `Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs`
- `Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs.meta`
- `Assets/Game/Scripts/Tutorial/Assistant/WarlineCaptureAssistantService.cs`
- `Assets/Tests/Editor/Tutorial/CommandIntentExecutorTests.cs`
- `Assets/Tests/Editor/Tutorial/CommandIntentExecutorTests.cs.meta`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
Contracts touched:
- M01 assistant runtime wiring contract now marks gameplay typed hooks as accepted and the command intent executor boundary as implemented.
- `WarlineCaptureAssistantService.ExecuteCurrentDoIt(CommandIntentExecutor)` exposes a service-level execution handoff without direct UI hierarchy dependency.
- `CommandIntentExecutor` accepts typed `AssistantIntent` DTOs and returns `TacticalCommandResult` accepted/rejected outcomes with existing `TacticalCommandReasonCode` values.
User-visible behavior:
- ARIA `Do It` can now select `unit.player.rifle_squad_01`, move selected units to `tutorial.move_target.cover_01`, and attack `unit.enemy.patrol_01` through accepted gameplay hooks.
- `Show Me` focus/highlight intents remain non-executing and reject if routed through the gameplay executor.
- `Stop` clears assistant preview/takeover session state only and does not issue gameplay commands.
- M01 build attempts still reject with the existing mission build-lock reason.
Validation run:
- `git diff --check -- Assets/Game/Scripts/Tutorial Assets/Tests/Editor/Tutorial Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`
- `rg -n "\.Find\(|FindObject|GetComponentInChildren|Screen\.|mousePosition|anchoredPosition|NameText|SelectedEntityPanel|Button" Assets/Game/Scripts/Tutorial/Assistant/CommandIntentExecutor.cs Assets/Game/Scripts/Tutorial/Assistant/WarlineCaptureAssistantService.cs`
- Unity EditMode `CommandIntentExecutorTests` with results at `/private/tmp/warlinecapture-command-intent-executor-results.xml`
- Unity EditMode `M01AssistantRuntimeTests` with results at `/private/tmp/warlinecapture-m01-assistant-runtime-results.xml`
- Unity EditMode `M01AssistantCommandRuntimeTests` with results at `/private/tmp/warlinecapture-m01-assistant-command-results.xml`
Validation result:
- `git diff --check` passed.
- UI hierarchy/screen-coordinate grep returned no matches.
- `CommandIntentExecutorTests`: 14/14 passed.
- `M01AssistantRuntimeTests`: 9/9 passed.
- `M01AssistantCommandRuntimeTests`: 10/10 passed.
Known gaps:
- Production still needs live `AssistantContextProvider` wiring to set `TypedCommandHooksAvailable` from actual runtime readiness.
- UI still needs to bind the mounted `AssistantPanelController` to the service/executor handoff and visible ownership/takeover state.
- This pass does not implement full autopilot, new Chapter 1 mechanics, or result-popup closing behavior.
Cross-lane impacts:
- UI can now wire the `Do It` button to `WarlineCaptureAssistantService.ExecuteCurrentDoIt(...)` instead of invoking gameplay through button hierarchy or HUD text.
- QA/HCI can validate the typed command executor path once UI mount and live context provider are connected.
- Gameplay typed hooks remain the command authority for select, move, attack, and M01 build rejection.
Next recommended task:
- Implement the live `AssistantContextProvider` and UI panel binding so `TypedCommandHooksAvailable`, current selection, anchor availability, enemy visibility, and latest command results are sourced from runtime state.
