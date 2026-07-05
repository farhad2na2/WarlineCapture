# ECS Instantiate Ownership Classification

## Scope
- Source tracker: `Design/Architecture/architecture_performance_audit_followup_tracker.md`
- Date: 2026-07-03
- Runtime scan roots: `Assets/Game/Scripts/Systems`, `Assets/Game/Scripts/Rendering/Systems`, `Assets/Game/Scripts/Environment`, `Assets/Game/Scripts/UI/Shell/Ecs`
- Excluded roots: editor validation/migration code, authoring code, ScenarioLab manual/visual test runners, and UI non-ECS menu code.

## 2026-07-05 Refresh

- Refresh command: `rg --pcre2 -n "Object\.Instantiate|UnityEngine\.Object\.Instantiate|(?<![A-Za-z0-9_])Instantiate\s*\(" Assets/Game/Scripts/Systems Assets/Game/Scripts/Environment Assets/Game/Scripts/Rendering/Systems Assets/Game/Scripts/UI/Shell/Ecs -g '!*.meta' --glob '!**/Editor/**'`.
- Current scan result: 41 instantiate-like call lines in the runtime ECS/system scan roots.
- ECS entity-prefab instantiation is not counted as `Object.Instantiate` drift. The current scan has 14 `EntityManager`/`EntityCommandBuffer.Instantiate` lines, split between ECS gameplay prefab ownership and ECS visual-entity projection ownership.
- Current `Object.Instantiate`/GameObject clone result in the scan roots: 27 lines, all classified as presentation, metadata/probe, or render-state material clone. No current line is an authoritative gameplay entity spawn.

## Summary

| Classification | Call lines | Notes |
|---|---:|---|
| Gameplay `Object.Instantiate` spawn | 0 | No `Object.Instantiate` call in this runtime ECS/system scan owns gameplay entity creation. Gameplay prefab/entity creation uses ECS `EntityManager`/`ECB.Instantiate`. |
| ECS entity-prefab instantiate | 14 | ECS-owned entity prefab spawns/projections; not `Object.Instantiate` drift. |
| Visual/presentation `Object.Instantiate` spawn | 21 | Runtime GameObject visual, marker, road, city, blocker, transport, building, and decoration presentation. |
| Metadata/probe instantiate | 5 | Temporary hidden prefab instantiation for local-bounds/cache/projection probing. |
| Environment material clone | 1 | Runtime skybox material clone; not gameplay. |
| Immediate Phase 6 target | 0 currently measured recurring `Object.Instantiate` families | `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` was the first target and is now pooled/probed; latest battle GC/probe data reports 0 direct bytes in the measured window. |

## ECS Entity-Prefab Instantiate

These are already in ECS ownership and should not be converted to GameObject presentation helpers:

- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs:135` - ECS unit model visual entity projection.
- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs:324` - ECS unit model visual entity projection.
- `Assets/Game/Scripts/Rendering/Systems/UnitModelSpawnSystem.cs:377` - ECS unit model visual entity projection.
- `Assets/Game/Scripts/Rendering/Systems/UnitSelectionMarkerSystem.cs:163` - ECS selection marker visual entity projection.
- `Assets/Game/Scripts/Systems/InitialUnitsBlockerChurnSystem.cs:90` - ECS blocker entity replacement.
- `Assets/Game/Scripts/Systems/InitialUnitSpawnApplySystem.cs:31` - ECS initial unit spawn.
- `Assets/Game/Scripts/Systems/RoadBuildEcsCompositionSystemHelper.cs:182` - ECS road entity spawn.
- `Assets/Game/Scripts/Systems/BuildingSpawnCompositionSystemHelper.cs:356` - ECS building entity spawn.
- `Assets/Game/Scripts/Systems/CitizenVisibleUnitPresentationSystemHelper.cs:257` - ECS visible citizen presentation entity projection.
- `Assets/Game/Scripts/Systems/UnitRuntimeHealthBarSystem.cs:113` - ECS health bar visual entity projection.
- `Assets/Game/Scripts/Systems/InitialUnitsSpawnSystem.cs:627` - ECS blocker prefab spawn during initial-unit setup.
- `Assets/Game/Scripts/Systems/VehicleDestroyedVisualSystem.cs:173` - ECS destroyed-vehicle visual entity projection.
- `Assets/Game/Scripts/Systems/UnitTransportAirdropSystem.cs:1017` - ECS airdrop visual entity projection.
- `Assets/Game/Scripts/Systems/UnitRespawnSystem.cs:69` - ECS unit respawn.

