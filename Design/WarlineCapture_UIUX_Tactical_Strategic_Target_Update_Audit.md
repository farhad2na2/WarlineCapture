# WarlineCapture UI/UX Tactical Strategic Target Update Audit

Date: 2026-05-07

## Purpose

This audit captures the UI impact of the updated strategic / tactical gameplay direction.

The major design change is that WarlineCapture now treats map presentation as two distinct gameplay contexts:

- **Strategic / zoomed-out map**: mission choice, Saga route context, briefing preview, Operation context, threat route preview, minimap overview, and objective jump context.
- **Tactical / zoomed-in map**: the actual playable combat map used for selection, movement, attack, build placement, objectives, camera bounds, minimap jumps, VFX, and command validation.

UI targets and prefabs must not blur these contexts. Strategic images are preview/minimap/context art. Tactical HUD and command feedback must validate over a close-up tactical map with runtime units, markers, metadata anchors, camera bounds, and typed command results.

## Source Docs Reviewed

- `Design/WarlineCapture_Strategic_Tactical_Map_Gameplay_Alignment.md`
- `Design/WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`
- `Design/WarlineCapture_M01_FirstContact_Production_Contract.md`
- `Design/WarlineCapture_UIUX_Gameplay_Element_Alignment.md`
- `Design/WarlineCapture_UIUX_Phase6_Immediate_Implementation_Plan.md`
- `Design/WarlineCapture_UIUX_Phase7_Immediate_Implementation_Plan.md`
- `Design/WarlineCapture_FTUE_And_Command_Assistant_Design.md`
- `Design/WarlineCapture_UIUX_Mockup_To_Canvas_Conversion_Plan.md`

## New VisualLock Targets Created

These targets are state targets or new-surface targets. They do not replace the accepted base screens unless the implementation pass explicitly switches to them. Before Canvas implementation, each required target must receive a matching `Design/VisualLockLayered/<SurfaceId>/` layer pack.

| Surface | Target Path | Why It Exists |
|---|---|---|
| `SCN-08_RTSBattleHUD_M01_TacticalFeedback` | `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png` | Promoted clean candidate target. Adds selected entity panel, command mode banner, world markers, invalid command toast, minimap viewport, and M01 move/attack feedback over tactical map context. |
| `SCN-09_BuildDrawer_M01DisabledState` | `Design/VisualLock/SCN-09_BuildDrawer_M01DisabledState/SCN-09_BuildDrawer_M01DisabledState_Landscape_Target.png` | Shows `MissionDoesNotAllowBuild` and item availability reasons for M01 where building is disabled. |
| `SCN-10_UnitCommandWheel_TargetingState` | `Design/VisualLock/SCN-10_UnitCommandWheel_TargetingState/SCN-10_UnitCommandWheel_TargetingState_Landscape_Target.png` | Adds explicit move/attack targeting hints and disabled command reason states. |
| `POP-01_ThreatAlert_RoutePreviewState` | `Design/VisualLock/POP-01_ThreatAlert_RoutePreviewState/POP-01_ThreatAlert_RoutePreviewState_Landscape_Target.png` | Adds route preview and jump-to-threat affordance tied to tactical/strategic focus anchors. |
| `POP-03_BuildPlacement_MetadataValidityState` | `Design/VisualLock/POP-03_BuildPlacement_MetadataValidityState/POP-03_BuildPlacement_MetadataValidityState_Landscape_Target.png` | Adds metadata-backed footprint overlay, socket/zone label, blocker reason, and confirm validity state. |
| `POP-05_MissionResult_M01ContractState` | `Design/VisualLock/POP-05_MissionResult_M01ContractState/POP-05_MissionResult_M01ContractState_Landscape_Target.png` | Adds M01 Mission / Scenario / Level / IsoMap source summary state for result binding. |
| `PREFAB-04_AssistantButton` | `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png` | New ARIA persistent entry point and recommendation/critical/takeover state target. |
| `PREFAB-05_AssistantPanel` | `Design/VisualLock/PREFAB-05_AssistantPanel/PREFAB-05_AssistantPanel_Landscape_Target.png` | New ARIA recommendations and explanation panel target. |
| `PREFAB-06_TutorialCard` | `Design/VisualLock/PREFAB-06_TutorialCard/PREFAB-06_TutorialCard_Landscape_Target.png` | New contextual FTUE tutorial card target. |
| `PREFAB-07_TutorialHighlight` | `Design/VisualLock/PREFAB-07_TutorialHighlight/PREFAB-07_TutorialHighlight_Landscape_Target.png` | New UI/world highlight and path-preview target. |
| `POP-10_AssistantTakeover` | `Design/VisualLock/POP-10_AssistantTakeover/POP-10_AssistantTakeover_Landscape_Target.png` | New ARIA control ownership/takeover banner target. |
| `POP-11_CommanderIdentity` | `Design/VisualLock/POP-11_CommanderIdentity/POP-11_CommanderIdentity_Landscape_Target.png` | New commander identity setup target. |

