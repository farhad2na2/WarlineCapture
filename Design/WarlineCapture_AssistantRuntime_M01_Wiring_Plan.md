# WarlineCapture Assistant Runtime M01 Wiring Plan

Date: 2026-05-07

## Purpose

This is the runtime wiring contract for the Mission 01 ARIA assistant recommendation flow.

It sits between:

- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md`

The goal is to remove runtime guesswork for the first playable assistant path: objectives intro, select squad, move to cover, attack patrol, invalid command recovery, and result explanation.

This is a contract only. It does not add Chapter 1 mechanics, does not approve art assets, and does not make ARIA an autopilot.

## Runtime Ownership

Create the runtime assistant module under the existing planned namespace folders:

```text
Assets/Game/Scripts/Tutorial
Assets/Game/Scripts/Tutorial/Assistant
Assets/Game/Scripts/Tutorial/Recommendations
Assets/Game/Scripts/Tutorial/Control
Assets/Game/Scripts/Tutorial/Data
Assets/Game/Configs/Tutorial
```

| Type | Owner | Responsibility |
|---|---|---|
| `WarlineCaptureAssistantService` | FTUE/runtime | Owns active assistance level, current assistant context, current recommendation, active preview/takeover state, and panel open state. |
| `AssistantContextProvider` | FTUE/runtime | Reads live mission, objective, selection, command feedback, result, route, and tutorial save state into one immutable context snapshot. |
| `M01AssistantRecommendationProvider` | FTUE/runtime | Produces the M01 recommendation state from the context snapshot and save/session flags. |
| `AssistantPanelController` | UI/runtime | Binds `AssistantRecommendation` into `AssistantPanelView`, wires `ShowMeButton`, `DoItButton`, and `StopButton`, and never talks directly to gameplay child UI paths. |
| `AssistantHighlightController` | UI/runtime | Shows UI pulses, world highlights, path previews, objective focus, and result-popup focus from typed targets only. |
| `CommandIntentExecutor` | Gameplay/FTUE boundary | Executes approved typed intents through real gameplay APIs and returns an accepted/rejected result. |
| `AssistantControlOwner` | FTUE/runtime | Tracks `Player`, `Guided`, `AssistantPreview`, `AssistantTakeover`, and `PlayerOverridePending`, including player-input cancellation. |
| `TutorialSessionState` | FTUE/runtime | Tracks in-session M01 step completion and dismissed recommendation ids before save persistence is available. |
| `TutorialSaveData` | Save/runtime | Persists assistance level, completed tutorial step ids, dismissed recommendations, and replay suppression. |

## Data Flow

```text
WarlineCaptureMissionSession
Chapter01M01PlayableRuntime
Objective/runtime result state
RTSSelectionSystem / selected entity state
BattleHudGameplayBridge command feedback
TacticalMapRuntimeLoader anchors
TutorialSaveData / TutorialSessionState
        |
        v
AssistantContextProvider.BuildContext()
        |
        v
M01AssistantRecommendationProvider.Evaluate(context)
        |
        v
WarlineCaptureAssistantService.SetRecommendation(...)
        |
        v
AssistantPanelController.Bind(...)
        |
        v
