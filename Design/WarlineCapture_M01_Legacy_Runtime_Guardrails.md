# WarlineCapture M01 Legacy Runtime Guardrails

Date: 2026-05-07

## Scope

This audit covers the active M01 production slice:

- `MissionId`: `saga.ch01.m01.first_contact`
- tactical map: `iso.ch01.district_edge_01`
- runtime entities: `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, `decor.command_point`

## Current Guardrails

- M01 fixed tactical gameplay bypasses `RuntimeCitySpawnerSystem`; random/procedural city roads do not rewrite authored M01 road cells.
- M01 build and road authoring are rejected with `MissionDoesNotAllowBuild`.
- M01 disables `DayNightSystem` runtime visual mutations through `GameBootstrap.ApplyFixedTacticalMissionGuardrails()`.
- Chapter 1 tactical asset manifest notes now require fixed-direction baked/contact shadows for M01 production unit/decor atlas frames.

## Legacy Render Blockers

M01 is not fully migrated away from legacy prefab rendering yet.

- `GameSubScene_InitialUnitsSpawner_Config.asset` still references legacy unit prefabs for the compact spawn source.
- `Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab` is a prefab variant with a child named `Model`.
- `UnitRenderBudgetSystem`, `UnitImpostorRenderSystem`, `SharedPrefabPreviewCache`, `UnitGridAuthoring`, `MenuView`, `BuildingPlacementSystem`, and `GameRuntimeStats` still contain assumptions around prefab model bounds, preview capture, LOD/impostor fallback, or model instance accounting.
- `Destroyed` child usage exists in legacy visual-target/editor art paths and external asset prefabs. Production M01 should use `vfx.unit.destroyed.small` / atlas state data rather than toggling a separate `Destroyed` child object.

These blockers mean M01 cannot yet be marked as fully independent of legacy 3D `Model` prefab rendering. The current production-safe treatment is to keep legacy prefabs isolated as temporary ECS spawn sources while the visible production target remains the Chapter 1 2D/isometric atlas contract.

## Migration Plan

1. Create an M01 runtime sprite presenter for ECS entities keyed by `MissionRuntimeEntityId` and `Chapter01TacticalAtlasContract` sprite ids.
2. Route `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point` to approved sprite atlas frames instead of `Model` child renderers.
3. Represent idle, move, attack, damaged, and destroyed states through atlas frames or explicit sprite overlay/VFX state data.
4. Use `vfx.unit.destroyed.small` for M01 destruction feedback; do not toggle a separate `Destroyed` child GameObject.
5. Remove M01 visible rendering dependency on `UnitRenderBudgetSystem`/`UnitImpostorRenderSystem` after sprite presenter validation passes at close tactical camera scale.
6. Keep legacy prefab children and generated assets in place until non-M01 systems are migrated or explicitly marked legacy/future.

## Shadow Requirements

M01 unit/decor atlas frames must include baked/contact shadows:

- fixed light direction matched to the tactical ground plate
- consistent contact scale across idle, move, attack, damaged, and destroyed frames
- no runtime dynamic shadow direction changes
- no floating sprites or shadows that separate from feet/vehicle contact points

## QA Treatment

- Legacy `Model` prefab rendering is a major migration gap for production art validation, not a blocker for current command/pathfinding PlayMode validation.
- `Destroyed` child rendering is not accepted for M01 production visuals; use VFX/atlas state data.
- `DayNightSystem is disabled for M01 fixed tactical gameplay` and should remain inactive unless a current design doc re-enables it.
