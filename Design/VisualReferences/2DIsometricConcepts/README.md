# WarlineCapture 2D Isometric Concepts

Date: 2026-05-05

These are exploratory AI-generated references for a possible 2D-isometric art direction.

They are not asset locks. They are meant to answer one question:

> Would WarlineCapture feel stronger as a premium 2D-isometric mobile RTS instead of trying to match generated references with the current 3D prefab set?

## Current References

| ID | Direction | Image |
|---|---|---|
| `ISO-01` | SCN-08-style RTS battle HUD over a dense 2D city battlefield | `ISO-01_RTSBattleHUD_CityCommand/ISO-01_RTSBattleHUD_CityCommand_Target.png` |
| `ISO-02` | Green town forward base with roads, houses, tents, towers, and helicopter staging | `ISO-02_GreenTown_ForwardBase/ISO-02_GreenTown_ForwardBase_Target.png` |
| `ISO-03` | Harbor fuel depot with docks, warehouses, fuel tanks, and logistics combat | `ISO-03_HarborFuelDepot/ISO-03_HarborFuelDepot_Target.png` |
| `ISO-04` | Mountain checkpoint with village, bridge, convoy route, and defensive perimeter | `ISO-04_MountainCheckpoint/ISO-04_MountainCheckpoint_Target.png` |

## Evaluation Questions

- Does 2D iso better match the desired premium reference quality?
- Are the units and vehicles readable enough at gameplay scale?
- Does the style still feel like WarlineCapture, or does it become a different game?
- Would we accept a pipeline based on generated/painted sprites, tilemaps, and sprite animation?
- Which environment direction has the strongest identity: city, green town, harbor, or mountain checkpoint?

## Production Caveat

If this direction is chosen, the next step is not to generate every asset. The next step is a small production spike:

1. Choose one scene direction.
2. Generate or paint 10-20 reusable isometric assets.
3. Import them into Unity as sprites/tilemaps.
4. Test sorting, scale, readability, and selection/UI overlays in-engine.
5. Decide whether the pipeline is reliable enough for the full game.

