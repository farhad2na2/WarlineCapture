# WarlineCapture Assistant Panel M01 Implementation Contract

Date: 2026-05-07

## Purpose

This is the implementation contract for `PREFAB-05_AssistantPanel` and the Mission 01 ARIA recommendation states.

It converts the flat visual target:

```text
Design/VisualLock/PREFAB-05_AssistantPanel/PREFAB-05_AssistantPanel_Landscape_Target.png
```

into concrete ids, runtime data fields, command intents, and validation gates for UI, gameplay, and FTUE work.

This contract does not make the flat visual reference a completed implementation asset. The target is a visual guide only; Unity must use live TMP text, reusable WarlineCapture chrome, and runtime data binding.

## Source Contracts

| Contract | Required Use |
|---|---|
| `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md` | ARIA role, takeover rules, assistant architecture, FTUE step plan. |
| `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` | Locked M01 ids, tactical anchors, runtime entities, FTUE step ids, command reasons. |
| `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md` | `BattleHudGameplayBridge`, active mission ids, tactical command modes, current bridge reason codes. |
| `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md` | Assistant panel purpose and gameplay data ownership. |
| `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md` | Runtime service ownership, M01 recommendation state transitions, typed intents, save/session state, and validation tests. |

## Surface Contract

| Field | Value |
|---|---|
| Surface id | `PREFAB-05_AssistantPanel` |
| Prefab path | `Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab` |
| View component | `AssistantPanelView` |
| Visual target | `Design/VisualLock/PREFAB-05_AssistantPanel/PREFAB-05_AssistantPanel_Landscape_Target.png` |
| Visual target type | Flat panel/popup reference over blurred WarlineCapture UI context. |
| Runtime owner | Future `AssistantPanelController` / `WarlineCaptureAssistantService` |
| Gameplay dependency | `BattleHudGameplayBridge` on `Screen_MatchOverlay` |

## Required UI Element Ids

These ids are the canonical handoff names for the UI prefab shell. UI may use Unity child objects with these names or equivalent serialized fields on `AssistantPanelView`, but tests should verify the exposed control contract.

| Id | Type | Current `AssistantPanelView` Field | Required |
|---|---|---|---|
| `PREFAB-05.TitleText` | TMP text | `TitleText` | Yes |
| `PREFAB-05.StatusText` | TMP text | `StatusText` | Yes |
| `PREFAB-05.RecommendationTitleText` | TMP text | `RecommendationTitleText` | Yes |
| `PREFAB-05.RecommendationBodyText` | TMP text | `RecommendationBodyText` | Yes |
| `PREFAB-05.AssistantTabs` | Transform/container | `AssistantTabs` | Yes |
| `PREFAB-05.Tab.Next` | Tab button/text | `TabLabels[0]` or child tab | Yes |
| `PREFAB-05.Tab.Why` | Tab button/text | `TabLabels[1]` or child tab | Yes |
| `PREFAB-05.Tab.Plan` | Tab button/text | `TabLabels[2]` or child tab | Yes |
| `PREFAB-05.Tab.Goals` | Tab button/text | `TabLabels[3]` or child tab | Yes |
| `PREFAB-05.RecommendationChips` | Transform/container | `RecommendationChips` | Yes |
| `PREFAB-05.Chip.Primary` | Chip text | `ChipLabels[0]` | Yes |
| `PREFAB-05.Chip.Tactical` | Chip text | `ChipLabels[1]` | Yes |
| `PREFAB-05.Chip.Risk` | Chip text | `ChipLabels[2]` | Yes |
| `PREFAB-05.ShowMeButton` | Button | `ShowMeButton` | Yes |
| `PREFAB-05.DoItButton` | Button | `DoItButton` | Yes |
| `PREFAB-05.StopButton` | Button | `StopButton` | Yes |
| `PREFAB-05.CloseButton` | Button | Not present yet | Optional first pass |
| `PREFAB-05.Portrait` | Image/portrait slot | Not present yet | Optional first pass |

Minimum first pass: all current `AssistantPanelView` fields must be wired and all text must be live TMP.

