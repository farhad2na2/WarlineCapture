# WarlineCapture Isometric Macro Tiles

This folder is the Unity-facing import root for authored terrain macro-tile PNGs.

The editor builder `Assets/Game/Scripts/Editor/WarlineCaptureFictionalGulfIsoMapBuilder.cs` looks here first when building usable iso map scenes. If a requested macro-tile PNG is missing, the builder falls back to a colored placeholder chunk and lists the missing tile in the Unity import report.

## Naming

Use lowercase sanitized tile ids:

```text
fg.mt.urban_straight_road -> fg_mt_urban_straight_road
```

Preferred shared files:

```text
Assets/Game/Art/Generated/IsometricMaps/MacroTiles/Shared/fg_mt_urban_straight_road_a.png
Assets/Game/Art/Generated/IsometricMaps/MacroTiles/Shared/fg_mt_urban_straight_road_a_rot90.png
```

Preferred map-specific files:

```text
Assets/Game/Art/Generated/IsometricMaps/MacroTiles/iso_fg_l01_coastal_command/fg_mt_command_plaza_a.png
```

## Rules

- Terrain art may bake roads, curbs, plazas, empty pads, seawalls, water edge detail, and non-interactive dressing.
- Terrain art must not bake runtime gameplay buildings, units, vehicles, boats, turrets, VFX, UI, markers, health bars, real flags, real insignia, real landmarks, or readable political text.
- Gameplay truth remains in map metadata and runtime entities.
