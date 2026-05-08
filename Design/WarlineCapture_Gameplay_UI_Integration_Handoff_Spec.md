# WarlineCapture Gameplay/UI Integration Handoff Spec

Date: 2026-05-07

This spec is the contract between the UI canvas work and the gameplay loop work. It captures the fixes already applied by Codex and the remaining work the gameplay agent should implement.

## PM Sync Directive

Status: blocking cross-lane handoff.

The gameplay agent should pause its current implementation plan and complete the remaining gameplay-side bridge wiring in this document before continuing unrelated gameplay work. The UI agent has created the contract needed by gameplay; continuing gameplay without this wiring risks mismatched controls, stale HUD state, duplicate command feedback, or UI elements that exist visually but are not driven by real gameplay.

After finishing, the gameplay agent must report in the standard WarlineCapture agent format from `Design/WarlineCapture_Agent_Coordination_Workflow.md`, including which gameplay systems call `BattleHudGameplayBridge`, which command modes and rejection reason codes are produced, what validation passed, and what remains for UI/FTUE.

## Active Gameplay Agent Task

Pause unrelated gameplay work and wire the real gameplay loop to `BattleHudGameplayBridge`.

Required output:

- selection updates call `ApplySelection` and `ClearSelection`
- command targeting calls `ApplyCommandMode` and `ClearCommandMode`
- rejected commands produce `TacticalCommandResult` with `TacticalCommandReasonCode` and call `ApplyCommandResult`
- production gameplay remains on the XZ plane
- mission/map routing uses `WarlineCaptureMissionSession` active ids
- required focused tests pass or failures are reported as blockers

## Fixed Now

### Tactical Runtime Plane

The production tactical map runtime now uses the same ground plane as the existing ECS gameplay systems:

- Gameplay/world plane: XZ
- Height axis: Y
- `GridConfig.Origin`: `(definition.WorldOrigin.x, 0, definition.WorldOrigin.y)`
- Tactical map loader grid transform: `(definition.WorldOrigin.x, 0, definition.WorldOrigin.y)`
- Camera map center for production XZ mode: `(center.x, existingCameraY, center.y)`

Updated files:

- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeMapLoaderValidationBuilder.cs`

The loader now exposes:

- `TacticalMapRuntimePlane.GameplayXZ`
- `TacticalMapRuntimePlane.ScreenXY`
- `MapWorldToRuntimeWorld(Vector2 mapWorldPosition)`
- `TryGetAnchorWorldPosition(string anchorId, out Vector3 worldPosition)`

Use `GameplayXZ` for production gameplay and ECS. Use `ScreenXY` only for isolated 2D design-target scenes.

### Mission / Scenario / Map Identity

Chapter 1 mission configs now carry the route identity required by the updated design docs:

- `MissionId`
- `ScenarioSetupId`
- `LevelId`
- `IsoMapId`
- `MapPreviewArtId`
- `MinimapArtId`

Updated files:

- `Assets/Game/Scripts/Campaign/MissionConfig.cs`
- `Assets/Game/Scripts/Campaign/ChapterOneMissionCatalog.cs`
- `Assets/Game/Scripts/Campaign/WarlineCaptureMissionSession.cs`
- `Assets/Game/Scripts/Campaign/ScenarioSetup.cs`

The first mission uses:

- Mission: `saga.ch01.m01.first_contact`
- Scenario setup: `scenario.ch01.m01.first_contact`
- Level: `level.ch01.district_edge_01`
- Iso map: `iso.ch01.district_edge_01`
- Preview art: `preview.ch01.first_contact`
- Minimap art: `minimap.ch01.first_contact`

### Quick Custom Launch

Quick Custom now explicitly starts the Chapter 1 M01 session before handing control to the legacy gameplay canvas, instead of relying only on the tactical runtime binder fallback.

Updated file:

- `Assets/Game/Scripts/UI/Screens/QuickCustomScreenController.cs`

Temporary behavior:

- Quick Custom launches `ChapterOneMissionCatalog.FirstContactMissionId`.
- Later, replace this with a selected/generated scenario setup when Quick Custom gets production map selection.

### Match HUD Gameplay Bridge

The match overlay now has a small gameplay-to-UI bridge contract:

- `Assets/Game/Scripts/UI/Components/BattleHudGameplayBridge.cs`
- `Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab`

The bridge owns these calls:

- `ApplySelection(string displayName, string status)`
- `ClearSelection()`
- `ApplyCommandMode(TacticalCommandMode mode)`
- `ClearCommandMode()`
- `ApplyCommandResult(TacticalCommandResult result)`
- `SetWorldMarkersVisible(bool visible)`

Command modes:

- `None`
- `Move`
- `Attack`
- `Hold`
- `Stop`
- `Build`
- `Special`

Canonical M01 command rejection reason codes:

- `NoSelection`
- `TargetOutOfBounds`
- `TargetBlocked`
- `TargetUnreachable`
- `TargetNotEnemy`
- `TargetNotAttackable`
- `CommandUnavailable`
- `MissionDoesNotAllowBuild`
- `CameraJumpUnavailable`

Do not use the earlier handoff aliases (`InvalidTarget`, `BlockedRoute`, `OutOfRange`, `BuildModeUnavailable`) for M01 Gate 4 assertions or assistant recovery. Later-mission codes such as `InsufficientResources`, `AbilityOnCooldown`, and `TransportUnavailable` are out of M01 scope unless explicitly marked future/non-M01.

The gameplay agent should call the bridge, not write UI text directly.

## Remaining Gameplay Agent Work

### 1. Plug Selection Into HUD

When a unit or squad is selected:

- Find `BattleHudGameplayBridge` in the active match overlay.
- Call `ApplySelection(displayName, status)`.
- Show selected unit/squad name and combat state.

When selection clears:

- Call `ClearSelection()`.

Do not make gameplay systems reference child UI paths such as `SelectedEntityPanel/NameText`.

### 2. Plug Command Modes Into HUD

When the player enters command targeting:

- Move command: `ApplyCommandMode(TacticalCommandMode.Move)`
- Attack command: `ApplyCommandMode(TacticalCommandMode.Attack)`
- Hold command: `ApplyCommandMode(TacticalCommandMode.Hold)`
- Build mode: `ApplyCommandMode(TacticalCommandMode.Build)`

When targeting exits:

- `ClearCommandMode()`

### 3. Plug Invalid Command Feedback

Every rejected command should return a `TacticalCommandResult` with a reason code:

```csharp
return TacticalCommandResult.Rejected(TacticalCommandReasonCode.TargetBlocked);
```

Then push that to UI:

```csharp
bridge.ApplyCommandResult(result);
```

Do not use ad hoc string-only error reporting inside selection, movement, attack, or build systems.

### 4. Replace Mouse-Only Prototype Input

`M01PlayableVisualPrototypeController` is still a design-target prototype. If it remains in use, add Android touch support:

- Primary touch press should behave like mouse click.
- Convert screen position to the active tactical map plane consistently.
- Keep prototype-only XY behavior isolated from production XZ behavior.

Production gameplay must use XZ.

### 5. Mission Route Resolution

Use the active session to resolve map data:

```csharp
WarlineCaptureMissionSession.ActiveMissionId
WarlineCaptureMissionSession.ActiveScenarioSetupId
WarlineCaptureMissionSession.ActiveLevelId
WarlineCaptureMissionSession.ActiveIsoMapId
WarlineCaptureMissionSession.ActiveMapPreviewArtId
WarlineCaptureMissionSession.ActiveMinimapArtId
```

The gameplay agent should not hard-code M01 except as a fallback while other maps are not authored.

### 6. Strategic / Tactical Zoom Split

The updated design direction has two battle map modes:

- Tactical zoom: unit command, selection, combat feedback, build/attack/hold.
- Strategic zoom: large map awareness, threat areas, objectives, routes, operation-level overlays.

Keep these surfaces separate in code:

- Tactical HUD state should feed `Screen_MatchOverlay`.
- Strategic overlay state should use a separate controller or bridge when its UI prefab exists.
- Do not overload the tactical command bridge with strategic map-only events.

## Required Validation

Before handing work back, run focused tests at minimum:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01TacticalRuntimeBindingTests -testResults /private/tmp/warlinecapture-chapter01-runtime-binding-results.xml -logFile /private/tmp/warlinecapture-chapter01-runtime-binding.log

"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter WarlineCaptureUiQuickCustomTests -testResults /private/tmp/warlinecapture-quickcustom-results.xml -logFile /private/tmp/warlinecapture-quickcustom.log

"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter WarlineCaptureUiMatchOverlayTests -testResults /private/tmp/warlinecapture-match-overlay-results.xml -logFile /private/tmp/warlinecapture-match-overlay.log
```

Add new tests for every gameplay-to-UI bridge connection. Do not rely only on manual play.

## Prompt For Gameplay Agent

Use this prompt with the other gameplay agent:

```text
Read Design/WarlineCapture_Gameplay_UI_Integration_Handoff_Spec.md first and follow it exactly.

Continue the WarlineCapture gameplay loop integration from the current codebase. Do not redesign the UI and do not write directly into child text objects. Use BattleHudGameplayBridge on Screen_MatchOverlay for selection, command mode, invalid command, and world-marker visibility.

Implement the gameplay-side wiring for:
1. Unit/squad selection -> BattleHudGameplayBridge.ApplySelection / ClearSelection.
2. Move, Attack, Hold, Stop, Build, and Special command modes -> BattleHudGameplayBridge.ApplyCommandMode / ClearCommandMode.
3. Rejected commands -> TacticalCommandResult with TacticalCommandReasonCode, then BattleHudGameplayBridge.ApplyCommandResult.
4. Android touch input parity for production tactical selection, camera drag, command targeting, road/wall/build placement, and green rectangle selection.
5. Active mission/session routing through WarlineCaptureMissionSession.ActiveMissionId, ActiveScenarioSetupId, ActiveLevelId, ActiveIsoMapId, ActiveMapPreviewArtId, and ActiveMinimapArtId.

Respect the production XZ plane. Do not reintroduce XY gameplay coordinates into ECS pathing, selection, movement, attack, build placement, or tactical map loading. Use TacticalMapRuntimeLoader.MapWorldToRuntimeWorld or the existing GridHelpers/CellToWorldCenter/WorldToCell APIs.

Keep Quick Custom's current M01 session fallback unless you implement full Quick Custom map/scenario selection in the same pass.

Before finishing, run:
- Chapter01TacticalRuntimeBindingTests
- WarlineCaptureUiQuickCustomTests
- WarlineCaptureUiMatchOverlayTests
Then report exactly what was wired, which systems call the HUD bridge, which command reason codes are produced, and any remaining manual Android-device validation needed.
```
