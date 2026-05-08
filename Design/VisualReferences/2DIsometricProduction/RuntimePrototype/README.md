# ISO-02 Runtime Prototype

Date: 2026-05-05

This folder contains manual design-validation outputs for the isolated 2D isometric runtime prototype.

## Files

- `ISO02_RuntimePrototype_Start.png`
  - Start-state capture of the prototype scene.
- `ISO02_RuntimePrototype_Mid.png`
  - Mid-movement capture showing moving units, sorting changes, and overlay followers.
- `ISO02_RuntimePrototype_End.png`
  - End-state capture after the prototype agents reach their target waypoints.
- `ISO02_RuntimePrototype_Report.md`
  - Manual report with movement, sorting, overlay-follow, readability, capture, and performance-smoke checks.

## Unity Paths

- Scene: `Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity`
- Builder: `Assets/Game/Scripts/Editor/WarlineCaptureIso2DRuntimePrototypeBuilder.cs`
- Runtime scripts: `Assets/Game/Scripts/Iso2D`
- Gameplay camera: `ISO02 Gameplay Camera`, tagged `MainCamera`, orthographic size `3.45`

## Manual Camera Check

Open `Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity` and switch to Game view.

- Use Play Mode to confirm the camera starts on the tactical battlefield composition.
- Use arrow keys or WASD to pan.
- Use the mouse wheel to inspect zoom levels.
- Confirm units, overlays, roads, HQs, and cover read clearly at the default zoom before any production integration.

## Layout Rule

The ISO-02 prototype should be regenerated as a deliberate base-to-base combat lane:

- friendly base on the western/left side
- enemy base on the eastern/right side
- connected main road through the center
- small side service lanes only where road art reads as connected
- ruins and barricades framing the central frontline
- rifle squad, APC, and tank moving along explicit waypoint lanes

The final production version should use the existing ECS grid/pathfinding and items/entities. This prototype is only for validating the 2D iso presentation, zoom, sorting, and overlay behavior before ECS integration.

## Rule

This prototype is for manual gameplay/art validation only. It is not wired into Jenkins or build validation, and it does not replace `Assets/Game/Scenes/Game.unity`.
