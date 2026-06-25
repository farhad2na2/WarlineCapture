# Premium Command Marker Feedback Implementation Plan

Date: 2026-06-14
Status: In Progress / First implementation pass complete
Owner: Gameplay

## Goal

Create clear premium command feedback markers for two player actions:

- Move command: when the player is in Move mode and taps the map, show a cyan/blue destination marker at the accepted move target.
- Attack command: when the player is in Attack mode and taps an enemy target, show a red/orange target marker on or around the accepted hostile target.

The markers must read clearly from the RTS gameplay camera, match the selected-unit marker family, and avoid the previous placeholder look of small circles, thick rectangles, noisy fragments, or under-ground effects.

## Current Problem

- Move mode can accept a map tap, but the player does not see a strong destination marker on the map.
- Attack mode can accept a hostile target tap, but the player does not get a strong attack-confirmation marker.
- Existing world marker visuals have been inconsistent across selection, move, attack, aircraft, vehicles, and soldiers.

## Visual Direction References

The imagegen reference sheets are preserved in the workspace here:

- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_01.png`
- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_02.png`
- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_03.png`
- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_04.png`
- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_05.png`
- `Design/VisualReferences/PremiumCommandMarkers/command_marker_reference_06.png`

Preferred move direction:

- Cyan/blue waypoint marker.
- Segmented destination ring or landing-zone pad.
- Directional chevrons that make the command feel intentional.
- Small vertical beacon/pin for camera-distance readability.
- Short pulse and fade after the accepted command.

Preferred attack direction:

- Red/orange hostile target lock.
- Brackets or reticle scaled to the target bounds.
- Thin projected ground reticle, not a giant solid disc.
- Short aggressive pulse when the command is accepted.
- Avoid covering the enemy model.

## Architecture Contract

This plan is constrained by:

- `Design/Architecture/gameplay_solid_ecs_contract.md`
- `Design/Architecture/rts_selection_system_responsibility_audit.md`
- `Design/Architecture/match_hud_attack_command_mode_plan.md`
- `Design/Architecture/premium_world_marker_implementation_plan.md`

- `SelectionOrderMarkerPresentationSystemHelper` owns move command markers, attack command markers, attack target locks, order marker lifetimes, order marker placement, and command marker material property blocks.
- `RtsSelectionRuntimeInputSystem` may detect pointer input and command mode state, but it must not own marker visuals.
- `RtsSelectionCommandResultFlushCompositionSystemHelper` may route accepted command results into `SelectionOrderMarkerPresentationSystemHelper`, but it must not own visual policy.
- `SelectionMoveCommandRequestSystem` and selected move-order systems own move validation and move command result publication.
- `SelectionAttackCommandRequestSystem`, `AttackOrderCommandSystem`, and `UnitTargetOrderSystem` own attack target validation and attack command result publication.
- `SelectionHudFeedbackSystem` and `BattleHudRuntimeFeedbackSystem` own HUD feedback. This feature must not bypass the ECS feedback queue to write HUD state directly.
- Preview markers shown while Attack mode is active are separate from accepted-command confirmation markers. Preview rings stay governed by existing target-preview flow; this plan only upgrades accepted move/attack command feedback unless a phase explicitly says otherwise.
- `BuildingSelectionMarkerPresentationSystemHelper` remains responsible only for building selection markers.
- `UnitSelectionMarkerSystem` remains responsible only for unit, vehicle, and aircraft selection markers.
- Do not add a marker manager, marker controller, marker facade, singleton, service locator, or broad orchestration shell.
- Do not attach command marker state directly to gameplay unit prefabs.
- Do not duplicate animated soldier meshes for command marker visuals.
- Do not add `Debug.Log*` calls in recurring gameplay paths. Diagnostics must use the existing diagnostics/event-buffer gates.
- Do not add `Object.Find*`, `GameObject.Find`, hierarchy string lookup, or runtime asset discovery for marker routing.
- Do not add static mutable gameplay state or `static Instance` access.
- Do not create per-frame managed allocations for marker ticking, color updates, target scans, or diagnostics.
- If a runtime source file is added, it must live under an existing bounded assembly. It must not compile into default `Assembly-CSharp`.
- Runtime marker code must stay inside existing gameplay/rendering assemblies and must not fall back into default `Assembly-CSharp`.

## ECS Data Flow

Accepted move command marker flow:

1. UI or hotkey arms `TacticalCommandMode.Move` through existing selection command intent flow.
2. `RtsSelectionRuntimeInputSystem` captures pointer input and queues ECS input/command requests only.
3. Move request systems validate the target and publish command results.
4. `RtsSelectionCommandResultFlushCompositionSystemHelper` drains accepted move results and calls the `SelectionOrderMarkerPresentationSystemHelper` move marker path.
5. `SelectionOrderMarkerPresentationSystemHelper` owns the marker instance, placement, material properties, scale, lifetime, and hide/show tick.

Accepted attack command marker flow:

1. UI or hotkey arms `TacticalCommandMode.Attack` through existing explicit attack command flow.
2. `RtsSelectionRuntimeInputSystem` captures pointer input and queues ECS input/command requests only.
3. Attack request/order systems validate hostile targets and publish command results.
4. `RtsSelectionCommandResultFlushCompositionSystemHelper` drains accepted attack results and calls the `SelectionOrderMarkerPresentationSystemHelper` attack marker path.
5. `SelectionOrderMarkerPresentationSystemHelper` owns the marker instance, placement, material properties, target-bounds scale, lifetime, and hide/show tick.

Rejected commands must publish feedback only through the existing command-result/HUD feedback path and must not show successful command markers.

## Design Rules

### Move Marker

- Intent color: cyan/blue.
- Shape language: waypoint, destination, direction.
- Runtime placement: accepted command world point, terrain-projected, lifted slightly above ground to prevent z-fighting.
- Runtime scale:
  - Infantry selection: compact destination ping.
  - Vehicle selection: wider waypoint pad.
  - Aircraft selection: ground point plus vertical beacon; no heavy aircraft-size rectangle.
- Lifetime: short visible confirmation, approximately 1.0 to 1.75 seconds unless existing config says otherwise.

### Attack Marker

- Intent color: red/orange.
- Shape language: hostile lock, target brackets, strike reticle.
- Runtime placement:
  - Enemy unit/vehicle/aircraft: marker around target bounds and/or projected under target.
  - Enemy building: bounds/corner marker around building footprint.
  - Attack-ground target if supported: red/orange strike reticle at world point.
- Runtime scale:
  - Infantry target: compact ring/brackets.
  - Vehicle target: target-bounds lock frame.
  - Building target: footprint/corner lock frame.
- Lifetime: short accepted-command confirmation, approximately 1.0 to 2.0 seconds unless target-lock behavior requires persistence.

## Phased Implementation Checklist

### Phase 0: Document And Reference Setup

Status: Complete / Live Match retest required
Validation: Documentation-only; no Unity run required.

- [x] Create this progress-tracking implementation document.
- [x] Preserve imagegen command marker references in `Design/VisualReferences/PremiumCommandMarkers/`.
- [x] User approved starting implementation from the documented direction.

### Phase 1: Runtime Flow Audit

Status: Complete
Validation: Static code audit; implementation notes below.

- [x] Find every call path that should show a move command marker after an accepted move order.
- [x] Find every call path that should show an attack command marker after an accepted attack order.
- [x] Confirm whether current marker spawning happens before or after command validation.
- [x] Confirm whether marker visibility is failing because of missing spawn calls, missing prefab references, hidden renderers, lifetime expiry, scale, or below-ground placement.
- [x] Confirm the fix can stay within `SelectionOrderMarkerPresentationSystemHelper` and command-result routing. If not, document why before expanding the edit surface.
- [x] Confirm preview marker behavior remains separate from accepted-command marker behavior.
- [x] Record exact systems, methods, and config assets touched before implementation.

Implementation notes:

- Accepted attack command results already routed to `SelectionOrderMarkerPresentationSystemHelper.ShowAttackOrderMarker`.
- Accepted move command results did not route to `SelectionOrderMarkerPresentationSystemHelper.ShowMoveOrderMarker` from `RtsSelectionCommandResultFlushCompositionSystemHelper`.
- `SelectedMoveOrderCommandSystem.TryIssueMoveOrder` had a direct pre-flush move marker call before final acceptance was owned by result flush. That direct visual side effect was removed and the accepted result path now owns projection.

Expected files to inspect:

- `Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs`
- `Assets/Game/Scripts/Systems/RtsSelectionCommandResultFlushCompositionSystemHelper.cs`
- `Assets/Game/Scripts/Systems/RtsSelectionRuntimeInputSystem.cs`
- `Assets/Game/Configs/Scene/Game_RTSSelection_Config.asset`
- `Assets/Game/Prefabs/Shapes/Target_Move.prefab`
- `Assets/Game/Prefabs/Shapes/Target_Attack.prefab`

### Phase 2: Accepted Move Command Marker Routing

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-result-contract.log` produced `[SelectionCommandRequestResultContractValidation] result=Passed tests=15`; `/private/tmp/warline-premium-command-marker-input.log` produced `[RtsSelectionInputSystemValidation] result=Passed tests=46`.

