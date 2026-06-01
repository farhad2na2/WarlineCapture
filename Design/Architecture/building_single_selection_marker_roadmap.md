# Building Single Selection Marker Roadmap

This roadmap tracks the replacement of per-building `SelectionMarker` prefab children with one shared runtime building selection marker.

## Target

Building selection state stays in `RuntimeBuildingSystem`. Building selection marker rendering, movement, resize, and visibility belong only to `BuildingSelectionMarkerSystem`. Unit selection marker authoring and visibility remain unchanged.

## Ownership Rules

- `BuildingSelectionMarkerSystem` owns the one runtime marker GameObject.
- The marker prefab is passed through explicit building gameplay config/composition.
- Building marker position is derived from selected runtime building origin, footprint, and grid cell size.
- Building marker visibility is refreshed from `RuntimeBuildingSystem.CurrentActiveBuildingId`.
- `BuildingRuntimeVisualSystem` initializes persistent per-building visuals only, including `FactionMarker`, `Door_Z`, `Destroyed`, alive roots, and animated parts.
- `FactionMarker` remains per building because it represents persistent ownership/state.
- Building prefabs under `Assets/Game/Prefabs/Buildings` must not contain a `SelectionMarker` child.
- Unit selection marker components and the `SelectionMarker` child in `Assets/Game/Prefabs/Characters/Unit.prefab` are intentionally allowed.

## Steps

1. Document this roadmap and the architecture contract rule.
2. Create `Assets/Game/Prefabs/Buildings/BuildingSelectionMarker.prefab` from the existing building marker visual.
3. Add `BuildingSelectionMarkerSystem` with an explicit context containing runtime buildings, `RuntimeBuildingSystem`, grid lookup, footprint-center delegate, marker prefab, marker parent/root, visual system, faction visuals, and marker property block.
4. Wire `BuildingSelectionMarkerSystem` through `BuildingGameplayCompositionSourceSystem`, startup config, and composition context creation.
5. Replace building marker refresh callbacks so they call `BuildingSelectionMarkerSystem.Refresh`.
6. Remove building `SelectionMarker` storage from `RuntimeBuildingData` and stop `BuildingRuntimeVisualSystem` from finding or toggling per-building selection markers.
7. Update combat/destroy paths so selected-building removal clears selection and refreshes the shared marker.
8. Remove `SelectionMarker` children from `Building.prefab` and `Tent.prefab`.
9. Add architecture tests preventing `SelectionMarker` children in building prefabs and preventing selection-marker state from returning to `RuntimeBuildingData` or `BuildingRuntimeVisualSystem`.
10. Add focused behavior tests covering one shared marker moving between selected buildings, hidden-on-clear, hidden-on-destroy, faction marker tint preservation, and unit marker exemption.

## Validation Gates

- EditMode: `BuildingSelectionMarkerSystemTests`
- EditMode: `BuildingRuntimeVisualSystemTests`
- EditMode: `RuntimeBuildingSystemTests`
- EditMode: `GameplayArchitectureContractTests`
- Prefab scan: no `SelectionMarker` under `Assets/Game/Prefabs/Buildings/*.prefab`
- Prefab scan: `Assets/Game/Prefabs/Characters/Unit.prefab` may still contain `SelectionMarker`
- Runtime smoke: start match, select two different buildings, verify only one marker exists and it moves/resizes correctly.

## Progress

- Step 1: Complete. Roadmap and architecture contract rule added.
- Step 2: Complete. Shared `BuildingSelectionMarker.prefab` created from the previous marker visual.
- Step 3: Complete. `BuildingSelectionMarkerSystem` owns the shared marker instance, refresh, move, resize, hide, and disposal behavior.
- Step 4: Complete. Building placement config and managed composition now expose the marker prefab through explicit context wiring.
- Step 5: Complete. Building marker refresh callbacks now route to `BuildingSelectionMarkerSystem.Refresh`.
- Step 6: Complete. `RuntimeBuildingData` and `BuildingRuntimeVisualSystem` no longer store or toggle per-building selection markers.
- Step 7: Complete. Combat/destroy paths still clear selection and refresh the shared marker through the existing runtime marker refresh callback.
- Step 8: Complete. `SelectionMarker` children removed from `Building.prefab` and `Tent.prefab`; variants inherit the cleanup.
- Step 9: Complete. Architecture drift guard added for building prefab marker children and runtime marker ownership.
- Step 10: Complete. Focused behavior tests added for shared marker move/hide and runtime visual marker ownership.