## M01 Runtime Ids

These ids are locked by `WarlineCapture_M01_FirstContact_Production_Contract.md` and must be used for M01 recommendations.

| Category | Id |
|---|---|
| Mission | `saga.ch01.m01.first_contact` |
| Scenario setup | `scenario.ch01.m01.first_contact` |
| Level | `level.ch01.district_edge_01` |
| Iso map | `iso.ch01.district_edge_01` |
| Preview art | `preview.ch01.first_contact` |
| Minimap art | `minimap.ch01.first_contact` |
| Player squad | `unit.player.rifle_squad_01` |
| Enemy patrol | `unit.enemy.patrol_01` |
| Move target | `tutorial.move_target.cover_01` |
| Objective anchor | `objective.destroy_patrol_group` |
| Objective result popup | `POP-05_MissionResult` |

FTUE step ids:

| Step Id | Assistant Panel Recommendation State |
|---|---|
| `ftue.m01.objectives` | `M01.ObjectivesIntro` |
| `ftue.m01.select_squad` | `M01.SelectSquad` |
| `ftue.m01.move` | `M01.MoveToCover` |
| `ftue.m01.attack` | `M01.AttackPatrol` |
| `ftue.m01.complete` | `M01.ResultExplain` |

## Recommendation State Model

The runtime data object can be implemented as C#, ScriptableObject data, or a service DTO. It must contain these fields conceptually:

```text
RecommendationId
StepId
MissionId
Title
Body
Reason
Tab
Chips[]
HighlightTargets[]
ShowMePlan
DoItPlan
CanExecute
CanShow
CanStop
BlockingReasonCode
BattleHudSelectionName
BattleHudCommandMode
LastCommandResultReason
ControlOwnerState
```

No field should store raw screen coordinates. Highlight targets and plans must reference UI element ids, runtime entity ids, tactical anchors, objective ids, or popup ids.

## M01 Recommendation States

### `M01.ObjectivesIntro`

| Field | Value |
|---|---|
| Step id | `ftue.m01.objectives` |
| Trigger | Match starts and objective tracker is visible. |
| Title | `Read the objective` |
| Body | `Destroy the hostile patrol and keep the command squad alive.` |
| Primary chip | `Check objective tracker` |
| Highlight target | `BattleHud.ObjectivePanel` |
| Show Me | Pulse `BattleHud.ObjectivePanel`; no camera or command action. |
| Do It | Not executable; disabled or hidden. |
| Stop | Dismiss current assistant recommendation. |
| Completion | Objective panel acknowledged, squad selection begins, or player directly selects squad. |

### `M01.SelectSquad`

| Field | Value |
|---|---|
| Step id | `ftue.m01.select_squad` |
| Trigger | `unit.player.rifle_squad_01` is spawned and no controllable squad is selected. |
| Title | `Select Rifle Squad` |
| Body | `Orders start with selection. Select the highlighted response team.` |
| Primary chip | `Select squad` |
| Tactical chip | `Prepare move order` |
| Highlight target | Runtime entity `unit.player.rifle_squad_01` |
| Show Me | Highlight entity and squad card; optionally focus camera through tactical bounds. |
| Do It | Execute typed intent `SelectRuntimeEntity(unit.player.rifle_squad_01)`. |
| Stop | Cancel highlight/takeover and leave player input unchanged. |
| Bridge dependency | Selection result should update `BattleHudGameplayBridge.ApplySelection("RIFLE SQUAD 01", status)`. |
| Completion | `BattleHudGameplayBridge` has selected entity state, or gameplay selection state reports the squad selected. |

### `M01.MoveToCover`