## Visual / Presentation Spawn

- `Assets/Game/Scripts/Environment/RuntimeDecorationSpawnerPresentationSystemHelper.cs:148` - runtime decoration presentation spawn. Startup/map-generation visual population; candidate for pooled/chunked spawn only if measured on device.
- `Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs:70` - city combined mesh visual presentation spawn.
- `Assets/Game/Scripts/Environment/RuntimeCityVisualPresentationSystemHelper.cs:72` - city prefab visual presentation spawn.
- `Assets/Game/Scripts/Environment/RuntimeGridBlockerPresentationSystemHelper.cs:362` - grid blocker visual presentation spawn plus ECS blocker entity registration.
- `Assets/Game/Scripts/Systems/BuildingSelectionMarkerPresentationSystemHelper.cs:182` - cached building selection marker visual. Already lazy-singleton style.
- `Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs:767-768` - production transport presentation spawn, already a presentation helper path.
- `Assets/Game/Scripts/Systems/BuildingProductionTransportPresentationSystemHelper.cs:1699-1700` - production drop-visual presentation spawn, already a presentation helper path.
- `Assets/Game/Scripts/Systems/RoadBuildBuildingPlacementCompositionSystemHelper.cs:87` - road/build placement preview visual spawn.
- `Assets/Game/Scripts/Systems/BuildingDestroyedVisualPresentationSystemHelper.cs:42` - destroyed building visual presentation spawn.
- `Assets/Game/Scripts/Systems/BuildingPlacementVisualPresentationSystemHelper.cs:89` - runtime building placement visual spawn. First measured Phase 6 target; now behind a presentation-edge pool.
- `Assets/Game/Scripts/Systems/MapBuildingPlacementSpawnPrefabSystemHelper.cs:234` - map building visual wrapper spawn.
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
- `Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs:218` - temporary road visual variant prefab instantiate for projection/cache work.
- `Assets/Game/Scripts/Systems/RoadVisualVariantSystem.cs:313` - temporary road visual variant prefab instantiate for projection/cache work.

These are metadata probes. They should be cached and run at startup/config projection time only. They are not gameplay spawns, but they should not enter recurring runtime update lanes.

## Environment Material Clone

- `Assets/Game/Scripts/Environment/DayNightSystem.cs:216` - runtime skybox material clone. This is a render-state isolation clone, not gameplay. It should remain outside ECS gameplay ownership.

## Next Implementation Target

The first Phase 6 implementation target was `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance`.

Reasoning:
- It is the only instantiate family already called out by the GC capture as a current top runtime stack.
- It is presentation-only, so pooling can preserve ECS gameplay ownership.
- It can be addressed without new UI Toolkit, Boundary/Presenter classes, gameplay balance changes, or parallel gameplay logic.

Completed slice:
- Kept the ECS request/event path unchanged.
- Added a narrow pooled visual-root path for building placement visuals.
- Pooled by `BuildingDefinition`.
- Returned visuals to the pool on placement cancel/replacement paths while committed runtime buildings keep their existing ownership.
- Follow-up probe/capture reported no direct measured allocation from `BuildingPlacementVisualPresentationSystemHelper.CreateBuildingVisualInstance` in the battle window.

Remaining rule:
- Do not migrate ECS `EntityManager`/`ECB.Instantiate` lines away from ECS ownership.
- Only pool or chunk GameObject presentation call-site families after capture data shows recurring runtime allocation or frame-time cost.
