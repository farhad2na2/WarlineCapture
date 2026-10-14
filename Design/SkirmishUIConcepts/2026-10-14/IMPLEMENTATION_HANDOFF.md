# Skirmish battle library redesign: implementation handoff

Recorded 2026-10-14. **Design handoff only; nothing below is implemented.** Visual direction and owner decision: [README.md](README.md).

## Goal

Replace the 120-row scrolling list in SCN-13 Skirmish Setup with a three-level battle library, and add per-difficulty medals as the only Skirmish progression.

| Level | Mockup | Shows |
|---|---|---|
| Theater | [approved-1-theater-map.jpg](approved-1-theater-map.jpg) | Five battlefields (DB, CC, MP, IB, AP) with won/24, medal totals, Recommended Next, selected battlefield summary. Footer: Back, Filters, Zoom In, Quick Battle. |
| Battlefield — Map | [approved-2-battlefield-map-view.jpg](approved-2-battlefield-map-view.jpg) | Objective tabs BA/FC/BT/CE with won/6; six pinned cards (G/A/C × F/E) for the selected objective; MAP/GRID toggle; briefing. Footer: Back, Theater Map, Start Battle, Advanced. |
| Battlefield — Grid | [approved-3-battlefield-grid-view.jpg](approved-3-battlefield-grid-view.jpg) | Same battlefield as 4 objective rows × 6 army/start columns; same briefing and footer as Map. GRID is a state of the battlefield level, not a separate route or tab strip. |

Not in scope (owner did not select): Skirmish/Operations rank ladders, combined Command Rank, main-menu mode-card progress, Operations city-progress view. Exploration images remain in [exploration/](exploration/) for history only.

## Binding rules

Read first: [BATTLE_CATALOG.md](../../Roadmap/Skirmish_Expansion/BATTLE_CATALOG.md) (Selection experience), [MATCH_SETUP.md](../../Roadmap/Skirmish_Expansion/MATCH_SETUP.md), [CAMPAIGN_MAP_REUSE_POLICY.md](../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md), [Mission_Product_Contract_2026-09-28.md](../../Monetization/Mission_Product_Contract_2026-09-28.md), and the Mission UI / Unity rules in `AGENTS.md`.

- **Medals never gate content.** Every published, owned scenario stays selectable at every level. No locks derived from medals.
- **Medal = one Victory on one scenario at one difficulty.** Recruit bronze, Regular silver, Veteran gold, Commander red star. A medal does not imply lower-tier medals. Draw, Defeat, TechnicalFailure, Surrender, Custom (`IsCustom != 0`) and legacy prototype (`IsLegacy != 0`) runs earn nothing. Seed and certified size changes stay eligible. Old prototype wins are not migrated into medals.
- **Theater plate is an illustrated collage, not one continuous map.** Battlefields run on existing Campaign sources; card and battlefield previews come from each scenario's actual source capture. Player copy follows actual geography (MP must not promise mountains). No new terrain or map work.
- **Ownership and availability come from existing authorities.** `ContentAccessRuntime.Evaluate("skirmish." + catalogId.ToLowerInvariant())` and `SkirmishBattleCatalogEntry.Status`. Only `Playable` entries are presented as playable. `Allowed` starts; `NotOwned` shows a lock and a store route; other states disable Start with the existing reason text. S001 is the free sample. The launch-boundary check in `SkirmishExpandedLaunchResolver` stays authoritative; UI state is only presentation.
- **Planned entries:** release builds show a battlefield's count from published entries only and do not advertise Planned cards as playable. Development builds may show Planned cards as clearly unavailable.
- Reuse Warline typography, frames and command button colours from SCN-05 Campaign District Atlas. All text is native localized TMP (EN/FA, RTL-safe); no text baked into art. Touch targets follow existing mobile spacing; pinch-zoom is optional, the Zoom In / Theater Map buttons are required.

## Current source seams

| Concern | Location | Notes |
|---|---|---|
| List screen | `Assets/Game/Scripts/UI/Screens/QuickCustomScreenView.cs`, `QuickCustomScreenView.Scenarios.cs` | Owns catalog load, `MapCollectionIds { "", "DB", "CC", "MP", "IB", "AP" }`, search, selection, `PresentCompiledBriefing`, `LastScenarioIdPrefsKey`. Keep `SelectScenarioId`/`SelectMapCollection` public API working for existing probes. |
| Prefab | `Assets/Game/Scripts/Editor/SkirmishSetupPrefabBuilder.cs` → `Assets/Game/Prefabs/UI/Shell/Content/SCN13_SkirmishSetupContent.prefab` | Route `UIRoute.QuickCustomSetup`; pass marker `[SkirmishSetupPrefabBuilder] result=Passed`. |
| Catalog | `Assets/Game/Scripts/Configs/SkirmishBattleCatalogConfig.cs` | Entry has `ScenarioId, MapId, ObjectiveId, ArmyProfile, StartProfile, Title*, Description*, Status, PlayableScenarioIndex, DefinitionId, ContentVersion`. Sort order already matches the grid (BA,FC,BT,CE × G,A,C × F,E). |
| Result | `Assets/Game/Scripts/Runtime/Skirmish/SkirmishResultSettlementService.cs` | `SkirmishExpandedResultJournal` is **in-memory only** and keyed by session. It already rejects Custom/legacy. Medal persistence is new. |
| Save | `Assets/Game/Scripts/Persistence/SaveDataModel.cs`, `SaveService.cs`, `SaveMigration` | Add data under `PlayerProfileSaveData`; bump migration; commit through the existing profile envelope. |
| Enums | `Game.Skirmish.Contracts.SkirmishDifficultyId` (Recruit=1…Commander=4), `SkirmishOutcomeKind` | Use these; do not add a parallel difficulty enum. |
| Access | `Assets/Game/Scripts/Missions/Contracts/ContentAccessAuthority.cs`, `Runtime/Campaign/ContentAccessRuntime.cs` | Canonical IDs `skirmish.s001`…`skirmish.s120`. |