- [x] Ensure accepted Move mode map taps call the move marker display path.
- [x] Ensure rejected/invalid move taps do not show a successful move marker.
- [x] Keep move validation in move command systems; do not move validation into marker code.
- [x] Keep `RtsSelectionRuntimeInputSystem` as request-input orchestration only; do not make it instantiate or mutate marker visuals.
- [x] Keep camera panning behavior in Move mode intact.
- [x] Keep selected units selected after issuing move orders.
- [x] Add or update focused test coverage for accepted move tap marker routing.

### Phase 3: Accepted Attack Command Marker Routing

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-result-contract.log` produced `[SelectionCommandRequestResultContractValidation] result=Passed tests=15`; `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`.

- [x] Ensure accepted Attack mode hostile target taps call the attack marker display path.
- [x] Ensure invalid/friendly/non-hostile target taps do not show a successful attack marker.
- [x] Keep attack validation in attack command systems; do not move validation into marker code.
- [x] Keep runtime attack-preview rings separate from accepted attack-confirmation markers.
- [x] Preserve the explicit Attack mode contract from `match_hud_attack_command_mode_plan.md`.
- [x] Keep Attack mode behavior aligned with the explicit command-mode contract.
- [x] Keep selected units selected after issuing attack orders.
- [x] Add or update focused test coverage for accepted attack target marker routing.

### Phase 4: Move Marker Visual Asset

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-prefab-builder.log` produced `[PremiumWorldMarkerPrefabBuilder] rebuilt premium world marker prefabs`; `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; visual proof screenshot `/private/tmp/warline_premium_world_marker_visual_qa/move_command_marker.png`.

- [x] Update or replace the move command marker prefab with the approved cyan waypoint design.
- [x] Add segmented ring or destination pad geometry.
- [x] Add directional chevron geometry.
- [x] Add subtle projected fill/grid if it remains readable and not noisy.
- [x] Add a small vertical beacon/pin if approved.
- [x] Use hologram-compatible material properties: base color, emission color, alpha, pulse, scan.
- [x] Disable shadows, probes, motion vectors, and unnecessary renderer features.
- [x] Ensure marker is lifted above terrain and not hidden under ground.
- [x] Correct accepted move marker placement to use the accepted world hit height instead of always forcing `grid.Origin.y`.
- [x] Increase accepted move marker X/Z runtime scale for gameplay-camera readability without scaling vertical beacon height.
- [x] Replace fragmented waypoint grid, chevrons, segmented rings, and beacon with a connected fill, continuous outer ring, continuous inner ring, and flat center dot.
- [x] Lower move marker scan/pulse material settings so it reads as one stable marker at gameplay distance.

Preferred existing asset path:

- `Assets/Game/Prefabs/Shapes/Target_Move.prefab`

### Phase 5: Attack Marker Visual Asset

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-prefab-builder.log` produced `[PremiumWorldMarkerPrefabBuilder] rebuilt premium world marker prefabs`; `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; visual proof screenshot `/private/tmp/warline_premium_world_marker_visual_qa/attack_command_marker.png`.

- [x] Update or replace the attack command marker prefab with the approved red/orange target-lock design.
- [x] Add hostile lock brackets.
- [x] Add thin projected reticle.
- [x] Add compact pulse/scan element.
- [x] Avoid giant opaque red or white slabs.
- [x] Avoid obscuring the target model.
- [x] Use hologram-compatible material properties: base color, emission color, alpha, pulse, scan.
- [x] Disable shadows, probes, motion vectors, and unnecessary renderer features.
- [x] Ensure marker is lifted above terrain and not hidden under ground.
- [x] Correct untargeted attack-ground marker placement to use the accepted world hit height instead of always forcing `grid.Origin.y`.
- [x] Increase untargeted attack-ground marker X/Z runtime scale for gameplay-camera readability.

Preferred existing asset path:

- `Assets/Game/Prefabs/Shapes/Target_Attack.prefab`

### Phase 6: Target-Bounds Scaling

Status: In Progress
Validation: `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`.

- [ ] Move marker scales from selected command context with min/max clamps.
- [x] Attack marker scales from target bounds, not a hardcoded radius.
- [x] Infantry target markers remain compact.
- [x] Vehicle target markers match visible vehicle bounds.
- [x] Building target markers match building footprint/bounds.
- [x] Aircraft target markers do not reintroduce unwanted ground rectangles unless explicitly approved.

Note: accepted move command marker scale is currently a deliberately larger fixed waypoint because accepted move command results do not carry selected unit class/footprint context yet. A 2026-06-14 live screenshot showed the marker was mostly clipped below terrain and only beacon dots were visible. The runtime now uses the accepted world hit height and a larger X/Z scale. A later 2026-06-14 live screenshot showed the previous grid/ring/chevron/beacon design still read as fragmented dashes, so `Target_Move.prefab` was changed to connected continuous pieces and calmer material settings. Add selected-unit footprint payload later only if live QA shows the larger fixed waypoint is still not readable across infantry, vehicle, and aircraft orders.

### Phase 7: Material Property Block Compatibility

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; `git diff --check` passed.

- [x] Reuse existing material property block patterns in `SelectionOrderMarkerPresentationSystemHelper`.
- [x] Avoid per-frame material allocation.
- [x] Support `_BaseColor`, `_Color`, `_EmissionColor`, `_AccentColor`, `_Alpha`, pulse, and scan properties if available.
- [x] Ensure command markers remain visually distinct from selection markers.
- [x] Confirm DOTS/GPU rendering compatibility if any command marker path is rendered through ECS/Batched rendering.
- [x] Confirm marker lifetime/color ticking does not allocate per frame.
- [x] Confirm no recurring direct logging or string formatting is added.

### Phase 8: Architecture Validation

Status: Complete
Validation: `git diff --check` passed; `/private/tmp/warline-premium-command-marker-result-contract.log` produced `[SelectionCommandRequestResultContractValidation] result=Passed tests=15`; `/private/tmp/warline-premium-command-marker-selection-order.log` produced `[SelectionOrderMarkerFocusedValidation] result=Passed tests=11`; `/private/tmp/warline-premium-command-marker-input.log` produced `[RtsSelectionInputSystemValidation] result=Passed tests=46`.

- [x] Run `git diff --check`.
- [x] Run focused selection/order marker validation.
- [x] Run focused input validation if routing/input code changes.
- [x] Run assembly boundary validation if runtime source files or asmdefs are added/moved.
- [x] Run Burst hot-path validation if an existing ECS hot path is touched.
- [x] Record exact commands and log paths in this document.

Note: no runtime source files were added or moved, no asmdefs were changed, and no ECS hot-path system was added. Existing managed boundary systems were touched only for command-result routing and editor tests.

### Phase 9: Visual Proof Capture

Status: Complete
Validation: `/private/tmp/warline-premium-command-marker-visual-proof.log` produced `[PremiumWorldMarkerVisualProof] PASS report=/private/tmp/warline_premium_world_marker_visual_qa/premium_world_marker_visual_qa_report.md`.

- [x] Capture move command marker on open terrain.
- [ ] Capture move command marker near a road/obstacle.
- [ ] Capture attack marker on enemy infantry.
- [ ] Capture attack marker on enemy vehicle.
- [ ] Capture attack marker on enemy building if supported.
- [x] Inspect screenshots for scale, color, readability, and ground clipping.
- [ ] Retest accepted move marker in the live Match scene after the 2026-06-14 terrain-height correction.

Captured proof screenshots:

- `/private/tmp/warline_premium_world_marker_visual_qa/move_command_marker.png`
- `/private/tmp/warline_premium_world_marker_visual_qa/attack_command_marker.png`
- `/private/tmp/warline_premium_world_marker_visual_qa/ground_missile_target_lock.png`

Suggested proof log path:

- `/private/tmp/warline-premium-command-marker-visual-proof.log`

### Phase 10: Manual Gameplay QA

Status: Pending
Validation: Manual checklist with screenshots or notes.

- [ ] Select soldier, enter Move mode, tap terrain: cyan move marker appears immediately and is not reduced to tiny beacon dots.
- [ ] Select vehicle, enter Move mode, tap terrain: cyan move marker appears and is readable.
- [ ] Select aircraft, enter Move mode, tap terrain: marker does not show an unwanted aircraft-size rectangle.
- [ ] Select soldier/vehicle, enter Attack mode, tap hostile soldier: red/orange attack marker appears.
- [ ] Select soldier/vehicle, enter Attack mode, tap hostile vehicle: red/orange attack marker appears and scales to target.
- [ ] Tap invalid/friendly target in Attack mode: no successful attack marker appears.
- [ ] Markers do not block camera pan in command mode.
- [ ] Markers do not appear below ground.

## Progress Tracker

| Phase | Status | Notes | Validation |
| --- | --- | --- | --- |
| Phase 0: Document and reference setup | Complete | Tracker created; imagegen references copied into workspace. Implementation start treated as visual direction approval for this pass. | Documentation-only |
| Phase 1: Runtime flow audit | Complete | Missing move marker was a result-flush routing gap. Attack accepted-result routing already existed. | Static audit |
| Phase 2: Accepted move command marker routing | Complete | Accepted move results now call `SelectionOrderMarkerPresentationSystemHelper.ShowMoveOrderMarker` from `RtsSelectionCommandResultFlushCompositionSystemHelper`. | `/private/tmp/warline-premium-command-marker-result-contract.log` |
| Phase 3: Accepted attack command marker routing | Complete | Added focused coverage for accepted attack result marker projection and kept validation outside marker code. | `/private/tmp/warline-premium-command-marker-result-contract.log` |
| Phase 4: Move marker visual asset | Complete / Live Match retest required | `Target_Move.prefab` now uses connected cyan fill, continuous outer ring, continuous inner ring, and flat center dot. Runtime uses accepted world hit height and larger X/Z scale after live terrain clipping report. | `/private/tmp/warline-premium-command-marker-prefab-builder.log`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log`; pending live screenshot |
| Phase 5: Attack marker visual asset | Complete | `Target_Attack.prefab` rebuilt as red/orange strike marker with scan fill, reticle arcs, hostile brackets, chevrons, and beacon. Untargeted attack-ground markers now use accepted world hit height. | `/private/tmp/warline-premium-command-marker-prefab-builder.log`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log`; `/private/tmp/warline_premium_world_marker_visual_qa/attack_command_marker.png` |
| Phase 6: Target-bounds scaling | In Progress | Attack target markers use target bounds/footprints. Move command marker remains fixed-size but now uses accepted world hit height and larger X/Z runtime scale after live screenshot showed terrain clipping. | `/private/tmp/warline-premium-command-marker-selection-order.log`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log` |
| Phase 7: Material property block compatibility | Complete | Reused existing marker property-block path; new command materials use hologram shader/material properties. | `/private/tmp/warline-premium-command-marker-selection-order.log`; `git diff --check` |
| Phase 8: Architecture validation | Complete | No new manager/controller/facade/singleton/static state; no runtime source/asmdef moves; validation passed after terrain-height correction. | `/private/tmp/warline-premium-command-marker-result-contract.log`; `/private/tmp/warline-premium-command-marker-selection-order.log`; `/private/tmp/warline-premium-command-marker-input.log`; `/private/tmp/warline-command-marker-terrain-lift-result-contract.log`; `/private/tmp/warline-command-marker-terrain-lift-selection-order.log`; `git diff --check` |
| Phase 9: Visual proof capture | Complete / Live Match retest required | Graphics-enabled proof passed for the earlier asset, but the proof scene missed the live terrain-height mismatch and the move marker has since been simplified. Retest in Match scene after connected-marker correction. | `/private/tmp/warline-premium-command-marker-visual-proof.log`; pending live screenshot |
| Phase 10: Manual gameplay QA | Pending | Verify in Match scene after implementation and terrain-height correction. | Pending |