| Field | Value |
|---|---|
| Step id | `ftue.m01.move` |
| Trigger | `unit.player.rifle_squad_01` is selected and `tutorial.move_target.cover_01` is valid. |
| Title | `Move to cover` |
| Body | `Move the squad to the marked cover point before patrol contact.` |
| Primary chip | `Move to cover` |
| Tactical chip | `Use MOVE` |
| Risk chip | `Patrol approaching` |
| Highlight target | Tactical anchor `tutorial.move_target.cover_01` |
| Show Me | Show path preview from selected squad to `tutorial.move_target.cover_01`. |
| Do It | Execute typed intent `MoveSelectedUnits(tutorial.move_target.cover_01)`. |
| Stop | Cancel highlight/path preview and clear assistant control owner. |
| Bridge dependency | Move targeting should call `BattleHudGameplayBridge.ApplyCommandMode(TacticalCommandMode.Move)` while active and `ClearCommandMode()` when complete/cancelled. |
| Completion | Move command accepted, destination marker shown, or squad reaches/approaches the cover anchor. |

### `M01.AttackPatrol`

| Field | Value |
|---|---|
| Step id | `ftue.m01.attack` |
| Trigger | Enemy patrol visible and selected squad can attack. |
| Title | `Attack hostile patrol` |
| Body | `Focus the hostile patrol before it reaches the civilian block.` |
| Primary chip | `Attack patrol` |
| Tactical chip | `Use ATTACK` |
| Risk chip | `Protect civilians` |
| Highlight target | Runtime entity `unit.enemy.patrol_01`; fallback objective anchor `objective.destroy_patrol_group`. |
| Show Me | Highlight patrol and objective marker; show attack focus ring. |
| Do It | Execute typed intent `AttackTarget(unit.enemy.patrol_01)`. |
| Stop | Cancel highlight/takeover and return to player input. |
| Bridge dependency | Attack targeting should call `BattleHudGameplayBridge.ApplyCommandMode(TacticalCommandMode.Attack)` while active and `ClearCommandMode()` when complete/cancelled. |
| Completion | Attack command accepted, enemy patrol destroyed, or objective completion starts. |

### `M01.InvalidCommandRecovery`

| Field | Value |
|---|---|
| Step id | Contextual recovery, not a required FTUE step. |
| Trigger | `BattleHudGameplayBridge.ApplyCommandResult` receives a rejected `TacticalCommandResult`. |
| Title | `Command blocked` |
| Body | Use bridge reason text, then recommend the nearest valid next M01 action. |
| Primary chip | Depends on reason code. |
| Highlight target | Attempted target if valid for feedback, otherwise current required FTUE target. |
| Show Me | Highlight valid replacement target or required selection target. |
| Do It | Only enabled when a safe typed replacement intent exists. |
| Stop | Dismiss recovery card/panel; does not retry command. |
| Completion | Player issues valid command, selects squad, dismisses recommendation, or moves to next step. |

Canonical M01 reason codes from `BattleHudGameplayBridge` / `TacticalCommandResult`:

| Reason Code | Assistant Direction |
|---|---|
| `NoSelection` | Recommend `M01.SelectSquad`. |
| `TargetOutOfBounds` | Recommend the active valid target or objective anchor for the current step. |
| `TargetBlocked` | Recommend `M01.MoveToCover` with valid anchor `tutorial.move_target.cover_01`. |
| `TargetUnreachable` | Recommend `M01.MoveToCover` or focus the nearest valid objective anchor. |
| `TargetNotEnemy` | Recommend `M01.AttackPatrol` and highlight `unit.enemy.patrol_01` if visible. |
| `TargetNotAttackable` | Recommend the valid current attack target or objective anchor. |
| `CommandUnavailable` | Show plain reason and recommend the next valid M01 action. |
| `MissionDoesNotAllowBuild` | Explain `Building unlocks in the next mission`; keep `Do It` disabled. |
| `CameraJumpUnavailable` | Explain no valid map focus exists; keep gameplay `Do It` disabled. |

Earlier bridge aliases (`InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`) are deprecated for M01 and must not be used in Gate 4 assertions. Later-mission codes (`InsufficientResources`, `AbilityOnCooldown`, `TransportUnavailable`) are out of M01 scope unless explicitly marked future/non-M01.

### `M01.ResultExplain`

