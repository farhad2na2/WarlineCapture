# ISO-01 Golden Assets

Date: 2026-05-05

This folder contains the first generated 2D-isometric golden assets for the City Command production spike.

Current terrain note:

- Road/plaza tile assets in this folder are historical spike assets.
- The active terrain path is large macro tiles plus metadata.
- Unit, building, prop, and overlay assets may still be useful as runtime sprites or references.

## Source Folders

- `SourceChroma`
  - Original generated source renders on chroma-key backgrounds.
- `Transparent`
  - Chroma-key-removed transparent PNGs used for review and Unity import.
- `Assets/Game/Art/Generated/2DISO/GoldenAssets`
  - Unity import location for the same transparent PNGs.

## Generated Assets

| ID | File | Role |
|---|---|---|
| `GA-01` | `GA-01_RoadStraight.png` | Isometric asphalt road tile |
| `GA-02` | `GA-02_RoadIntersection.png` | Isometric road intersection tile |
| `GA-03` | `GA-03_ConcretePlaza.png` | Concrete plaza/base tile |
| `GA-04` | `GA-04_ForwardCommandHQ.png` | Friendly command HQ landmark |
| `GA-05` | `GA-05_EnemyCommandHQ.png` | Enemy command HQ landmark |
| `GA-06` | `GA-06_RuinedCityBuilding.png` | Ruined urban cover/landmark |
| `GA-07` | `GA-07_BarricadeRow.png` | Defensive line prop |
| `GA-08` | `GA-08_RifleSquad.png` | Friendly infantry squad unit |
| `GA-09` | `GA-09_APC.png` | Medium vehicle unit |
| `GA-10` | `GA-10_Tank.png` | Heavy vehicle unit |
| `GA-11` | `GA-11_RoadTurn.png` | Modular asphalt road turn tile |
| `GA-12` | `GA-12_RoadTJunction.png` | Modular asphalt road T-junction tile |
| `GA-13` | `GA-13_RoadEndCap.png` | Modular asphalt road end/cap tile |
| `GA-14` | `GA-14_CurbSidewalkTransition.png` | Curb/sidewalk transition tile |
| `GA-15` | `GA-15_DamagedRoadOverlay.png` | Damaged road/rubble overlay sprite |
| `GA-16` | `GA-16_ConcretePlazaAlt.png` | Alternate concrete plaza tile |
| `GA-17` | `GA-17_SelectionRing.png` | Friendly unit selection ring overlay |
| `GA-18` | `GA-18_MoveMarker.png` | Move command ground marker overlay |
| `GA-19` | `GA-19_AttackMarker.png` | Attack command ground marker overlay |
| `GA-20` | `GA-20_HealthBarFrame.png` | Health bar frame overlay |
| `GA-21` | `GA-21_HealthBarFill.png` | Health bar fill overlay |
| `GA-22` | `GA-22_SquadBadge.png` | Squad badge overlay |
| `GA-23` | `GA-23_CapturePointMarker.png` | Capture point marker overlay |

## Unity Spike

These assets are imported as sprites by `WarlineCaptureIso2DSpikeBuilder`.

Tile assets under `Assets/Game/Art/Generated/2DISO/Tiles`:

- `GA-01_RoadStraight`
- `GA-02_RoadIntersection`
- `GA-03_ConcretePlaza`
- `GA-11_RoadTurn`
- `GA-12_RoadTJunction`
- `GA-13_RoadEndCap`
- `GA-14_CurbSidewalkTransition`
- `GA-16_ConcretePlazaAlt`

`GA-15_DamagedRoadOverlay` remains a sorted SpriteRenderer overlay in the spike because damaged decals may need independent placement and gameplay state.

`GA-17` through `GA-23` remain sorted SpriteRenderer overlays in the spike because tactical markers, health bars, and badges must stay separate from terrain/unit/building art.

Manual validation output:

- `../UnitySpike/ISO01_TilemapSpike_Capture.png`
- `../UnitySpike/ISO01_TilemapSpike_Report.md`