## Validation Commands To Record As Work Completes

Use the exact commands/log paths that match the touched systems. Initial candidates:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod SelectionOrderMarkerPresentationSystemHelperTests.RunFocusedValidation -logFile /private/tmp/warline-premium-command-marker-selection-order.log
```

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-Clone -executeMethod RtsSelectionInputSystemTests.RunFocusedValidation -logFile /private/tmp/warline-premium-command-marker-input.log
```

```bash
git diff --check
```

If runtime source files, assembly definitions, or ECS hot paths are touched, also record the relevant architecture validation commands here after selecting the final edit surface.

## Completion Criteria

- User approves the visual direction before final implementation.
- Accepted Move mode map taps show a premium cyan/blue marker.
- Accepted Attack mode hostile target taps show a premium red/orange marker.
- Invalid/rejected commands do not show successful command markers.
- Marker size, color, and placement are readable at gameplay camera distance.
- Marker placement is terrain-safe and does not clip below ground.
- Marker ownership remains inside `SelectionOrderMarkerPresentationSystemHelper`.
- UI/input systems continue to emit requests only and do not own marker visuals.
- Move and attack validation remains in command systems, not marker systems.
- Rejected commands do not bypass the ECS feedback queue.
- No new manager/controller/facade/singleton/static mutable state is introduced.
- No recurring direct `Debug.Log*` or per-frame managed allocation is introduced.
- Runtime code remains inside bounded assemblies.
- Focused automated validation passes.
- Visual proof screenshots are captured before claiming final visual acceptance.