| Field | Value |
|---|---|
| Step id | `ftue.m01.complete` |
| Trigger | Victory condition starts result flow. |
| Title | `Mission complete` |
| Body | `The patrol is destroyed. The result screen shows stars, rewards, and city impact.` |
| Primary chip | `Read result` |
| Highlight target | `POP-05_MissionResult` |
| Show Me | Highlight result stars/reward rows when popup is open. |
| Do It | Not executable; disabled or hidden. |
| Stop | Dismiss assistant explanation only, not the result popup. |
| Completion | Result popup acknowledged or next route loads. |

## Data Dependencies

### From Gameplay / Mission Runtime

| Data | Source |
|---|---|
| Active mission ids | `WarlineCaptureMissionSession.ActiveMissionId`, `ActiveScenarioSetupId`, `ActiveLevelId`, `ActiveIsoMapId`, `ActiveMapPreviewArtId`, `ActiveMinimapArtId` |
| Selected entity name/status | Gameplay selection state, bridged through `BattleHudGameplayBridge.ApplySelection` |
| Current command mode | `BattleHudGameplayBridge.ApplyCommandMode` / `ClearCommandMode` |
| Last command result | `BattleHudGameplayBridge.ApplyCommandResult(TacticalCommandResult)` |
| World marker visibility | `BattleHudGameplayBridge.SetWorldMarkersVisible` |
| Tactical anchor availability | `TacticalMapRuntimeLoader.TryGetAnchorWorldPosition` or equivalent tactical map definition lookup |
| Runtime entity availability | Selection/combat runtime ids for `unit.player.rifle_squad_01` and `unit.enemy.patrol_01` |
| Objective progress | `objective.destroy_patrol_group` / M01 objective completion |

### From FTUE / Assistant Runtime

| Data | Source |
|---|---|
| Assistance level | Future tutorial save data / assistant settings |
| Completed step ids | Future `TutorialSaveData.completedStepIds` |
| Current recommendation id | `AssistantRecommendationService` |
| Control owner state | `AssistantControlOwner` |
| Show Me plan | `AssistantRecommendation.OptionalShowMePlan` |
| Do It plan | `AssistantRecommendation.OptionalDoItPlan` |
| Dismissed recommendation ids | Future tutorial save data |

## Show Me / Do It / Stop Behavior

### `Show Me`

- Never executes a gameplay command.
- May pulse UI element ids, runtime entities, objective anchors, or tactical anchors.
- May focus camera only through typed tactical context and camera bounds.
- Must not use raw screen coordinates.
- For `M01.MoveToCover`, may show a path preview to `tutorial.move_target.cover_01`.
- For `M01.AttackPatrol`, may pulse `unit.enemy.patrol_01` and `objective.destroy_patrol_group`.

### `Do It`

- Requires explicit player tap on `PREFAB-05.DoItButton`.
- Executes exactly one bounded typed intent, then yields control.
- Valid M01 intents:
  - `SelectRuntimeEntity(unit.player.rifle_squad_01)`
  - `MoveSelectedUnits(tutorial.move_target.cover_01)`
  - `AttackTarget(unit.enemy.patrol_01)`
- Must not start a full mission, spend premium resources, accept purchases, change settings, or complete the mission unattended.
- Must fail visibly and return control if required runtime ids or anchors are missing.

### `Stop`

- Always visible when `Show Me`, `Do It`, preview, or takeover is active.
- Cancels highlight, path preview, takeover, and any pending assistant control owner state.
- Must not cancel the player's already-issued normal command unless the player explicitly cancels that command through gameplay controls.
- Should call assistant control owner transition to `Player` / clear active plan.

## Player Input Cancellation

During `AssistantTakeover`, any pointer/touch/keyboard/gamepad input outside the assistant panel must:

1. Transition `AssistantControlOwner` to `PlayerOverridePending`.
2. Stop any pending assistant intent after the current atomic action boundary.
3. Hide or downgrade the takeover banner.
4. Leave the player in `Player` control.
5. Keep `PREFAB-05.StopButton` available until the state is fully cleared.

## BattleHudGameplayBridge Dependency

