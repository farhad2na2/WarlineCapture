# Lane worker 24 — 2026-11-18

Logs: /Volumes/T7/Projects/WarlineCapture-program-logs/2026-11-18/lane24/. No gameplay changes.

## Step 1 — FC/BT/CE copy merge
- Merged missions/fcbt-result-copy 2e9b72c88 --no-ff (94398bbab), no conflicts. Compile clean: `[SkirmishNativeRenderingDiagnosticSerialization] result=Passed` (compile.log:1771), 0 `error CS`. Python `unittest discover` 148 OK (pytests.txt).
- EditMode run 1 (tests.xml, no -quit; Editor exited by itself, exit 2):
  | class | passed / total |
  |---|---|
  | Game.Tests.Editor.SkirmishObjectiveCopyTests | 6 / 6 |
  | Game.Tests.Editor.SkirmishEndReasonProjectionTests | 28 / 28 |
  | StartThroughM02LocalizationTests | 7 / 9 |
  Failures: `EveryConfiguredStringResolves...` en/ui.skirmish.library.zone_progress empty (key missing from the catalog asset; config added in e28cf2c95, before this merge); `LoadingProgressRendersEveryPhase...` 'Preparing command interface' Farsi loses its ZWNJ (expected آماده‌سازی, got آمادهسازی).
- Catalog out of sync -> ran `Game.Editor.V3UiLocalizationCatalogBuilder.ApplyConfiguredUiTables` (menu "Apply Configured UI Tables", no Play mode) with -quit: `[ConfiguredUiTables] result=Passed configEntries=1058` (apply-tables.log:552). Key-set diff: only `ui.skirmish.library.zone_progress` added in en and fa-IR, nothing removed. Committed f18c5a30a.
- EditMode run 2 (tests2.xml, exit 2): 6/6, 28/28, StartThrough 7/9. Remaining failures (not catalog sync, so stopped after 2 attempts):
  - `fa-IR/ui.unit.type.transport_vehicle` expected `خودروی ترابری` got `ماشین حمل‌ونقل`: two configs share source `TRANSPORT VEHICLE` with different Farsi (SharedUiBindingRepairStrings.json ui.unit.type.transport_vehicle vs StartThroughM02UiStrings.json ui.catalog.transport_vehicle); Apply maps by source, so one wins. Needs a copy decision (which Farsi) or distinct sources.
  - `LoadingProgressRendersEveryPhase...` ZWNJ dropped by UIShellLoadingProgressView (catalog asset holds ‌). Code-side, not the catalog.
  - Neither failure involves the FC/BT/CE copy; both new copy classes pass.

## Step 2 — S004 pacing measurement (one queue, --menu-journey, --no-table; q1/unity.log; Editor exited by itself, exit 0)
First-visit S004 seeds from SkirmishAcceptanceScaffolding.TryCreateFirstVisitS004: 104733, 130367, 155925 (no rejections).

| job | result | match clock | % of 1080 | gate-3 unused= | menu-journey |
|---|---|---|---|---|---|
| S004 104733 en | Victory | 174.2 s | 16.13% | building.airport;role.aa;role.apc_fast;role.attack_heli;role.breacher;role.drone;role.fighter;role.marksman;role.radar;role.strike;role.transport_heli;role.transport_plane | Passed (:2264) |
| S004 130367 en | Victory | 175.6 s | 16.26% | same 12 | Passed (:417144) |
| S004 155925 en | Victory | 185.0 s | 17.13% | same 12 | Passed (:881598) |

- Menu-journey line (all three, seed differs): `[SkirmishMenuJourney] catalog=S004 result=Passed path=Hotspot>ObjectiveTab_BA>BattleCard_S004>StartBattleButton seed=<seed> size=Standard difficulty=Regular input=Touch matchStartedFromMenu=1`.
- ARIA used apc_armored, attack_heli_light, car, gunner, rifle, rocketeer, tanker in every job; clock_source=TerminalFrozen; result_return_ok=yes.
- With lane 21 (179.8 s, 16.6%) and lane 20 (174.3 s, 16.14%): five S004 runs at 174–185 s, 16.1–17.1% of 1080, all below the 20% band. Consistent across seeds.
