# WarlineCapture 2D Isometric Production References

Date: 2026-05-05

This folder contains the active 2D-isometric production references for WarlineCapture.

## Active Direction

Use large authored 2D isometric terrain macro tiles plus repeatable gameplay metadata.

Do not continue the removed tiny road tile, road chunk, generated road overlay, or procedural final-art attempts.

## Current Files

- `ISO-01_CityCommand_Target/ISO-01_CityCommand_ProductionTarget.png`
  - Baseline production visual target for the 2D isometric direction.

- `ISO-01_CityCommand_ProductionBreakdown.md`
  - Original asset categories and early validation criteria.

- `GoldenAssets/`
  - First generated transparent golden assets plus source notes.
  - Useful for units/buildings/overlays, but terrain production now moves to macro tiles.

- `UnitySpike/`
  - ISO-01 Unity test scene capture and manual report for import/sorting/readability.

- `RuntimePrototype/`
  - ISO-02 runtime prototype captures and report for movement, sorting, and overlay followers.

- `TerrainVisualTarget/`
  - ISO-04 polished terrain-only visual reference for target road/plaza quality.

- `MacroTilePrototype/Chapter01_2_5DReferences/`
  - Five Chapter 1 2.5D macro-tile reference images based on the current saga/gameplay docs.
  - Use these to keep the macro-tile approach closer to the high-depth 3D look of the target references.

- `MacroTilePrototype/PremiumGameplayScene/`
  - One full-scene premium gameplay reference with runtime buildings, soldiers, vehicles, and VFX.
  - Use this to judge final gameplay art quality, not as importable terrain.

- `MacroTilePrototype/FictionalGulfStyle/`
  - One fictional Gulf-region premium gameplay reference with air/naval pressure.
  - Use this to evaluate a more specific WarlineCapture visual identity while avoiding real countries, cities, flags, landmarks, or real-world attack framing.
  - Includes `UsableIsoMaps/` with schema, macro-tile catalog, and three first large-map definitions.

## Future Macro-Tile Outputs

The next macro-tile validation outputs should be added under:

```text
Design/VisualReferences/2DIsometricProduction/MacroTilePrototype
```

Expected first outputs:

- straight road macro tile source/reference
- intersection macro tile source/reference
- Unity 2-tile connection capture
- macro tile metadata report
- 2x2 or 3x3 assembly capture after the first four tiles exist

## Unity Baseline Outputs

- ISO-01 scene: `Assets/Game/Scenes/DesignTargets/ISO01_CityCommand_TilemapSpike.unity`
- ISO-01 capture: `UnitySpike/ISO01_TilemapSpike_Capture.png`
- ISO-01 report: `UnitySpike/ISO01_TilemapSpike_Report.md`
- ISO-02 scene: `Assets/Game/Scenes/DesignTargets/ISO02_CityCommand_RuntimePrototype.unity`
- ISO-02 captures:
  - `RuntimePrototype/ISO02_RuntimePrototype_Start.png`
  - `RuntimePrototype/ISO02_RuntimePrototype_Mid.png`
  - `RuntimePrototype/ISO02_RuntimePrototype_End.png`
- ISO-02 report: `RuntimePrototype/ISO02_RuntimePrototype_Report.md`
- ISO-02 runtime scripts: `Assets/Game/Scripts/Iso2D`

## Removed Attempts

The following active-output folders were intentionally removed because they represent abandoned approaches:

- `CleanTerrain`
- `ModularTerrainUnityTarget`
- `HybridTerrainUnityTarget`
- `RoadSlabBitmapChunks`
- `RoadSlabChunkKit`
- `ConnectedRoadSlabComposition`
- `ConnectedRoadNetworkOverlay`
- `DeterministicRoadGraph`

Reason: they either failed visual quality, failed reliable road connection, or moved toward procedural visuals that do not meet the target art quality.

## Rule

Macro-tile art can be beautiful and baked. Gameplay remains separate through metadata and runtime entities.