AssistantPanelView.BindRecommendation(title, body, chips, canShow, canExecute, canStop)
```

`AssistantPanelView` is a passive view. It should receive title/body/chips/button availability only. It should not query mission ids, execute gameplay, inspect ECS entities, or know M01 step rules.

## Context Snapshot

`AssistantContextProvider` must expose these conceptual fields for M01:

```text
ActiveRoute
MissionId
ScenarioSetupId
LevelId
IsoMapId
MapPreviewArtId
MinimapArtId
IsM01Active
IsMatchOverlayActive
ObjectivePanelVisible
ResultPopupVisible
CommandSquadSpawned
CommandSquadSelected
CommandSquadAlive
EnemyPatrolSpawned
EnemyPatrolVisible
EnemyPatrolDestroyed
MoveTargetAvailable
MoveCommandAccepted
AttackCommandAccepted
LastCommandResultAccepted
LastCommandReasonCode
LastCommandReasonText
CurrentCommandMode
CurrentControlOwnerState
CompletedStepIds
DismissedRecommendationIds
AssistanceLevel
```

M01 ids are locked:

| Field | Id |
|---|---|
| Mission | `saga.ch01.m01.first_contact` |
| Scenario setup | `scenario.ch01.m01.first_contact` |
| Level | `level.ch01.district_edge_01` |
| Iso map | `iso.ch01.district_edge_01` |
| Player squad | `unit.player.rifle_squad_01` |
| Enemy patrol | `unit.enemy.patrol_01` |
| Move target | `tutorial.move_target.cover_01` |
| Objective anchor | `objective.destroy_patrol_group` |
| Result popup | `POP-05_MissionResult` |

## Recommendation DTO

The runtime DTO should match the panel implementation contract and add explicit action routing:

```text
RecommendationId
StepId
MissionId
Priority
Title
Body
Reason
Tab
Chips[]
HighlightTargets[]
ShowMeIntent
DoItIntent
CanShow
CanExecute
CanStop
BlockingReasonCode
CompletionRule
SuppressAfterCompletion
```

`HighlightTargets`, `ShowMeIntent`, and `DoItIntent` must use typed ids. Do not store raw screen coordinates or click positions.

## M01 State Transition Order

`M01.InvalidCommandRecovery` is an overlay recommendation. It can temporarily override the base state when the latest command result is rejected, but it must not mark required FTUE steps complete by itself.

| Order | State | Step Id | Enters When | Completes When | Next Base State |
|---:|---|---|---|---|---|
| 1 | `M01.ObjectivesIntro` | `ftue.m01.objectives` | M01 match overlay is active and objective panel is visible. | Objective acknowledged, squad selected, or player issues a valid selection/action that implies progress. | `M01.SelectSquad` |
| 2 | `M01.SelectSquad` | `ftue.m01.select_squad` | M01 command squad exists and no controllable squad is selected. | `unit.player.rifle_squad_01` is selected or `BattleHudGameplayBridge.ApplySelection` has selected squad feedback. | `M01.MoveToCover` |
| 3 | `M01.MoveToCover` | `ftue.m01.move` | Command squad is selected, alive, and `tutorial.move_target.cover_01` resolves. | Move order is accepted, destination marker/path state is active, or squad reaches/approaches the cover anchor. | `M01.AttackPatrol` |
| 4 | `M01.AttackPatrol` | `ftue.m01.attack` | Enemy patrol is visible/alive and command squad can attack. | Attack order accepted, enemy patrol destroyed, or objective completion begins. | `M01.ResultExplain` |
| 5 | `M01.ResultExplain` | `ftue.m01.complete` | M01 victory/result flow starts or `POP-05_MissionResult` is open. | Result popup acknowledged or route leaves result flow. | None |

Skip-forward rule: if a player naturally performs a later valid action, mark earlier implied steps complete in session state. Example: if the player selects the squad and attacks the patrol without using `Do It`, complete select and attack, and complete move only if the move command or cover approach condition actually occurred.

Replay rule: completed step ids suppress full tutorial cards, but the assistant may still provide contextual recommendations unless the recommendation id was dismissed or assistance is muted.

## Invalid Command Recovery

`BattleHudGameplayBridge.ApplyCommandResult` is the visible HUD feedback source. The assistant should mirror the latest rejected `TacticalCommandResult` through the context provider and produce a recovery card when it can help.

| Reason Code | Assistant State | Recommendation |
|---|---|---|
| `NoSelection` | `M01.InvalidCommandRecovery` | Recommend `M01.SelectSquad`; `Do It` selects `unit.player.rifle_squad_01` if spawned. |
| `TargetOutOfBounds` | `M01.InvalidCommandRecovery` | Keep current base state and highlight the valid typed target or objective anchor for that state. |
| `TargetBlocked` | `M01.InvalidCommandRecovery` | Recommend `M01.MoveToCover`; `Show Me` previews `tutorial.move_target.cover_01` if the anchor is valid. |
| `TargetUnreachable` | `M01.InvalidCommandRecovery` | Recommend `M01.MoveToCover`; `Show Me` previews `tutorial.move_target.cover_01` or the nearest valid objective anchor. |
| `TargetNotEnemy` | `M01.InvalidCommandRecovery` | Recommend `M01.AttackPatrol`; `Show Me` highlights `unit.enemy.patrol_01` if visible. |
| `TargetNotAttackable` | `M01.InvalidCommandRecovery` | Keep current base state and highlight the valid attack target or objective anchor. |
| `CommandUnavailable` | `M01.InvalidCommandRecovery` | Explain the command is unavailable for the selected unit; recommend the next valid M01 action. |
| `MissionDoesNotAllowBuild` | `M01.InvalidCommandRecovery` | Explain building unlocks later; disable `Do It`; offer `Show Me` for active M01 objective. |
| `CameraJumpUnavailable` | `M01.InvalidCommandRecovery` | Explain no valid map focus exists; keep gameplay `Do It` disabled and leave player control unchanged. |

Canonical M01 reason codes come from `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`. Do not use earlier bridge aliases (`InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`) in M01 Gate 4 assertions or ARIA recovery logic. Later-mission codes (`InsufficientResources`, `AbilityOnCooldown`, `TransportUnavailable`) are out of M01 scope unless a future mission explicitly enables them.

Recovery must clear or downgrade when the player issues a valid command, dismisses the recommendation, changes selection to the expected unit, or advances to a later M01 state.

## Typed Intent Model

Use one typed intent DTO for `Show Me`, `Do It`, and `Stop` operations:

```text
AssistantIntent
IntentId
IntentKind
MissionId
StepId
TargetType
TargetId
CommandMode
CanExecuteGameplay
RequiresSelectedEntity
RequiresVisibleTarget
CompletionBoundary
```

| Intent Kind | Purpose | Executes Gameplay Command |
|---|---|---|
| `FocusUiElement` | Pulse a UI element such as `BattleHud.ObjectivePanel` or `POP-05_MissionResult`. | No |
| `FocusRuntimeEntity` | Highlight/focus `unit.player.rifle_squad_01` or `unit.enemy.patrol_01`. | No |
| `PreviewPathToAnchor` | Show a path preview to `tutorial.move_target.cover_01`. | No |
| `FocusObjectiveAnchor` | Highlight `objective.destroy_patrol_group`. | No |
| `SelectRuntimeEntity` | Select `unit.player.rifle_squad_01`. | Yes |
| `MoveSelectedUnits` | Move selected squad to `tutorial.move_target.cover_01`. | Yes |
| `AttackTarget` | Attack `unit.enemy.patrol_01`. | Yes |
| `StopAssistantControl` | Cancel preview/takeover and return ownership to player. | No normal gameplay command |

No assistant intent may start a mission, spend resources, accept purchases, change Settings, delete saves, or complete M01 unattended.

## Required Gameplay APIs / Intents

Existing gameplay APIs can stay as they are, but `CommandIntentExecutor` needs stable wrappers so FTUE does not call random UI child buttons.

| Assistant Intent | Required Runtime Hook |
|---|---|
| `SelectRuntimeEntity(unit.player.rifle_squad_01)` | Resolve M01 runtime entity id to live controllable squad, then use selection system selection API. If no public API exists, gameplay should add `TrySelectRuntimeEntity(string runtimeEntityId)`. |
| `MoveSelectedUnits(tutorial.move_target.cover_01)` | Resolve anchor through `TacticalMapRuntimeLoader.TryGetAnchorWorldPosition`, then issue the normal selected-unit move command. If no public API exists, gameplay should add `TryIssueMoveToAnchor(string anchorId)`. |
| `AttackTarget(unit.enemy.patrol_01)` | Resolve target entity id to live hostile patrol, then issue normal selected-unit attack command. If no public API exists, gameplay should add `TryIssueAttackTarget(string runtimeEntityId)`. |
| `FocusObjectiveAnchor(objective.destroy_patrol_group)` | Resolve objective anchor and ask highlight/camera systems to focus within tactical camera bounds. |
| `StopAssistantControl` | Clear assistant preview/takeover state, stop ARIA-owned highlight/path preview, and leave normal player commands untouched. |

Each gameplay wrapper returns a result that can be translated to `TacticalCommandResult` and displayed through `BattleHudGameplayBridge.ApplyCommandResult` when rejected.

## Show Me / Do It / Stop Rules

| State | Show Me | Do It | Stop |
|---|---|---|---|
| `M01.ObjectivesIntro` | Enabled; pulse `BattleHud.ObjectivePanel`. | Disabled. | Enabled only while highlight is active. |
| `M01.SelectSquad` | Enabled when squad exists; highlight squad and squad card. | Enabled when squad exists and controllable. | Enabled during highlight/takeover. |
| `M01.MoveToCover` | Enabled when squad selected and anchor resolves; preview path. | Enabled when squad selected and anchor resolves. | Enabled during preview/takeover. |
| `M01.AttackPatrol` | Enabled when patrol exists/visible; highlight patrol/objective. | Enabled when selected squad can attack patrol. | Enabled during highlight/takeover. |
| `M01.InvalidCommandRecovery` | Enabled when a valid replacement target exists. | Enabled only when a safe replacement intent exists. | Enabled while recovery card/highlight is active. |
| `M01.ResultExplain` | Enabled when result popup is open; focus stars/rewards rows. | Disabled. | Enabled only for the assistant explanation; must not close the result popup. |

Button binding:

```text
AssistantPanelView.BindRecommendation(title, body, chips, canShow, canExecute, canStop)
```

`canStop` should be `true` only while ARIA owns an active preview, takeover, or recovery highlight. The `StopButton` must remain visible and become interactable during those states.

## Control Ownership

`AssistantControlOwner` state transitions:

| From | Trigger | To |
|---|---|---|
| `Player` | Recommendation shown without preview. | `Guided` |
| `Guided` | Player taps `Show Me`. | `AssistantPreview` |
| `Guided` or `AssistantPreview` | Player taps `Do It`. | `AssistantTakeover` |
| `AssistantPreview` | Player taps `Stop` or preview completes. | `Player` |
| `AssistantTakeover` | One bounded intent completes or fails. | `Player` |
| `AssistantTakeover` | Player input outside assistant panel. | `PlayerOverridePending`, then `Player` at the next atomic boundary. |
| Any assistant-owned state | Player pauses, disables assistance, route changes, or mission ends. | `Player` |

Takeover must show the `POP-10 Assistant Takeover` ownership banner or equivalent visible state. Any pointer/touch/keyboard/gamepad input outside the assistant panel cancels or pauses ARIA control and returns the player to command.

## Save And Session Fields

First runtime pass may use in-memory session state, but persistence needs these fields under future `TutorialSaveData`:

```text
assistanceLevel
completedStepIds[]
dismissedRecommendationIds[]
chapterOneFtueComplete
assistantMuted
takeoverUseCount
lastCompletedMissionId
```

M01 session-only fields:

```text
activeRecommendationId
activePreviewIntentId
activeTakeoverIntentId
lastRejectedReasonCode
lastRejectedAtStepId
m01MoveCommandAccepted
m01AttackCommandAccepted
m01ResultExplained
```

Do not replay a completed M01 tutorial step during the same session or after persistence. Do allow the assistant panel to answer "what now?" with a lower-priority recommendation after the tutorial step is complete.

## Validation Tests

Add focused tests before calling the M01 assistant runtime complete:

| Test | Required Proof |
|---|---|
| `M01AssistantRecommendationProvider_ObjectivesIntroStartsWhenMatchObjectiveVisible` | Active M01 + objective panel context produces `M01.ObjectivesIntro`. |
| `M01AssistantRecommendationProvider_SelectSquadWhenNoSelection` | Spawned unselected squad produces `M01.SelectSquad` with `SelectRuntimeEntity` Do It intent. |
| `M01AssistantRecommendationProvider_MoveAfterSelection` | Selected squad + valid cover anchor produces `M01.MoveToCover` with path preview and move intent. |
| `M01AssistantRecommendationProvider_AttackWhenPatrolVisible` | Visible hostile patrol produces `M01.AttackPatrol` with attack intent. |
| `M01AssistantRecommendationProvider_InvalidNoSelectionRecoversToSelect` | `NoSelection` rejection produces recovery recommendation and does not complete a step. |
| `M01AssistantRecommendationProvider_BuildRejectedExplainsMissionLock` | `MissionDoesNotAllowBuild` disables `Do It` and explains the M01 build lock. |
| `CommandIntentExecutor_RejectsCoordinateTargets` | Intent executor rejects or cannot construct raw screen-coordinate targets. |
| `CommandIntentExecutor_OneBoundedIntentThenYields` | Each `Do It` action completes one selection/move/attack intent and returns owner to player. |
| `AssistantPanelController_BindsM01RecommendationButtonStates` | Calls `BindRecommendation(..., canShow, canExecute, canStop)` with the expected availability per state. |
| `AssistantControlOwner_PlayerInputCancelsTakeover` | Input outside the assistant panel during takeover moves to player override and then player control. |
| `M01AssistantRuntime_DoesNotReplayCompletedSteps` | Completed step ids suppress the same tutorial step on re-entry. |
| `M01AssistantRuntime_ResultExplainDoesNotClosePopup` | Stop on result explanation dismisses assistant state only, not `POP-05_MissionResult`. |

Recommended validation command once implementation exists:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter M01AssistantRuntimeTests -testResults /private/tmp/warlinecapture-m01-assistant-runtime-results.xml -logFile /private/tmp/warlinecapture-m01-assistant-runtime.log
```