## Work packages

### WP1 — Persistent medal ledger

- Add `SkirmishMedalSaveData[] skirmishMedals` to `PlayerProfileSaveData` with `catalogId`, `difficulty` (byte), `contentVersion`, `firstEarnedUtc`, `sessionId`. One row per catalogId + difficulty; first Victory wins, later victories are no-ops.
- Add a small `SkirmishMedalLedger` (runtime, pure C#) with `TryAward(receipt)`, `Has(catalogId, difficulty)`, `CountFor(mapId)`, `CountFor(mapId, objectiveId)`, `TotalsByDifficulty()`, `BattlesWon()` (scenarios with ≥1 medal).
- Hook after a successful `SkirmishResultSettlementService.TrySettleSession`: award only when `Outcome == Victory`, not Custom/legacy, catalog entry `Playable`. Persist in the same commit as the settled result so resume/duplicate settlement cannot double-award or lose the award.
- Save migration: empty array for existing profiles; no award from `QuickGameSaveData.lastResult`.
- Edit-mode tests: award once; idempotent duplicate session; Custom/legacy/Draw/Defeat rejected; save round-trip; migration from previous version; counts per map/objective.

### WP2 — Library view model

- Pure projection from catalog + access + ledger: per battlefield (published count, won count, medal grid 4×6), per objective tab (won/6), per card (title, army/start labels, four medal flags, access state, preview key).
- **Recommended Next:** first owned Playable scenario without a Regular medal, ordered by catalog order within the battlefield with fewest wins; deterministic and testable. Do not recommend NotOwned entries unless nothing owned remains.
- **Quick Battle:** random owned Playable scenario at the remembered difficulty; never a Custom run; opens the briefing selected instead of launching immediately.
- Remember level (Theater/Battlefield), battlefield, objective tab, MAP/GRID, selected scenario and difficulty in PlayerPrefs next to `LastScenarioIdPrefsKey`; restore after returning from a match.
- Search and Filters stay available (Filters opens objective/army/start/medal-missing filters); search results jump to the battlefield + objective containing the hit.

### WP3 — Art

- Theater plate: one ImageGen collage of the five battlefields in the SCN-05 district-plate style, labelled internally as illustrative; region outlines are runtime UI, not baked.
- Five battlefield plates: derived from each logical group's actual Campaign source capture (DB shared desert/dense city, CC shared city sectors, MP Armor Break approach, IB RefineryDistrict, AP CityEdgeAirfield/Grounded Signal), cropped consistently.
- Card previews: one per published scenario from its planning camera; Planned scenarios use the battlefield plate crop, never a fake gameplay capture.
- Medal icons: four round medals plus empty outline, matching existing V3 icon atlas. Card pin positions per battlefield are authored data (6 per objective), not baked into art.

### WP4 — Native UI

- Rebuild the SCN13 content in `SkirmishSetupPrefabBuilder` as Theater / Battlefield-Map / Battlefield-Grid states inside one route; reuse the existing briefing composition (`PresentCompiledBriefing`) for the right panel, extended with a four-slot medal row and difficulty selector.
- Card states: owned-playable, earned medals, selected, NotOwned (lock + store route), unavailable (disabled with reason). The selected card always shows the purpose line and the next player action (Start Battle).
- Layout at 16:9 and 20:9 reference sizes; EN and FA; large-text setting does not clip card titles.
- Result screen: on first medal for scenario+difficulty, show the earned medal on the existing result card; Back returns to the battlefield level with the scenario selected.

### WP5 — Validation

Follow the macOS Unity execution contract in `AGENTS.md` (`Tools/CI/invoke_unity_macos.sh`, no `-batchmode`, explicit log, timeout and pass marker). Report separately:

1. **Visual review:** native captures of Theater, Battlefield-Map and Battlefield-Grid at 16:9 and 20:9, EN and FA, compared with the approved mockups.
2. **Automated:** WP1 ledger tests, WP2 projection tests, prefab builder pass marker, existing `Assets/Tests/Editor/SkirmishSetupScreenTests.cs` and the S002 ARIA menu probe (`SkirmishS002AriaRunHarness.MenuProbe.cs`) still pass (update the probe to the new navigation).
3. **Normal-input journey:** Main Menu → Skirmish → Theater → Zoom In → objective tab → card → Start Battle → complete with ARIA Play → result shows medal → return lands on the same battlefield/card → medal visible on card, grid and theater totals → restart app → medal persists.
4. **Access:** fresh profile with only S001 owned shows NotOwned locks elsewhere and cannot launch them through the UI or the launch resolver.
5. **Device/player acceptance:** pending until performed on a real device; do not infer it from Editor captures.

## Open decisions for the owner

- Whether medals should be shown for battles on a different `contentVersion` after a balance update (proposal: keep earned medals; store version for audit only).
- Whether NotOwned cards show their full briefing or a purchase panel only.
- Whether the theater shows Planned battlefields with zero published battles in release builds (proposal: hide until at least one scenario is Playable).
