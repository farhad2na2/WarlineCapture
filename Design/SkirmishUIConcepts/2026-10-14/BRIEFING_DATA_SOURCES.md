# Briefing data sources

Recorded 2026-10-14. Audit of the briefing panel on the approved mockups [approved-2-battlefield-map-view.jpg](approved-2-battlefield-map-view.jpg) and [approved-3-battlefield-grid-view.jpg](approved-3-battlefield-grid-view.jpg). [approved-1-theater-map.jpg](approved-1-theater-map.jpg) has no TIME / SIZE briefing panel. Illustrative numbers on the mockups are not catalog data.

`SkirmishBriefingState` gains only fields that already exist on a catalog row, a compiled scenario definition, or the legacy preset clock. Existing fields stay.

| Briefing label | State field | Source |
|---|---|---|
| Scenario id (S007, S001) | `ScenarioId` | `SkirmishBattleCatalogEntry.ScenarioId` in `Assets/Game/Resources/SkirmishBattleCatalog.asset` |
| Title | `TitleLocKey` | `SkirmishBattleCatalogEntry.TitleKey`, else `ui.skirmish.{scenarioId}.title` |
| Description | `DescriptionLocKey` | `ui.skirmish.{scenarioId}.description`. English copy is `SkirmishBattleCatalogEntry.DescriptionEnglish`; it is not a baked time or size |
| TIME 15:00 | `DeadlineSeconds`, `ShowTime` | Compiled definition: `SkirmishScenarioDefinitionConfig.ObjectiveConfig.DeadlineSeconds(RecommendedSize)` (`SkirmishObjectiveConfig`; Base Assault Standard is 1080 seconds). Legacy playable row with no definition: `SkirmishPresetConfig.MatchDurationSeconds` (900), the same clock `SkirmishRulesSystem` uses when the session is not expanded. Planned row with no definition: 0. 15:00 is that legacy 900-second clock, not the compiled Base Assault deadline. `ShowTime` is true when `DeadlineSeconds > 0`, including the 900-second fallback. A planned row with no definition stays at 0, so `ShowTime` is false |
| SIZE | `SizeLocKey`, `ShowSize` | Localized `RecommendedSize` name. `ShowSize` is true only when a compiled definition is found and the size is Standard, War, or Large War. Keys: `ui.skirmish.size.standard` (Standard / استاندارد), `ui.skirmish.size.war` (War / جنگ), `ui.skirmish.size.large_war` (Large War / جنگ بزرگ). `ShowSize` is false when no compiled definition exists. `StartingStructureCount` is not the SIZE label |
| OBJECTIVE | `ObjectiveLocKey` | `SkirmishBattleCatalogEntry.ObjectiveId` mapped to `ui.skirmish.objective.ba`, `.fc`, `.bt`, or `.ce` |
| STARTING FORCES army | `ArmyLocKey` | `SkirmishBattleCatalogEntry.ArmyProfile` mapped to `ui.skirmish.army.{g\|a\|c}` |
| STARTING FORCES start | `StartLocKey` | `SkirmishBattleCatalogEntry.StartProfile` mapped to `ui.skirmish.start.{f\|e}` |
| MEDALS | `Medals` | `ISkirmishMedalReader` / `SkirmishMedalLedger` |
| DIFFICULTY | `AvailableDifficulties`, `SelectedDifficulty` | Playable rows expose Recruit, Regular, Veteran, and Commander (`SkirmishDifficultyId`). Selection is library prefs. `SkirmishScenarioDefinitionConfig.DefaultDifficulty` is Regular. The four `SkirmishDifficultyConfig` assets are global, not a per-scenario subset |
| PURPOSE | `PurposeLocKey` | `ui.skirmish.{scenarioId}.purpose` |
| START BATTLE | `CanLaunch` | Battlefield level, card access, selected scenario, and a medal difficulty. `SkirmishExpandedLaunchResolver` stays the launch gate |

## Real facts that are not the mockup SIZE label

These are filled only when `SkirmishExpansionCatalogConfig.TryGetDefinition` finds the scenario (`SkirmishLibraryComposition.ReadBriefingSource`). Otherwise they stay unset (`SkirmishSizeId.None` / 0), including for a playable legacy row whose deadline falls back to 900.

| State field | Source |
|---|---|
| `RecommendedSize` | `SkirmishScenarioDefinitionConfig.RecommendedSize` (`SkirmishSizeId`: Standard, War, LargeWar). `None` on the definition is read as Standard |
| `StartingStructureCount` | `SkirmishScenarioDefinitionConfig.StartPackageConfig.StandardStructureCount` (`SkirmishStartPackageConfig`; Field 7, Established 10) |
| `MapWidthMetres`, `MapDepthMetres` | `SkirmishScenarioDefinitionConfig.MapLayout.WorldWidthMetres` / `WorldDepthMetres` (`SkirmishMapLayoutConfig`; Desert Base constants 600 x 420) |

## Audited, not on the approved briefing panel

| Candidate | Result |
|---|---|
| Per-scenario enemy count | NO SOURCE. `SkirmishSetupBriefing` hardcodes one opponent. Do not bind `QuickGameConfig.EnemyCount`. It is the legacy setup slider (default 1, clamp 1–3), not a catalog field |

## Library plates and card previews

`Game.Editor.SkirmishLibraryPreviewCapture.Run` writes 512×288 PNGs. Plates for Desert Base, City Crossroads, and Industrial Basin stay on the operation map's planning camera. Mountain Pass and Airfield Plains have no skirmish operation-map asset. The capture opens the existing playable campaign scene (`CH04M05ArmorBreak` / Armor Break, `CH04M04GroundedSignal` / Grounded Signal). It uses a camera whose id contains `overview` when one exists, otherwise a top-down orthographic frame of the playable bounds (world bounds when those extents are empty). Canvases and fog behaviours on that scene are disabled. If that scene is missing, the plate stays the logical key `skirmish.plate.MP` or `skirmish.plate.AP` and the log is `plate=Placeholder ... reason=NoPlayableMap`. No new terrain is authored.

Card previews are not the plate camera. Each published scenario is framed from its compiled definition when one exists: air armies use `air.player`, an established ground start uses `base.player`, and a field start uses `staging.player` then `base.player`. The enemy point is the objective's enemy-base role (`base.enemy` for Base Assault). A scenario with no definition uses the operation map's faction deployment anchors (an air army prefers that faction's helipad, then runway). The card camera is a closer perspective oblique between those points, tighter for an established start. It must not match the planning camera. S073 moves off the Industrial Basin planning camera that sits on the rock.

Before the three warm-up renders, the capture applies the match presentation. `MatchSceneView` adds `SkirmishS004CampaignLighting` when the loaded operation-map scene has an authored directional sun. `Configure` copies that scene's `RenderSettings`, turns fog off, and calls `ApplyDaylightFill`. The Match scene's Global Volume uses `Military_Demo_Gameplay`. Desert Base's runtime scene has no directional and a dim blue skybox ambient, so the capture supplies the Match scene directional (intensity 1.25, warm colour, the Match sun rotation), applies the same daylight fill, assigns a global volume with that profile, and renders with HDR and post-processing on. The pass marker stays `[SkirmishLibraryPreviewCapture] result=Passed`.