## First Service Slice Status

Implemented first pass:

- `WarlineCaptureAssistantService` owns current recommendation state and in-session tutorial state.
- `M01AssistantRecommendationProvider` produces read-only M01 recommendations for objectives intro, select squad, move to cover, attack patrol, invalid command recovery, and result explanation.
- `AssistantContext` is the current explicit snapshot boundary for live route/mission/objective/selection/command-result state.
- `AssistantIntent` and `AssistantRecommendation` carry typed ids only. They do not carry screen coordinates or execute gameplay.
- `TutorialSessionState` tracks completed step ids, dismissed recommendation ids, active recommendation id, active preview/takeover ids, last rejected command, and M01 move/attack/result session flags.
- `WarlineCaptureAssistantService.CreatePresentationData()` converts the current recommendation into `AssistantPanelPresentationData` for `AssistantPanelController`.
- `CommandIntentExecutor` executes approved M01 `Do It` intents through accepted typed gameplay hooks and records accepted/rejected session outcomes.
- `AssistantContextProvider` builds the live M01 context snapshot from `WarlineCaptureMissionSession`, ECS runtime ids, `TacticalMapRuntimeLoader` anchors, `BattleHudGameplayBridge` command feedback, and `WarlineCaptureMatchResultFlow` result visibility.

