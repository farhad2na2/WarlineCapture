# Skirmish battle library view-model seam

Programmer 2 (WP3 art and WP4 SCN-13) binds the screen to this seam. Programmer 1 owns the medal ledger (WP1) and the real projection (WP2). The types below are the contract. The catalog adapter is a temporary reader so the screen compiles and can be captured before WP2 lands.

## Types

Assembly `Game.UI.Runtime`, namespace `Game.UI.Runtime`.

| Type | Role |
|---|---|
| `ISkirmishBattleLibraryProjection` | `Project(in SkirmishLibraryQuery)` returns a `SkirmishLibrarySnapshot`. |
| `SkirmishBattleLibraryCatalogAdapter` | Temporary projection. Reads the catalog, `ISkirmishMedalReadModel` and `SkirmishLibraryAccessEvaluator`. Does not award medals and does not choose Recommended Next. |
| `ISkirmishMedalReadModel` | WP1 read model. `Has`, `BattlesWon(map)`, `BattlesWon(map, objective)`, `TotalsByDifficulty`. `BattlesWon` counts scenarios with at least one medal. |
| `SkirmishMedalReadModelEmpty` | Assigned by default. Every count is zero. |
| `ISkirmishBattleLibraryAdvisor` | `TryRecommendNext` and `TryQuickBattle`. |
| `SkirmishBattleLibraryAdvisorUnimplemented` | Assigned by default. Both methods return false. The screen then shows “No recommendation yet.” Quick Battle opens the selected battlefield briefing and does not launch. |
| `SkirmishBattleLibraryServices` | Static replacement points: `Medals`, `Advisor`, `Access`. |
| `SkirmishLibraryAccessEvaluator` | `(catalogId, catalogPlayable) -> SkirmishLibraryCardAccess`. `SkirmishBattleLibraryAccessHost` in `Game.Composition` maps `ContentAccessRuntime.Evaluate("skirmish." + id)`. |

`SkirmishLibraryCardAccess`: `OwnedPlayable`, `NotOwned`, `Unavailable`, `Planned`. Medals never change access. Release builds omit `Planned`. Development builds show them as unavailable.

`SkirmishLibraryLevel` is Theatre or Battlefield. `SkirmishLibraryPlateMode` is Map or Grid. Grid is not a route.

## What Programmer 1 replaces

1. Assign `SkirmishBattleLibraryServices.Medals` to the ledger read model after WP1.
2. Replace `SkirmishBattleLibraryCatalogAdapter` with the WP2 projection, or assign an advisor that implements Recommended Next (first owned Playable scenario without a Regular medal, catalog order, battlefield with fewest wins; do not recommend NotOwned unless nothing owned remains).
3. Implement `TryQuickBattle` as a random owned Playable scenario at the remembered `SkirmishDifficultyId`. Never a Custom run. The screen selects that scenario and opens the briefing. It does not launch.
4. Put the authority’s reason string on `SkirmishLibraryCardModel.UnavailableReason` when the access state needs the existing reason text. The screen uses that string when it is non-empty.

The screen keeps `SelectScenarioId` and `SelectMapCollection`. `SelectScenarioId` still selects the catalog row and, when the row is playable, writes `Warline.Skirmish.LastScenarioId` and the scenario index. It also opens that row’s battlefield. `SelectMapCollection("")` returns to the theatre. A battlefield id highlights that battlefield and still filters the dormant list.

## Session keys

Beside the existing scenario key:

| Key | Value |
|---|---|
| `Warline.Skirmish.LibraryLevel` | `0` theatre, `1` battlefield |
| `Warline.Skirmish.LibraryMap` | `DB`, `CC`, `MP`, `IB`, `AP` |
| `Warline.Skirmish.LibraryObjective` | `BA`, `FC`, `BT`, `CE` |
| `Warline.Skirmish.LibraryPlate` | `0` map, `1` grid |
| `Warline.Skirmish.LibraryDifficulty` | `SkirmishDifficultyId` byte (`Regular` = 2) |

`QuickCustomScreenView.SelectedLibraryDifficulty` exposes the selector. This change does not rewrite the match-launch difficulty payload.

## Art

Textures live in `Assets/Game/Resources/SkirmishBattleLibrary/` so `Resources.Load` works in the player.

| File | Source |
|---|---|
| `theater_collage.png` | Illustrated collage. No baked lettering. Region rectangles are in `SkirmishBattleLibraryArt.TheaterRegions`. |
| `plate_db.png` | Campaign opening crop, desert / city edge (`native996` M01). |
| `plate_cc.png` | Dense-city full map. |
| `plate_mp.png` | Armor Break approach crop (`native1026` M05). Copy does not call it a mountain pass. |
| `plate_ib.png` | RefineryDistrict crop (`SupplyLine/native-pump-gate.png`). |
| `plate_ap.png` | CityEdgeAirfield crop (`native889`). |
| `preview_s001`–`s003` | Desert Base crops for the published rows. |
| `preview_s004` | CityEdgeAirfield crop. S004 keeps that binding. |
| `preview_s025`, `preview_s073` | Dense-city crops. S073 stays on the shared city source. |
| `medal_*.png` | Recruit bronze, Regular silver, Veteran gold, Commander red star, plus an empty ring. |

Planned scenarios use the battlefield plate. Pin centres are `SkirmishBattlefieldPinLayout` (six per objective). They are not painted into the plates.

Grid columns are the same six army/start slots (Ground, Air, Combined × Field, Established). A scenario that is not published leaves an empty cell. Cards are not packed to the left.

## Editor capture

Programmer 2 runs this on the Mac with the repository wrapper. Do not pass `-batchmode`.

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/warline-skirmish-library-capture.log -- \
  -quit -executeMethod Game.Editor.SkirmishBattleLibraryCapture.RunFocusedValidation
```

Pass marker: `[SkirmishBattleLibraryCapture] result=Passed`

PNGs: `Design/AgentReports/SkirmishBattleLibrary/2026-10-14/{theater|map|grid}-{16x9|20x9|4x3}-{en|fa-IR}.png` (18 files).

Rebuild the SCN-13 prefab first if the plate nodes are missing from the asset:

```bash
Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/warline-skirmish-library-prefab.log -- \
  -quit -executeMethod Game.Editor.SkirmishSetupPrefabBuilder.Build
```

Prefab marker: `[SkirmishSetupPrefabBuilder] result=Passed`

Plate marker: `[SkirmishBattleLibraryPlate] result=Passed`

These captures are Editor renders. They are not visual sign-off, a normal-input playthrough, or device acceptance.
