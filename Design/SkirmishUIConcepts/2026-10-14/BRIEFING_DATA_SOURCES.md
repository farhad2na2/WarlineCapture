# Briefing data sources

Recorded 2026-10-14. Audit of the briefing panel on the approved mockups [approved-2-battlefield-map-view.jpg](approved-2-battlefield-map-view.jpg) and [approved-3-battlefield-grid-view.jpg](approved-3-battlefield-grid-view.jpg). [approved-1-theater-map.jpg](approved-1-theater-map.jpg) has no TIME / SIZE briefing panel. Illustrative numbers on the mockups are not catalog data.

`SkirmishBriefingState` gains only fields that already exist on a catalog row, a compiled scenario definition, or the legacy preset clock. Existing fields stay.

| Briefing label | State field | Source |
|---|---|---|
| Scenario id (S007, S001) | `ScenarioId` | `SkirmishBattleCatalogEntry.ScenarioId` in `Assets/Game/Resources/SkirmishBattleCatalog.asset` |
| Title | `TitleLocKey` | `SkirmishBattleCatalogEntry.TitleKey`, else `ui.skirmish.{scenarioId}.title` |
| Description | `DescriptionLocKey` | `ui.skirmish.{scenarioId}.description`. English copy is `SkirmishBattleCatalogEntry.DescriptionEnglish`; it is not a baked time or size |
| TIME 15:00 | `DeadlineSeconds` | Compiled definition: `SkirmishScenarioDefinitionConfig.ObjectiveConfig.DeadlineSeconds(RecommendedSize)` (`SkirmishObjectiveConfig`; Base Assault Standard is 1080 seconds). Legacy playable row with no definition: `SkirmishPresetConfig.MatchDurationSeconds` (900), the same clock `SkirmishRulesSystem` uses when the session is not expanded. Planned row with no definition: 0. 15:00 is that legacy 900-second clock, not the compiled Base Assault deadline |
| SIZE 7 | — | NO SOURCE — Design to decide. No catalog or definition field is a unitless size integer. The digit 7 is `SkirmishStartPackageConfig.StandardStructureCount` after `ConfigureField()`, which is a structure count |
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
| Per-scenario enemy count | NO SOURCE — Design to decide. `SkirmishSetupBriefing` hardcodes one opponent. `QuickGameConfig.EnemyCount` is the legacy setup slider (default 1, clamp 1–3), not a catalog field |