Recommendations' `Do It` availability remains gated by `AssistantContext.TypedCommandHooksAvailable`, now sourced from runtime readiness rather than test-authored snapshots.

Remaining implementation checklist:

| Owner | Item | Status |
|---|---|---|
| Gameplay | Add or confirm `TrySelectRuntimeEntity(string runtimeEntityId)`. | Accepted: `M01AssistantCommandRuntime.TrySelectRuntimeEntity` selects `unit.player.rifle_squad_01`. |
| Gameplay | Add or confirm `TryIssueMoveToAnchor(string anchorId)`. | Accepted: `M01AssistantCommandRuntime.TryIssueMoveToAnchor` moves to `tutorial.move_target.cover_01`. |
| Gameplay | Add or confirm `TryIssueAttackTarget(string runtimeEntityId)`. | Accepted: `M01AssistantCommandRuntime.TryIssueAttackTarget` attacks `unit.enemy.patrol_01`. |
| Gameplay/UI | Expose selection state for `unit.player.rifle_squad_01` without reading HUD text. | Implemented through ECS `MissionRuntimeEntityId` + `SelectedUnitTag` context mapping. |
| Gameplay/UI | Expose enemy visible/attackable state for `unit.enemy.patrol_01`. | Implemented through ECS runtime id, faction, transform, and health context mapping. |
| UI | Bind `WarlineCaptureAssistantService` output into the mounted `AssistantPanelController`. | Needed to replace placeholder panel content. |
| UI | Add first visible ownership state or `POP-10 Assistant Takeover` mount. | Needed before `canStop`/takeover UX is user-facing complete. |
| Support/FTUE | Implement live `AssistantContextProvider` from mission/session/objective/selection/bridge state. | Implemented: live mission ids, route/match state, objective panel visibility, typed readiness, selection, anchor, patrol, command result, move, and attack state are mapped. |
| Support/FTUE | Add command intent executor boundary once gameplay wrappers exist. | Implemented: `CommandIntentExecutor` routes select, move, attack, stop, and M01 build rejection through typed boundaries. |
| QA/HCI | Run integrated player-route validation once service is mounted and context is live. | Blocked until UI mount plus gameplay wrappers are connected. |

## Open Questions / Blockers

| Topic | Decision Needed |
|---|---|
| Public selection/move/attack wrappers | Gameplay should confirm whether existing `RTSSelectionSystem` APIs are sufficient or add `TrySelectRuntimeEntity`, `TryIssueMoveToAnchor`, and `TryIssueAttackTarget`. |
| Selection state source | UI/gameplay should expose whether `unit.player.rifle_squad_01` is selected without requiring assistant code to inspect HUD text. |
| Enemy visibility source | Gameplay should expose whether `unit.enemy.patrol_01` is visible/attackable for M01 recommendation gating. |
| Objective/result events | Runtime should expose objective complete and result-popup-open state so ARIA does not infer result timing from route text. |
| Takeover banner first pass | UI should confirm whether `POP-10 Assistant Takeover` exists yet or whether the first pass uses a temporary in-panel visible ownership state. |

## Non-Goals

- Full mission autopilot.
- New Chapter 1 mechanics beyond M01 select, move, attack, invalid command recovery, objective explanation, and result explanation.
- Screen-coordinate click automation.
- Asset approval or asset-register completion.
- Replacing `BattleHudGameplayBridge`.
