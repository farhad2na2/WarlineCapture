# Skirmish mission 5 runtime repair — 2026-09-23

Catalog identity **S003** (Desert Base · Base Assault · Air Mobile · Field Base), handoff
ordinal 5. This lane applies the [S002 library launch audit](../S002/AUDIT_2026-09-23.md)
to the shared expanded runtime and wires S003 for player launch. Source-only environment
(no Unity Editor on the authoring VM); live evidence is re-captured through the wrapper
lane listed at the bottom.

## Audit findings addressed

| Audit finding | Repair in this lane |
|---|---|
| P1 — deployment coordinates do not match the loaded map | `SkirmishMapLayoutConfig` gains a pinned `worldBinding` (origin/forward/across/usable metres + source anchor ids). Desert Base binds the normalized authoring frame onto the operation map's authored deployment anchors (`anchor.skirmish.desert_base_01.deployment.faction_1/2` at 949,344.7 / 1686,108). `SkirmishMapLayoutBuilder.BindRegularStandard` projects every pad/force/structure through the binding. `SkirmishMapLayoutValidation` now fails a layout with no binding, any anchor/pad projecting outside the 2048×1024 world bounds, or base anchors missing the deployment anchors. The across extent is fitted to 300 m so all 24 anchors and every pad stay inside the map; `Tools/Warline/Skirmish/Derive Desert Base World Binding` re-derives the frame from the map definition and reports drift. |
| P1 — startup focus not retried | `SkirmishPresentationSystem` latches the opening focus only after the camera request is actually written; a missing base transform or camera boundary now retries on later frames instead of dropping the opening frame. |
| P1 — unit presentation bypasses the entity spawning path | `SkirmishScenarioSpawnSystem` resolves baked prefab entities from the shared `UnitPrefabRegistryEntry` registry (the same source `InitialUnitsSpawnSystem` consumes) and instantiates them with faction, role, ownership, grid cell, and map-grounded transforms. GameObject stand-ins remain only as the no-registry fallback (Editor fixture worlds) and stay flagged through `SpawnVisualPending`. Production dispatch (`SkirmishProductionService`) uses the same prefab path. Prefab units carry `CampaignMissionCombatSuppressedTag` so the session's fog-aware objective combat stays the single combat path; shared auto-engagement reconnection is the remaining redo item below. |
| P1 — movement bypasses obstacle-aware path following | `SkirmishWorldMovementService` no longer strips `UnitPathFollow`. When the unit carries real movement data and the destination resolves inside the loaded grid, the shared `UnitPathRequest` → pathfinding pipeline owns movement and the intent clears on arrival. The local direct step remains for grid-less Editor worlds and as a latched 3-second stall recovery, never the default. `DefaultAdvance` now reads the enemy staging pad from the resolved setup (map frame) instead of the stand-in extent frame. |
| P1 — HUD describes a different rules/economy model | `UiShellEcsGateway.Skirmish` reads the expanded objective clock (1080 s Standard deadline) and `SkirmishCapacityComponent.InfantryCap` (48) for expanded sessions instead of the legacy 900 s / 24 constants. `UiMatchHudResourceReadModelSystem` projects `SkirmishEconomyStockComponent` into the materials header for expanded sessions instead of misreading the legacy tactical component as 0/0. |
| P1 — normal launch depends on a repository-only CSV | Player launch now loads the packaged matrix (`Resources/SkirmishExpansion/InitialSetupMatrix.csv`, TextAsset) and the authored definitions through the new packaged catalog (`Resources/SkirmishExpansionCatalog.asset` -> `SkirmishExpansionCatalogConfig`). No `Application.dataPath/..` read and no `CreateInMemory` on the player path; the design CSV stays the Editor test-vector source and a focused test fails on drift between the two copies. |
| P2 — retained visual acceptance does not demonstrate gameplay readiness | No evidence is re-stamped from this lane. The S002/S003 Game View PNG/JSON pairs and the counted ARIA matrix must be re-captured on the repaired runtime before certification; the checked-in manifest rows are certification records, not proof of the repaired integration. |

## S003 player enablement

- Catalog row S003 is Playable at library dispatch index 5 (`DefinitionId skirmish.s003`,
  `ReadinessManifestId publication.skirmish.s003`), next free index after S002's 4; index 2
  stays the Editor stress probe. `QuickGameConfig.NormalizeForBaseAssault` preserves index 5;
  `SkirmishLaunchProjection` compiles `skirmish.s003` (Air Mobile / Field) from packaged
  content and queues the expanded session on the Desert Base map hint.
- `SkirmishPublicationValidator.ApplyLegacyPrototypeCompatibility` keeps S003 playable at 5
  across catalog rebuilds; catalog validation requires S001@0, S025@1, S073@3, S002@4, S003@5.
- Compiled setups now carry the definition's copy keys, and the HUD/library copy resolves
  for any expanded entry through the `skirmish.sNNN.*` key convention (S003 EN/FA strings are
  already merged in `V3UiLocalizationCatalog.asset`).

## Deliberately not in this lane

- Structure spawning through `BuildingRuntimeSpawnRequest` (async shared building pipeline
  with objective rebinding) — needs live placement verification on the map.
- Shared combat execution (fog-aware auto-acquisition) replacing the overlay engagement
  service — needs live balance validation.
- Field/Established backbone structures beyond Barracks/Ground Staging/Helipad (the matrix's
  7/10 starting-facility rows) and air flight/return/refuel — tracked in the S003 slice notes.
- War / Large War stay unbound to the measured layout (first visit remains Regular Standard).

## Validation lane (macOS shadow, wrapper only)

Run on the Skirmish shadow with Unity Hub open; never `-batchmode` on macOS.

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/s003-definitions.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation
# required marker: [SkirmishExpandedDefinitionTests] result=Passed
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/s003-catalog.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation
# required marker: [SkirmishExpandedCatalogTests] result=Passed
```

Then the live evidence, from the open Editor via the menus (the wrapper's `-quit` does not
wait for play-mode completion):

1. `Tools/Warline/Skirmish/Derive Desert Base World Binding` — expect `pinnedMatch=1`.
2. `Tools/Warline/Skirmish/Launch S003 Regular Standard Game View` — verify the opening frame
   shows the player base/army on the loaded map, units render through the shared presentation,
   and the HUD reads 18:00 / 48 infantry / 450 materials.
3. Re-capture S002 Game View evidence identically (the repaired spawn/movement path is shared).
4. S003 ARIA matrix: seeds 104732/130366/155924 x EN/FA on the shared fail-fast harness;
   counted wins require normal speed, zero input violations, zero human interventions.
5. Standalone build check: launch S003 from the library in a packaged player (no Design
   directory present).
