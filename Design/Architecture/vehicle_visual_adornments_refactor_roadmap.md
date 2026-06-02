# Vehicle Visual Adornments Refactor Roadmap

## Goal
Remove per-vehicle prefab child ownership for `Destroyed`, inherited `SelectionMarker`, inherited `FactionMarker`, and inherited `HealthBar`. Vehicles use explicit ECS visual boundaries for destroyed visuals, multi-selection markers, health bars, and faction tint. Character prefabs remain unchanged until their later refactor.

## Progress
- [x] 1. Add this roadmap document.
- [x] 2. Update the architecture contract with vehicle visual ownership rules.
- [x] 3. Add architecture tests that fail when vehicle prefabs contain or inherit forbidden visual children.
- [x] 4. Add vehicle visual prefab/config fields to `UnitGridAuthoringConfig`.
- [x] 5. Add matching cached fields and read-only properties to `UnitGridAuthoring`.
- [x] 6. Add ECS components for vehicle visual prefab references and runtime instance references.
- [x] 7. Bake vehicle visual prefab references only for vehicle-motion units.
- [x] 8. Extract each vehicle `Destroyed` child into a configured destroyed visual prefab.
- [x] 9. Assign extracted destroyed visual prefabs and shared marker/health prefabs to vehicle configs.
- [x] 10. Remove `Destroyed` children from all vehicle prefabs.
- [x] 11. Remove inherited `SelectionMarker`, `FactionMarker`, and `HealthBar` from `Unit_Veh.prefab`.
- [x] 12. Confirm vehicle variants still keep `Model` and functional visual children.
- [x] 13. Implement `VehicleDestroyedVisualSystem`.
- [x] 14. Route vehicle death/wreck through configured destroyed visuals while preserving character death behavior.
- [x] 15. Implement shared `UnitSelectionMarkerSystem` with multi-selected vehicle support.
- [x] 16. Wire marker refresh from ECS selection state.
- [x] 17. Implement shared `UnitRuntimeHealthBarSystem` with configured runtime health bars.
- [x] 18. Replace vehicle health bar child usage with runtime-spawned health bars.
- [x] 19. Add vehicle model renderer faction tint baking.
- [x] 20. Keep building tint and character visual behavior unchanged.
- [x] 21. Remove vehicle-specific dependencies on prefab child marker/health/destroyed authoring.
- [x] 22. Add focused vehicle selection marker tests.
- [x] 23. Add focused vehicle health bar tests.
- [x] 24. Add focused vehicle destroyed visual tests.
- [x] 25. Add prefab architecture tests proving vehicles are clean and characters are still allowed.
- [ ] 26. Run compile validation and focused tests.
- [ ] 27. Run runtime smoke in `WarlineCapture-CodexUnity1`.
- [x] 28. Record validation result here.

## Ownership Rules
- Vehicle selection marker projection belongs in shared `UnitSelectionMarkerSystem`.
- Vehicle health bar projection belongs in shared `UnitRuntimeHealthBarSystem`.
- Vehicle destroyed visual projection belongs in `VehicleDestroyedVisualSystem`.
- Vehicle owner-faction visuals use renderer tint through ECS faction visual data.
- Vehicle prefabs must not contain or inherit `SelectionMarker`, `FactionMarker`, `HealthBar`, or `Destroyed` children.

## Validation Result
- `git diff --check` passed after trimming Unity YAML whitespace.
- Vehicle prefab text scan passed: no `SelectionMarker`, `FactionMarker`, `HealthBar`, `Destroyed`, `SelectionMarkerAuthoring`, or legacy `UnitDestroyedVisualReference` remains under live `Unit_Veh*.prefab` assets.
- Vehicle config scan passed: all `Prefab_UnitGrid_Veh*` config assets reference a destroyed visual prefab, the shared vehicle selection marker prefab, and the shared vehicle health bar prefab.
- Unity editor domain reload completed with no current `error CS` entries in the latest log tail. Earlier `Child` lookup errors were stale from a previous import and were absent after the reload.
- Focused edit-mode tests were added in `Assets/Tests/Editor/VehicleVisualAdornmentsSystemTests.cs`, but the Test Runner was not executed in this turn because the main project is open in the editor and batchmode cannot attach to the active project.
- Runtime smoke in `WarlineCapture-CodexUnity1` is still pending.
