# ECS Instantiate Ownership Classification

## Scope
- Source tracker: `Design/Architecture/architecture_performance_audit_followup_tracker.md`
- Date: 2026-07-03
- Runtime scan roots: `Assets/Game/Scripts/Systems`, `Assets/Game/Scripts/Rendering/Systems`, `Assets/Game/Scripts/Environment`, `Assets/Game/Scripts/UI/Shell/Ecs`
- Excluded roots: editor validation/migration code, authoring code, ScenarioLab manual/visual test runners, and UI non-ECS menu code.

## Summary

| Classification | Call lines | Notes |
|---|---:|---|
| Gameplay entity spawn | 0 | No `Object.Instantiate` call in this runtime ECS/system scan owns gameplay entity creation. |
| Visual/presentation spawn | 17 | Runtime GameObject visual, marker, road, city, blocker, and decoration presentation. |
| Metadata/probe instantiate | 2 | Temporary hidden prefab instantiation for local-bounds/cache probing. |
| Environment material clone | 1 | Runtime skybox material clone; not gameplay. |
| Immediate Phase 6 target | 1 family | `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` is already observed in GC capture top rows. |

## Visual / Presentation Spawn

- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs:140` - runtime decoration presentation spawn. Startup/map-generation visual population; candidate for pooled/chunked spawn only if measured on device.
- `Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs:70` - city combined mesh visual presentation spawn.
- `Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs:72` - city prefab visual presentation spawn.
- `Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs:344` - grid blocker visual presentation spawn plus ECS blocker entity registration.
- `Assets/Game/Scripts/Systems/BuildingSelectionMarkerPresentationSystemHelper.cs:182` - cached building selection marker visual. Already lazy-singleton style.
- `Assets/Game/Scripts/Systems/BuildingPlacementVisualPresentationSystemHelper.cs:26` - runtime building placement visual spawn. Current measured GC target.
- `Assets/Game/Scripts/Systems/BuildingDestroyedVisualPresentationSystemHelper.cs:42` - destroyed building visual presentation spawn.
- `Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs:229` - map building visual wrapper spawn.
- `Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs:190` - straight debug road visual spawn.
- `Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs:272` - intersection debug road visual spawn.
- `Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs:536` - runtime road visual spawn.
- `Assets/Game/Scripts/Systems/RoadSpecialVisualSystem.cs:745` - runtime road visual spawn.
- `Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs:674` - cached move marker visual.
- `Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs:697` - cached attack marker visual.
- `Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs:718` - pooled attack-target preview marker visual expansion.
- `Assets/Game/Scripts/Systems/SelectionOrderMarkerPresentationSystemHelper.cs:908` - cached attack target selection marker visual.
- `Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs:70-72` and road/marker families should stay at presentation edges, not gameplay systems.

## Metadata / Probe Instantiate

- `Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:681` - temporary hidden visual-template instantiate for bounds metadata.
- `Assets/Game/Scripts/Systems/BuildingDefinitionPrefabSystemHelper.cs:682` - temporary hidden prefab instantiate for bounds metadata.
- `Assets/Game/Scripts/Systems/RoadBuildDefinitionProjectionSystem.cs:40` - temporary hidden road/building prefab instantiate for bounds metadata.

These are metadata probes. They should be cached and run at startup/config projection time only. They are not gameplay spawns, but they should not enter recurring runtime update lanes.

## Environment Material Clone

- `Assets/Game/Scripts/Environment/DayNightSystem.cs:216` - runtime skybox material clone. This is a render-state isolation clone, not gameplay. It should remain outside ECS gameplay ownership.

## Next Implementation Target

Start Phase 6 with `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`.

Reasoning:
- It is the only instantiate family already called out by the GC capture as a current top runtime stack.
- It is presentation-only, so pooling can preserve ECS gameplay ownership.
- It can be addressed without new UI Toolkit, Boundary/Presenter classes, gameplay balance changes, or parallel gameplay logic.

Recommended slice:
- Keep the ECS request/event path unchanged.
- Add a narrow pooled visual-root helper for building placement visuals.
- Pool by `BuildingDefinition` or prefab identity where practical.
- Reset transform, renderer state, and active state before reuse.
- Return visuals to the pool on placement cancel/replacement/destroy paths.
- Validate with Unity compile, focused building-placement smoke, `git diff --check`, and a follow-up GC capture.
