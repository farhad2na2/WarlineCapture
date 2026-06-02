# Character Visual Adornments Refactor Roadmap

## Goal
Remove per-character prefab child ownership for inherited `SelectionMarker`, `FactionMarker`, and `HealthBar` under `Assets/Game/Prefabs/Characters`. Characters and vehicles are both units, so this pass must reuse/generalize the vehicle runtime visual-adornment path instead of creating duplicate character-only systems. Characters should use shared ECS unit visual boundaries for multi-selection markers, damage health bars, and faction tint without changing character movement, selection state, combat, death, LOD, impostor, animation, or pathfinding behavior.

## Audited Prefab Shape
- `Assets/Game/Prefabs/Characters/Unit.prefab` previously owned `SelectionMarker`, `FactionMarker`, and `HealthBar` children.
- Character variants inherit from `Unit.prefab`, so the cleanup happened at the base prefab first.
- No `Destroyed` child exists under the character prefabs scanned for this plan; character destroyed/death behavior is out of scope for this pass.

## Progress
- [x] 1. Add this roadmap document.
- [x] 2. Update the architecture contract with character visual ownership rules.
- [x] 3. Audit the current vehicle adornment implementation and identify exactly what must become shared unit code versus vehicle-only code.
- [x] 4. Add architecture tests that define the target shared ownership: `UnitSelectionMarkerSystem` and `UnitRuntimeHealthBarSystem` serve both character and vehicle units; `VehicleDestroyedVisualSystem` remains vehicle-only.
- [x] 5. Add architecture tests that fail when character prefabs contain or inherit `SelectionMarker`, `FactionMarker`, or `HealthBar` children.
- [x] 6. Rename or wrap `VehicleSelectionMarkerSystem` into a shared `UnitSelectionMarkerSystem` without changing vehicle behavior.
- [x] 7. Rename or wrap `VehicleHealthBarSystem` into a shared `UnitRuntimeHealthBarSystem` without changing vehicle behavior.
- [x] 8. Keep `VehicleDestroyedVisualSystem` vehicle-only because characters do not have a matching destroyed-child requirement in this pass.
- [x] 9. Replace vehicle marker/health tests with shared unit marker/health tests that still cover vehicle units.
- [x] 10. Add shared unit visual prefab/config fields to `UnitGridAuthoringConfig` or convert existing vehicle marker/health fields into generic unit marker/health fields while preserving serialized vehicle assignments.
- [x] 11. Add matching cached fields and read-only properties to `UnitGridAuthoring`.
- [x] 12. Add or generalize ECS components for unit visual prefab references and runtime instance references.
- [x] 13. Bake shared unit visual prefab references for both vehicle-motion units and non-vehicle character units.
- [x] 14. Create `Assets/Game/Prefabs/Characters/CharacterSelectionMarker.prefab` from the current character selection marker visual, unless the vehicle marker prefab is already visually correct at character scale. Reused `Assets/Game/Prefabs/Vehicles/VehicleSelectionMarker.prefab` through the generic unit config path to avoid duplicate unit marker assets.
- [x] 15. Create `Assets/Game/Prefabs/Characters/CharacterHealthBar.prefab` from the current character health bar visual, unless the vehicle health bar prefab is already visually correct at character scale. Reused `Assets/Game/Prefabs/Vehicles/VehicleHealthBar.prefab` through the generic unit config path to avoid duplicate unit health-bar assets.
- [x] 16. Assign shared unit selection marker and health bar prefab references to all character configs.
- [x] 17. Add a narrow shared unit visual prefab reference backfill path for source-key spawned units whose baked prefab references are stale or missing.
- [x] 18. Extend `UnitSelectionMarkerSystem` to include non-vehicle character units with multi-selected character support.
- [x] 19. Wire marker refresh from ECS selection state and `SelectedUnitTag`, not UI or managed lookup.
- [x] 20. Ensure marker instances follow unit world position, rotation policy, and ground/offset policy without changing unit transforms.
- [x] 21. Size markers from deterministic unit footprint/config, not renderer bounds every frame.
- [x] 22. Destroy/hide marker instances on deselect, death, despawn, transport hiding, impostor-only hiding, or invalid entity.
- [x] 23. Extend `UnitRuntimeHealthBarSystem` to include non-vehicle character units using configured runtime health bar instances.
- [x] 24. Replace child-authored character health bar usage with runtime-spawned health bars driven by `RecentDamageHealthBarVisibility` and `UnitHealth`.
- [x] 25. Ensure health bar follow/offset behavior matches the previous child-authored visual without adding per-frame GameObject lookup.
- [x] 26. Generalize the existing vehicle faction tint path into shared unit tint target projection for vehicle and character model renderers using `UnitFactionTintTargetBackfillSystem`, `FactionTintTarget`, and `FactionTintColor`.
- [x] 27. Route character faction indication through existing shared `FactionVisualSystem`; do not create `CharacterFactionMarkerSystem` or `UnitFactionMarkerSystem`.
- [x] 28. Keep vehicle tint, vehicle destroyed visuals, building tint, and building marker behavior unchanged.
- [x] 29. Keep character animation, LOD, impostor, surface grounding, selection state, and combat behavior unchanged.
- [x] 30. Remove inherited `SelectionMarker`, `FactionMarker`, and `HealthBar` from `Unit.prefab`.
- [x] 31. Confirm all character variants inherit the cleanup and still keep `Model`, animation, weapon, renderer, and required functional children.
- [x] 32. Remove character-specific dependencies on prefab child `SelectionMarkerAuthoring` and `UnitHealthBarAuthoring`.
- [x] 33. Keep `SelectionMarkerAuthoring`, `SelectionMarkerVisibilitySystem`, `UnitHealthBarAuthoring`, and `UnitHealthBarSystem` only if another approved prefab family still needs them; otherwise schedule their deletion in a separate cleanup. `UnitHealthBarAuthoring` remains required by the shared runtime health bar prefab, `SelectionMarkerVisibilitySystem` remains required by runtime-created markers, and `UnitHealthBarSystem` remains responsible for health-bar fill/expiry. `SelectionMarkerAuthoring` has no live character/vehicle/building prefab dependency after this pass and can be audited separately with other unused authoring helpers.
- [x] 34. Add focused edit-mode tests for shared unit marker create/move/remove behavior across vehicle and character units.
- [x] 35. Add focused edit-mode tests for shared unit health bar visibility/fill/follow behavior across vehicle and character units.
- [x] 36. Add focused edit-mode tests for character faction tint projection.
- [x] 37. Add architecture tests proving character prefabs are clean while building and vehicle visual-adornment rules still pass.
- [x] 38. Add source-key/backfill tests proving spawned character entities receive shared marker and health bar prefab references.
- [x] 39. Run compile validation and focused gameplay tests.
- [x] 40. Run runtime smoke in `WarlineCapture-CodexUnity1`: select multiple soldiers and vehicles, damage one of each, verify marker/health/faction visuals.
- [x] 41. Run a visual proof capture in Match showing selected characters with visible markers and no inherited prefab marker children.
- [x] 42. Record validation result here.

