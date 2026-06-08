# WarlineCapture Tactical UI Missing Parts Work Order

Date: 2026-05-07

## Purpose

This is the handoff checklist for the UI agent before the Chapter 1 tactical gameplay slice is coded against the new AI-generated tactical-map workflow.

It adds the missing UI pieces that were identified after the close-up tactical-map scale test:

- selected unit and selected building feedback
- direct move and direct attack command feedback
- explicit attack mode feedback
- invalid move / attack / build feedback
- minimap and camera jump behavior
- build placement feedback over metadata-backed pads / zones
- strategic preview versus tactical gameplay map contract

This document does not replace the existing UI phase plans. It extends:

- Phase 6: `SCN-08 RTS Battle HUD`
- Phase 7: `SCN-09 Build Drawer`, `SCN-10 Command Wheel`, `POP-01 Threat Alert`, `POP-03 Build Placement`, `POP-05 Mission Result`
- Phase 8: objectives and result binding

It must stay aligned with `Strategic_Tactical_Map_Gameplay_Alignment.md`; that document owns the shared strategic-preview versus tactical-playable-map contract.

For the first production slice, the UI agent must also read `M01_FirstContact_Production_Contract.md`. That document owns the concrete M01 ids, anchors, command reason codes, FTUE targets, UI feedback, and validation gates.

## Locked Map Decision

Use two map views with different jobs:

| View | Meaning | Used By | Art Source |
|---|---|---|---|
| Strategic / zoomed-out | Campaign/mission preview, minimap, camera-jump context. Not used for close combat scale validation. | `SCN-05`, `SCN-06`, minimap panel, threat jump previews. | `MapPreviewArtId`, `MinimapArtId` from ScenarioSetup / TacticalMapDefinition. |
| Tactical / zoomed-in | Real playable combat view where units, vehicles, buildings, selection, movement, and attack are evaluated. | `SCN-08`, runtime match scene, validation scenes. | Approved close-up POT ground plate plus separate runtime sprites. |

Do not try to make one large image satisfy both views. The close-up tactical image is the playable map. The strategic image is a preview/minimap aid.

## UI Phase Additions

### Phase 6 - SCN-08 Battle HUD

Add these missing parts to the tactical HUD work queue:

| ElementId | Surface | Required UI | Gameplay Contract | Validation |
|---|---|---|---|---|
| `BattleHud.SelectedEntityPanel` | `SCN-08` | Compact bottom/side panel for selected unit, squad, vehicle, or building. Shows name, icon, HP, owner, current order, command availability. | Reads selected ECS entity/group, health, owner, command capability set. | Select infantry, vehicle, friendly building, enemy building; panel updates and never uses baked text from art. |
| `BattleHud.CommandModeBanner` | `SCN-08` | Small transient mode label for `Move`, `Attack`, `Build`, `Patrol`, or no explicit mode. | Reads command input mode from selection/controller state. | Tapping `ATTACK` enters attack targeting; cancel/stop clears banner. |
| `BattleHud.WorldCommandMarkerLayer` | `SCN-08` | Runtime world markers for selected unit ring, destination marker, attack target marker, invalid target marker. | Uses world-space anchors from unit/building/map metadata, not baked pixels. | Move and attack markers render at correct tactical scale above the ground plate. |
| `BattleHud.InvalidCommandToast` | `SCN-08` | Short feedback row/toast for invalid target, blocked path, no attack target, unreachable building, insufficient resources. | Reads command validation result and reason code. | Invalid road/building/blocked-cell taps show reason and do not issue partial orders. |
| `BattleHud.MinimapCameraBridge` | `SCN-08` | Minimap drag/tap/jump affordance and active camera viewport rectangle. | Reads `MinimapArtId`, camera bounds, tactical map metadata, threat/objective anchors. | Tapping minimap or threat jump moves the tactical camera without showing black map edges. |

### Phase 7 - SCN-09 Build Drawer / POP-03 Build Placement

Add metadata-backed placement feedback:

| ElementId | Surface | Required UI | Gameplay Contract | Validation |
|---|---|---|---|---|
| `BuildDrawer.ItemAvailabilityReason` | `SCN-09` | Disabled reason for locked, unaffordable, mission-banned, or producer-missing items. | Reads build catalog, mission rules, resources, producer state. | Disabled rows explain exactly why they cannot be used. |
| `BuildPlacement.FootprintOverlay` | `POP-03` / world overlay | Valid / invalid footprint cells over the tactical map. | Reads `BuildingDefinition`, footprint, map buildable cells, blockers, resources. | Footprint turns valid/invalid as the pointer moves across pads, roads, blockers, and map edge. |
| `BuildPlacement.SocketOrZoneLabel` | `POP-03` | Names the active build socket, pad, or reason the area is blocked. | Reads `TacticalMapDefinition.BuildableCells`, named build zones, blocker reason. | Build placement on a non-buildable cell reports a clear reason. |
| `BuildPlacement.ConfirmState` | `POP-03` | Confirm button enabled only when footprint and cost are valid. | Reads placement validator and resources. | Confirm cannot fire on invalid metadata cells. |

### Phase 7 - SCN-10 Command Wheel

Add command target ownership and explicit attack mode:

| ElementId | Surface | Required UI | Gameplay Contract | Validation |
|---|---|---|---|---|
| `CommandWheel.AttackModeSegment` | `SCN-10` | Attack segment enters explicit attack targeting mode. | Reads selected entity combat capability and target filters. | Attack mode remains active until target, cancel, or stop. |
| `CommandWheel.MoveModeSegment` | `SCN-10` | Move segment enters explicit move targeting mode. | Reads movement capability and pathing state. | Move target rejects blocked cells with feedback. |
| `CommandWheel.TargetHint` | `SCN-10` | Context hint says what kind of map/entity target is expected. | Reads selected command target type. | Hint differs for ground, enemy unit, enemy building, transport, and build placement. |
| `CommandWheel.DisabledReason` | `SCN-10` | Disabled command segments show reason. | Reads command capability set, cooldown, mission lock, selected entity type. | Disabled commands are explainable, not inert. |

### Phase 7 - POP-01 Threat Alert

Add camera and minimap integration:

| ElementId | Surface | Required UI | Gameplay Contract | Validation |
|---|---|---|---|---|
| `ThreatAlert.JumpToThreat` | `POP-01` | Jump button focuses tactical camera on threat anchor. | Reads threat event id, map anchor, route waypoint, camera bounds. | Jump lands inside tactical map bounds and selected threat marker is visible. |
| `ThreatAlert.RoutePreview` | `POP-01` | Small route/ETA row uses metadata route anchors. | Reads threat route id and ETA. | Route row matches the runtime route used by enemy movement. |

### Phase 8 - Objectives And Results

Add objective marker ownership and result consistency:

| ElementId | Surface | Required UI | Gameplay Contract | Validation |
|---|---|---|---|---|
| `Objective.WorldMarker` | `SCN-08` | Runtime objective markers on map. | Reads objective anchors from TacticalMapDefinition and ObjectiveRuntimeState. | Objective row tap focuses the matching world marker. |
| `Objective.HudProgressBinding` | `SCN-08` | Objective rows show current progress, complete, failed, and selected/focused states. | Reads ObjectiveManager state. | M01 destroy-patrol objective updates from enemy death. |
| `MissionResult.TacticalSourceSummary` | `POP-05` | Result identifies mission/source route and star outcomes. | Reads MissionResultData, active MissionConfig, ScenarioSetup. | Result uses current mission ids and rewards, not placeholder labels. |

## Direct Control Rules

The playable RTS control model should support both direct and explicit commands:

| Player Action | Expected Result | UI Required |
|---|---|---|
| Tap friendly unit | Select unit/group. | Selection ring, selected entity panel, available commands. |
| Selected unit + tap walkable ground | Issue move order. | Destination marker, command toast if invalid. |
| Selected unit + tap enemy unit/building | Issue attack order. | Attack marker, target highlight, selected entity current order. |
| Tap `MOVE`, then tap ground | Explicit move mode. | Command mode banner and target hint. |
| Tap `ATTACK`, then tap enemy | Explicit attack mode. | Command mode banner and target hint. |
| Tap minimap/threat/objective jump | Move camera within tactical bounds. | Viewport indicator and no black/empty edge exposure. |
| Choose building from drawer | Enter placement mode. | Footprint overlay and valid/invalid confirm state. |

## UI Agent Work Order

The UI agent should handle this in order:

1. Use `M01_FirstContact_Production_Contract.md` as the first concrete UI target.
2. Update `SCN-08` prefab/controller targets for selected entity panel, command mode banner, marker-layer hooks, invalid feedback, and minimap camera bridge.
3. Update `SCN-10` command wheel target mapping so move/attack/patrol/build/extract segments can show target hints and disabled reasons.
4. Update `SCN-09` and `POP-03` build placement UI so availability and footprint validity are visible from metadata.
5. Update `POP-01` threat alert so jump-to-threat and route preview are part of the target contract.
6. Confirm `POP-05` result binding uses the active mission/scenario/map ids.
7. Add focused tests for each UI element id above.
8. Produce 16:9 and 20:9 captures of the battle HUD over the approved tactical close-up map, with runtime sprites visible at approved scale.

### M01 UI Target

For `saga.ch01.m01.first_contact`, the UI work should prioritize:

- `BattleHud.SelectedEntityPanel` for `unit.player.rifle_squad_01`
- `BattleHud.CommandModeBanner` for Move and Attack
- move, attack, invalid, selection, and objective markers
- minimap viewport and objective jump against `iso.ch01.district_edge_01`
- disabled Build feedback using `MissionDoesNotAllowBuild`
- `POP-05` result summary for `saga.ch01.m01.first_contact`

## Gameplay Agent Work Order

The gameplay work should proceed in parallel only after the UI element contracts above exist:

1. Create `TacticalMapDefinition` schema with walkable, road, sidewalk, blocked, buildable, spawn, route, objective, attack-target, and camera-bound metadata.
2. Build the first metadata authoring overlay for `iso.ch01.district_edge_01`.
3. Load metadata into current grid buffers: `GridWalkable`, `GridRoad`, `GridRoadSidewalk`, `GridRoadDirt`, static blockers, spawn anchors, objective anchors.
4. Wire selected unit + tap ground into move validation with reason codes for UI feedback.
5. Wire selected unit + tap enemy unit/building into attack validation with reason codes for UI feedback.
6. Wire minimap/threat/objective jumps to camera bounds from map metadata.
7. Wire build placement validator to map buildable cells and blocker footprint metadata.
8. Build M01 validation scene with art-only view, metadata overlay view, and playable select/move/attack objective flow.

## Acceptance Gate

This work order is ready for production implementation when:

- every listed `ElementId` has a prefab/controller/test owner
- direct select/move/attack can be represented by UI feedback
- explicit move/attack modes can be represented by UI feedback
- invalid commands return reason codes consumed by UI
- minimap/threat/objective jumps use tactical map bounds and anchors
- build placement uses metadata, not image pixels
- the tactical HUD can be captured over the approved close-up map at unit scale without blurred map quality mismatch