`PREFAB-05_AssistantPanel` must read or react to battle HUD state through a service/controller boundary, but the source truth for live HUD feedback remains `BattleHudGameplayBridge`.

Required dependency points:

| Assistant Need | Bridge / Runtime Contract |
|---|---|
| Know if a squad is selected | Selection system calls `ApplySelection` / `ClearSelection`; assistant context mirrors selection state. |
| Know active move/attack targeting | Gameplay/UI command entry calls `ApplyCommandMode(TacticalCommandMode.Move/Attack)` and clears on exit. |
| Recover from invalid command | Gameplay calls `ApplyCommandResult` with rejected `TacticalCommandResult`; assistant reads reason for recovery state. |
| Show/hide world markers | Use assistant highlight controller and/or bridge-owned marker visibility, not baked target pixels. |
| Avoid child UI coupling | Gameplay and assistant must not write directly to `SelectedEntityPanel/NameText` or other child paths. |

Current known bridge gap from PM review: Hold, Stop, Build, and Special command mode wiring may still be in progress in the gameplay lane. `PREFAB-05` M01 behavior can proceed with Select, Move, Attack, and invalid-command recovery, but should not claim final coverage for Hold/Stop/Build/Special until the gameplay report lands.

## Asset Register Implications

- The flat visual target for `PREFAB-05_AssistantPanel` is not a final implementation asset.
- Do not mark ARIA portrait, assistant button icon, assistant panel frame, chips, or tutorial highlight art as complete based only on the flat reference.
- If UI ships the first prefab shell using existing WarlineCapture chrome and live TMP, asset rows can remain `missing` or `exists_needs_review` until production art is approved.
- Required future asset ids remain:
  - ARIA portrait for panel/tutorial card.
  - ARIA assistant button icon / radio waveform mark.
  - Assistant button state set.
  - Assistant panel frame and recommendation chip states.
  - Tutorial highlight ring/path/blocked feedback treatment.

## Acceptance Checks

### UI Acceptance

- `PREFAB-05_AssistantPanel.prefab` exists at the contracted path.
- `AssistantPanelView` is present and all required fields are serialized.
- `TitleText`, `StatusText`, `RecommendationTitleText`, `RecommendationBodyText`, tab labels, chip labels, and button labels are live TMP, not baked into sprites.
- `ShowMeButton`, `DoItButton`, and `StopButton` are visible and interactable according to the active recommendation state.
- The panel can bind at least one M01 recommendation through `BindRecommendation(title, body, chips)`.
- The prefab does not require a sliced layer pack from the flat visual target.

### Gameplay / FTUE Acceptance

- M01 active ids match the locked contract.
- `Show Me` and `Do It` use typed ids from this contract and never screen coordinates.
- `M01.SelectSquad`, `M01.MoveToCover`, `M01.AttackPatrol`, and `M01.InvalidCommandRecovery` can be produced from live gameplay state.
- `BattleHudGameplayBridge` receives selection, Move, Attack, and rejected-command feedback during the relevant M01 steps.
- Any `Do It` action yields control after one bounded intent.
- Any player input during takeover cancels or pauses assistant control and returns player control.

### Validation Commands

Recommended focused validation after UI/runtime implementation:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-match-overlay-results.xml -logFile /private/tmp/warlinecapture-match-overlay.log

"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter BattleHudGameplayBridgeConnectionTests -testResults /private/tmp/warlinecapture-battlehud-bridge-results.xml -logFile /private/tmp/warlinecapture-battlehud-bridge.log
```

Add a focused assistant panel test once the prefab shell lands:

```text
AssistantPanel_HasRequiredButtonsAndLiveText
AssistantPanel_BindsM01RecommendationWithoutCoordinateTargets
AssistantPanel_ShowMeDoItStopButtonsExposeExpectedStates
```

## Non-Goals For This Contract

- Implementing final ARIA runtime services.
- Implementing final ARIA voice/portrait art.
- Implementing a full AI player or unattended mission autopilot.
- Replacing the Battle HUD bridge or writing gameplay directly into child UI objects.
- Defining screen coordinates for ARIA actions.