## Ownership Rules
- Character and vehicle selection marker projection belongs in shared `UnitSelectionMarkerSystem`.
- Character and vehicle damage health bar projection belongs in shared `UnitRuntimeHealthBarSystem`.
- Shared unit marker/health prefab references belong on explicit unit startup ECS boundaries through `UnitSharedVisualPrefabReferences` and `InitialUnitsSpawnConfig` so runtime-spawned units do not depend on prefab entities remaining loaded.
- Vehicle destroyed/wreck visuals remain in `VehicleDestroyedVisualSystem`.
- Character and vehicle owner-faction visuals use renderer tint through ECS faction visual data. `UnitFactionTintTargetBackfillSystem` owns tint target projection and the shared `FactionVisualSystem` owns tint color updates.
- Character prefabs under `Assets/Game/Prefabs/Characters` must not contain or inherit `SelectionMarker`, `FactionMarker`, or `HealthBar` children after this roadmap is complete.
- Character death/destroyed visuals are not part of this pass unless a future audit finds a character-specific destroyed child or config requirement.
- Do not add parallel character-only marker, faction-marker, or health-bar systems unless a concrete behavior difference cannot be represented by unit config/policy.

## Validation Gates
- Prefab scan: no `SelectionMarker`, `FactionMarker`, `HealthBar`, `SelectionMarkerAuthoring`, or `UnitHealthBarAuthoring` remains under live character prefab assets after cleanup.
- Runtime smoke: selecting multiple soldiers creates one marker per selected character, deselect removes only the relevant marker, and no marker remains for dead/despawned/transport-hidden characters.
- Runtime smoke: damaging a character shows one health bar with correct fill and hides it after the existing visibility timeout.
- Runtime smoke: faction tint applies to both vehicle and character model renderers without a `FactionMarker` child or bake-time mutation of nested model prefabs.
- Regression: shared unit marker/health systems continue to pass vehicle behavior tests, `VehicleDestroyedVisualSystem` keeps vehicle wreck behavior, and building marker/faction/destroyed systems continue to pass their existing tests.
- Performance: runtime marker and health bar instances are created/destroyed on state transitions, not every frame; follow/update loops use ECS component queries and do not use scene searches.

## Assumptions
- This pass applies only to `Assets/Game/Prefabs/Characters`.
- Characters support multi-selection, so the marker strategy should share the vehicle unit marker implementation, not the single shared building marker.
- Shared character marker and health bar prefabs are acceptable because all current character variants inherit the same base child visual shape.
- `FactionMarker` should be replaced by tinting real unit model renderers through `UnitFactionTintTargetBackfillSystem` and the existing shared `FactionVisualSystem`, not by another marker object.
- Existing character death animation/destroyed behavior remains unchanged unless separately requested.

## Validation Result
- Focused validation passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` on 2026-06-02:
  - `VehicleVisualAdornmentsSystemTests` exited with code 0 and the Unity log contained no compile errors or failed assertions.
  - `GameplayArchitectureContractTests.VehicleVisualAdornmentsMustUseRuntimeVisualBoundaries` exited with code 0 and the Unity log contained no compile errors or failed assertions.
- Static validation passed in the main workspace:
  - `git diff --check`
  - character prefab scan found no `SelectionMarker`, `FactionMarker`, `HealthBar`, `SelectionMarkerAuthoring`, or `UnitHealthBarAuthoring` under `Assets/Game/Prefabs/Characters`.
- Runtime visual smoke/proof passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` on 2026-06-02:
  - `/private/tmp/warline_character_visual_adornments_proof.txt` reported `result=completed`.
  - The proof selected three character units and one vehicle, damaged one of each, and verified `characterMarkers=3/3`, `vehicleMarkers=1/1`, `healthBars=2/2`, and character faction tint targets.
  - Screenshot written to `/private/tmp/warline_character_visual_adornments_proof.png`.
  - Unity graphics-enabled batchmode exited with code 0 and the filtered log showed no compile errors or gameplay exceptions.
