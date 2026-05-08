# PM Dispatch: Gameplay Legacy Render And Day/Night Guardrails

Date: 2026-05-07

## Trigger

The user flagged two legacy gameplay directions that are no longer aligned with the current M01 production slice:

- Unit/building prefabs were previously built with a 3D `Model` child and often a separate `Destroyed` child object.
- A day/night system existed, but should be disabled for now.

## Task Update

`Design/AgentTasks/gameplay_current.md` now includes both constraints in the active gameplay task.

Gameplay must treat these as production-direction guardrails:

- Current M01 gameplay should move toward animated 2D/isometric sprite-atlas rendering.
- Alive, idle, move, attack, damaged, and destroyed visuals should be atlas states/frames or explicit sprite-overlay state data.
- Unit sprite atlases must include baked/contact shadows aligned to the tactical map's fixed lighting direction and ground-contact scale.
- Separate `Destroyed` child GameObjects should not be required for production gameplay destroyed state.
- Legacy 3D `Model` children and `Destroyed` children must not be bulk-deleted blindly; first audit runtime dependencies and migrate the M01 production path with validation.
- `DayNightSystem`, time-of-day lighting, night vision, dynamic sun/sky/fog, and related UI hooks must be disabled or isolated from M01/fixed-road tactical gameplay for now.

## Initial Evidence

Repo search found legacy render-state references in representative paths including:

- `Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Slow.prefab`
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Tray.prefab`
- `Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab`
- `Assets/Game/Prefabs/Characters/Unit.prefab`
- `Assets/Game/Scripts/Editor/UnitImpostorAtlasGenerator.cs`
- `Assets/Game/Scripts/Configs/UnitPrefabRegistryAuthoringConfig.cs`

Repo search found day/night runtime wiring in representative paths including:

- `Assets/Game/Scripts/Environment/DayNightSystem.cs`
- `Assets/Game/Scripts/Bootstrap/GameBootstrap.cs`
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`
- `Assets/Game/Scripts/UI/MenuView.cs`
- `Assets/Game/Scripts/UI/MainMenuPlayUI.cs`

## Required Direction

For this gameplay slice, the required outcome is not a full all-prefab art migration. The required outcome is:

- Audit exact runtime dependencies.
- Guard or disable legacy systems that should not run in M01.
- Implement the M01-safe first slice only where low-risk and clearly validated.
- Carry fixed-direction baked/contact shadow requirements into the sprite-atlas migration plan so units do not float or cast shadows inconsistent with map art.
- Report a concrete migration plan for the remaining unit/building prefab work.

## Cross-Lane State

- Gameplay owns runtime prefab/render-state guards, M01 migration planning, day/night disabling, and validation.
- UI should continue assistant and visual target work; do not ask UI to solve runtime prefab rendering.
- Support/FTUE should avoid documenting day/night or legacy destroyed-child visuals as current M01 behavior.
- QA/HCI should treat visible 3D model fallback, floating units, mismatched baked shadow direction, incorrect destroyed-state toggles, day/night lighting shifts, night-vision effects, or time-of-day readability changes in M01 as findings.
