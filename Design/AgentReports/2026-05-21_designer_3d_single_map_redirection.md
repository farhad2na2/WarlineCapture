# Designer Report: 3D Single-Map Redirection

Date: 2026-05-21
Lane: Designer
Status: Complete

## Summary

Updated the design source of truth to return WarlineCapture to a full 3D single-map RTS direction. The new direction removes the 2.5D isometric macro-tile target and the separate strategic/tactical map product model for future design work.

## Design Decisions

- Active gameplay target is now one large 3D operation map per mission or operation.
- Campaign, Operations, and Skirmish select setup/rules/loadout/objectives for the same 3D operation-map model.
- Planning, briefing, minimap, threat alerts, and deployment are UI/camera overlays over the same world, not separate maps.
- Public unit/building names and descriptions should come from `Assets/Game/Configs/Prefabs`.
- The new main-menu visual direction is the command-base style with Campaign, Operations, Skirmish, Store, Commander, Settings, Credits, Supplies, Command, and Deploy Operation.

## Key Files Updated

- `Design/3D_SingleMap_Gameplay_Direction.md`
- `Design/README.md`
- `README.md`
- `Design/Gameplay_North_Star_And_Content_Grammar.md`
- `Design/Command_Offensive_Premise_Alignment.md`
- `Design/Strategic_Tactical_Map_Gameplay_Alignment.md`
- `Design/2D_Isometric_Production_Direction.md`
- `Design/2D_Isometric_Art_Bible.md`
- `Design/2D_Isometric_Implementation_Validation_Plan.md`
- `Design/MacroTile_Terrain_Production_Plan.md`
- `Design/UIUX_MainMenu_Visual_Contract.md`

## Follow-Up Needed

- UI agent should regenerate/update visual targets for the menu surfaces listed in `3D_SingleMap_Gameplay_Direction.md`.
- Gameplay/PM should decide when M01 can expand beyond the current infantry-only playable-slice gate.
- Project-state dashboard may need regeneration after PM updates the project-state JSON.