Supporting generator:

- `Tools/UI/generate_tactical_strategic_target_refresh.py`
- Contact sheet: `Design/VisualLock/TacticalStrategic_TargetRefresh_2026-05-07_ContactSheet.png`

## Existing Targets That Are Still Valid As Base Chrome

These existing VisualLock targets remain useful for screen layout, chrome, typography, and state baseline, but are no longer sufficient for the updated tactical/strategic gameplay contract by themselves.

| Existing Surface | Current Status | Required Refresh |
|---|---|---|
| `SCN-05_SagaMap` | Base target valid for strategic campaign context. | Confirm mission nodes carry `MissionId`, `ScenarioSetupId`, `LevelId`, `IsoMapId`, `MapPreviewArtId`, and star/unlock state. Do not represent this as the tactical playable map. |
| `SCN-06_MissionBriefing` | Base target valid for mission briefing context. | Ensure the map image is `MapPreviewArtId`, not tactical ground. Add/validate minimap preview and mission/source data binding. |
| `SCN-08_RTSBattleHUD` | Base HUD chrome valid. | Stale for M01 tactical feedback. Use `SCN-08_RTSBattleHUD_M01_TacticalFeedback` as the next state target. |
| `SCN-09_BuildDrawerProduction` | Base drawer valid. | Stale for M01 disabled build and availability reason states. Use `SCN-09_BuildDrawer_M01DisabledState`. |
| `SCN-10_UnitCommandWheel` | Base command wheel valid. | Stale for target hint and disabled reason states. Use `SCN-10_UnitCommandWheel_TargetingState`. |
| `POP-01_ThreatAlert` | Base alert chrome valid. | Stale for route preview and jump-to-threat state. Use `POP-01_ThreatAlert_RoutePreviewState`. |
| `POP-03_BuildPlacement` | Base placement popup valid. | Stale for metadata-backed footprint, socket label, blocker reason, and confirm-validity state. Use `POP-03_BuildPlacement_MetadataValidityState`. |
| `POP-05_MissionResult` | Base result popup valid. | Stale for M01 tactical source summary and explicit Mission / Scenario / Level / IsoMap binding. Use `POP-05_MissionResult_M01ContractState`. |

## Missing Layer Packs

Status update: initial `VisualLockLayered` packs now exist for all refreshed targets listed above. The `SCN-08_RTSBattleHUD_M01_TacticalFeedback` pack was regenerated from the promoted clean target and has object-level HUD/marker reference layers. Some M01 layers are exact visual reference crops from the approved target; Unity implementation must keep text, icons, runtime markers, and panel chrome as separate Canvas layers instead of pasting those crops as final UI.

Before editing Unity prefabs for any refreshed target, confirm the matching folder contains:

- `Design/VisualLockLayered/<SurfaceId>/reference/<SurfaceId>_Landscape_Target.png`
- `Design/VisualLockLayered/<SurfaceId>/layers/`
- `Design/VisualLockLayered/<SurfaceId>/layer_manifest.json`
- `Design/VisualLockLayered/<SurfaceId>/generated_one_go/layers_contact_sheet.png`
- `Design/VisualLockLayered/<SurfaceId>/README.md`

Current M01 HUD pack:

- `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback/layer_manifest.json`
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback/generated_one_go/layers_contact_sheet.png`
- The staged M01 Unity assets are clean implementation layers: frame assets contain no baked text/icons, and runtime marker assets contain no map pixels.

## Updated UI Todo List

### Target And Layer Pack Work

1. Review the new flattened state targets in `Design/VisualLock`.
2. For M01 tactical work, create / expand layer packs in this order:
   - `SCN-08_RTSBattleHUD_M01_TacticalFeedback` - object-level pack created and staged.
   - `SCN-10_UnitCommandWheel_TargetingState`
   - `SCN-09_BuildDrawer_M01DisabledState`
   - `POP-03_BuildPlacement_MetadataValidityState`
   - `POP-01_ThreatAlert_RoutePreviewState`
   - `POP-05_MissionResult_M01ContractState`
3. For FTUE / ARIA work, create layer packs in this order:
   - `PREFAB-04_AssistantButton`
   - `PREFAB-06_TutorialCard`
   - `PREFAB-07_TutorialHighlight`
   - `PREFAB-05_AssistantPanel`
   - `POP-10_AssistantTakeover`
   - `POP-11_CommanderIdentity`
4. Refresh the canonical target inventory after the layer packs exist.

### Implementation Work

1. Update `Screen_MatchOverlay` for:
   - `BattleHud.SelectedEntityPanel` - prefab scaffold created, hidden by default.
   - `BattleHud.CommandModeBanner` - prefab scaffold created, hidden by default.
   - `BattleHud.WorldCommandMarkerLayer` - prefab scaffold created, hidden by default.
   - `BattleHud.InvalidCommandToast` - prefab scaffold created, hidden by default.
   - `BattleHud.MinimapCameraBridge` - prefab scaffold created with visible viewport rectangle.
   - objective-row camera jump and world objective marker pulse.
2. Update command input and UI binding so direct and explicit commands share typed result codes:
   - `NoSelection`
   - `TargetOutOfBounds`
   - `TargetBlocked`
   - `TargetUnreachable`
   - `TargetNotEnemy`
   - `TargetNotAttackable`
   - `CommandUnavailable`
   - `MissionDoesNotAllowBuild`
   - `CameraJumpUnavailable`
3. Update `SCN-10` command wheel for move/attack targeting, target hints, selected states, and disabled reasons.
4. Update `SCN-09` / `PREFAB-03` build drawer and `POP-03` placement for availability reasons, footprint overlays, socket labels, and confirm-validity states.
5. Update `POP-01` for route preview and jump-to-threat behavior.
6. Update `POP-05` result binding to M01 Mission / Scenario / Level / IsoMap ids.
7. Add FTUE / ARIA route surfaces after M01 HUD feedback exists:
   - Assistant button
   - Assistant panel
   - Tutorial card
   - Tutorial highlight
   - Assistant takeover
   - Commander identity.
8. Add focused tests for the new UI element ids and binding contracts.
9. Capture 16:9 and 20:9 target-vs-render comparisons for every updated state.

## Recommendations Before Implementation

1. **Do not implement the new tactical UI from the old `SCN-08_RTSBattleHUD` target alone.** It lacks required M01 command feedback and will keep producing misleading “matched” results.
2. **Create layer packs for the new state targets before prefab edits.** The flattened PNGs are target references, not implementation assets.
3. **Use M01 as the first validation slice.** It is the only fully specified mission contract and includes exact ids, anchors, commands, disabled-build behavior, and result binding.
4. **Keep map art contracts separate.** `MapPreviewArtId` and `MinimapArtId` can be strategic/preview art; `IsoMapId` and `TacticalMapDefinition` own the playable close-up map.
5. **Do not bake units, markers, objective symbols, minimap viewport rectangles, build footprints, or ARIA highlights into map images.** They must be runtime layers.
6. **Add typed UI result reasons before polishing the visual states.** Without reason codes, disabled build/invalid move/invalid attack UI will become decorative.
7. **Treat FTUE / ARIA as a second pass after M01 HUD feedback.** ARIA targets depend on the same typed UI ids, runtime entity ids, tactical anchors, and minimap/camera bridge.
8. **For Saga and Mission Briefing, perform a content-binding audit before visual rework.** Their base targets are not visually invalid, but they must not imply strategic previews are the actual tactical map.
