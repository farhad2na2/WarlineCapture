Lane:
Gameplay

Task:
P0 assistant-facing typed command runtime hooks for M01.

Files changed:
- Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRuntime.cs
- Assets/Game/Scripts/Tutorial/Assistant/M01AssistantCommandRuntime.cs.meta
- Assets/Game/Scripts/UI/RTSSelectionSystem.cs
- Assets/Tests/Editor/Tutorial/M01AssistantCommandRuntimeTests.cs
- Assets/Tests/Editor/Tutorial/M01AssistantCommandRuntimeTests.cs.meta

Contracts touched:
- Added gameplay-owned M01 assistant hooks: `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`.
- Assistant typed execution is bounded to M01 command squad selection, move-to-cover anchor, enemy patrol attack, and safe rejection behavior.
- Hooks resolve stable M01 runtime ids and anchors, then route through gameplay selection, move, attack, and `BattleHudGameplayBridge` command-result feedback.
- M01 build rejection is exposed as a typed `TacticalCommandResult` with `MissionDoesNotAllowBuild`.
- ARIA still must not click UI buttons, use screen coordinates, or reach through child transform paths.

User-visible behavior:
- ARIA/FTUE can select `unit.player.rifle_squad_01` through gameplay selection state.
- ARIA/FTUE can issue a move order to `tutorial.move_target.cover_01` through normal selected-unit move components.
- ARIA/FTUE can issue an attack order against `unit.enemy.patrol_01` through normal `EngageTarget` command state.
- Invalid ids, missing selection, missing anchors, dead squad, dead patrol, and M01 build attempts return rejected command results with bridge-compatible reason codes.

Validation run:
- Unity EditMode: M01AssistantCommandRuntimeTests
- Unity EditMode: Chapter01M01PlayableRuntimeTests
- Unity EditMode: Chapter01TacticalRuntimeBindingTests
- Unity EditMode: BattleHudGameplayBridgeConnectionTests
- Unity EditMode: WarlineCaptureCampaignObjectiveTests
- Unity PlayMode: Chapter01M01PlayModeValidationTests

Validation result:
- M01AssistantCommandRuntimeTests: 10/10 passed, /private/tmp/warlinecapture-m01-assistant-command-results.xml
- Chapter01M01PlayableRuntimeTests: 7/7 passed, /private/tmp/warlinecapture-m01-playable-results.xml
- Chapter01TacticalRuntimeBindingTests: 4/4 passed, /private/tmp/warlinecapture-chapter01-runtime-binding-results.xml
- BattleHudGameplayBridgeConnectionTests: 6/6 passed, /private/tmp/warlinecapture-battlehud-bridge-results.xml
- WarlineCaptureCampaignObjectiveTests: 7/7 passed, /private/tmp/warlinecapture-campaign-objective-results.xml
- Chapter01M01PlayModeValidationTests: 3/3 passed, /private/tmp/warlinecapture-m01-playmode-results.xml

Known gaps:
- The typed hooks do not implement full assistant recommendation state, Show Me highlighting, path preview, control ownership, or step persistence; they provide the gameplay command execution layer those systems can call.
- PlayMode still logs the known `EntitiesGraphicsSystemUtility.RootsHandlerDelegate` `NullReferenceException`, preview-scene leak warning, and `RuntimeCitySpawner` hitch. They did not block validation and appear to be editor/headless test cleanup plus initial scene-spawn cost, not typed-command regressions. They should remain a cleanup/perf task before balance QA or device-readability signoff.

Cross-lane impacts:
- UI can wire `PREFAB-05.DoItButton` to typed intents without child-path or coordinate command execution.
- Support/FTUE can call the gameplay hooks against locked M01 ids and reason-code results.
- QA/HCI can validate assistant selection, move, attack, and build-lock recovery as typed runtime behavior instead of UI click automation.

Next recommended task:
Support/FTUE should connect `CommandIntentExecutor` and assistant recommendation Do It actions to these gameplay hooks, then UI should validate button states and ownership feedback against the live assistant panel.
