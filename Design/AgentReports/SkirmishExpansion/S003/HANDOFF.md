# Mission 5 (S003) continuation handoff — 2026-09-23

**Read this first if you are the next agent on skirmish mission 5.** Everything below is
pushed on branch `cursor/skirmish-mission-5-runtime-repair-e8ac` (9 commits, head
`f3191438b` at handoff time). Base: `main` at `f07b3bcf5` (includes the S002 audit).

## Terminology (do not re-derive)

- Skirmish mission N = handoff work ordinal. Missions 1–3 = playable prototypes
  S001/S025/S073 (legacy runtime indices 0/1/3; index 2 is the Editor stress probe).
  Mission 4 = S002 (playable, dispatch index 4). **Mission 5 = S003** (Desert Base ·
  Base Assault · Air Mobile · Field Base), now Playable at dispatch index 5.
- Design brief: `Design/Roadmap/Skirmish_Expansion/Scenarios/DB_BA.md` § S003.
  Shared tech contract: `Design/Roadmap/Skirmish_Expansion/TECHNICAL_ARCHITECTURE.md`.
- The S002 audit (`Design/AgentReports/SkirmishExpansion/S002/AUDIT_2026-09-23.md`)
  found the expanded runtime bypassed the shared spawn/navigation/combat paths. The
  repair landed on this branch; see `RUNTIME_REPAIR_2026-09-23.md` beside this file for
  the finding-to-fix map.

## What this branch already did (do not redo)

1. **Packaged content loading** — player launch no longer reads the repo CSV or uses
   `CreateInMemory`. New: `Assets/Game/Resources/SkirmishExpansionCatalog.asset`
   (`SkirmishExpansionCatalogConfig`, script in `Configs/Skirmish/`) and
   `Assets/Game/Resources/SkirmishExpansion/InitialSetupMatrix.csv`. Launch:
   `SkirmishLaunchProjection.TryQueueExpanded(em, catalogId, seed)` handles S002 (index 4)
   and S003 (index 5).
2. **Map-relative deployment** — `SkirmishMapLayoutConfig.worldBinding` pins the authoring
   frame onto the operation map's deployment anchors (949,344.7 / 1686,108). All 24 anchors
   and pads project inside the 2048×1024 bounds (verified by
   `SkirmishMapLayoutValidation` + tests). Editor re-derivation:
   `Tools/Warline/Skirmish/Derive Desert Base World Binding` (expects `pinnedMatch=1`).
3. **Camera focus retry** — `SkirmishPresentationSystem` latches the opening focus only
   after the camera request is written.
4. **HUD parity** — expanded sessions show the 1080 s deadline, 48 infantry cap, and the
   expansion materials stock (`UiShellEcsGateway.Skirmish.cs`,
   `UiMatchHudResourceReadModelSystem.cs`).
5. **Entity prefab spawning** — `SkirmishScenarioSpawnSystem.CreateForceMember`
   instantiates baked registry prefabs (same source as `InitialUnitsSpawnSystem`), with
   map grounding + `UnitGrid`. Bare-entity + GameObject stand-in remains only for
   registry-less Editor worlds. Production dispatch uses the same path.
6. **Shared movement** — `SkirmishWorldMovementService` keeps `UnitPathFollow`; units with
   `UnitMove` and an in-grid destination move via `UnitPathRequest`. Direct step remains
   for grid-less Editor worlds and a latched 3 s stall recovery. `DefaultAdvance` reads the
   enemy staging pad from the resolved setup.
7. **S003 enablement** — catalog row Playable at index 5; `QuickGameConfig` normalization
   preserves 5; `SkirmishBattleCatalogConfig` validation pins S003@5; copy keys compile
   into `SkirmishResolvedSetup` and HUD/library copy resolves generically via the
   `skirmish.sNNN.*` convention (S003 EN/FA strings already merged).
8. **Tests** — new: `LibraryDispatchIndexQueuesAirMobileS003`, `PackagedMatrixMatchesDesignCsv`,
   `PackagedCatalogResolvesAuthoredDefinitions`, `WorldBindingProjectsDeploymentOntoMapAnchors`,
   `WorldBindingMissingFailsValidation`. Updated envelope-coordinate assertions to map-space;
   fixed the stale S004 manifest assertion (pre-existing main drift from commit `6678b47d4`).

## What remains (in order)

### 1. Focused Editor validation (any Editor machine; macOS: wrapper only, never -batchmode)

```bash
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/s5-definitions.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation
# marker: [SkirmishExpandedDefinitionTests] result=Passed
Tools/CI/invoke_unity_macos.sh --timeout 900 --log /private/tmp/s5-catalog.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation
# marker: [SkirmishExpandedCatalogTests] result=Passed
```

Also run the other SkirmishExpansion suites (`SkirmishExpandedVisualTests`,
`SkirmishExpandedArmyTests`, `SkirmishExpandedEconomyTests`, `SkirmishExpandedObjectiveTests`,
`SkirmishExpandedCheckpointTests`, `SkirmishExpandedAriaTests`,
`SkirmishS002AriaHarnessTests`) — same pattern. Fix what fails; the most likely failure is a
test still asserting an envelope coordinate.

### 2. Live Game View evidence on the repaired runtime (Mac/Windows Skirmish shadow)

From the open Editor menus (wrapper `-quit` returns early; use the menu items):
- `Tools/Warline/Skirmish/Launch S003 Regular Standard Game View` (seed 104732).
  Verify: opening frame shows the player base/army on the loaded map (not blank),
  units render via the shared presentation, HUD reads 18:00 / 48 infantry / 450 materials,
  movement follows paths around obstacles. Evidence lands in
  `Design/AgentReports/SkirmishExpansion/S003/_Evidence/`.
- Re-capture S002 the same way (`Launch S002 Regular Standard Game View`) — the spawn and
  movement repair is shared, so S002's retained evidence is stale.

### 3. Counted ARIA matrices (S003 then S002 re-run)

S003: seeds 104732/130366/155924 × EN/FA, 3 runs per locale, ≥2 real wins each, normal
speed, zero input violations/interventions. Harness: `SkirmishS002AriaRunHarness` already
drives S003/S004 watches (see `Design/AgentReports/SkirmishExpansion/S002/acceptance.md`
for the run-log rules; `runs.csv` starts header-only, never pre-write Victory).

### 4. Remaining runtime redo items (audit "redo" list, need live validation)

- **Structures via `BuildingRuntimeSpawnRequest`** — expanded structures still spawn as
  bare entities + GameObject stand-ins. Reconnect to the shared building pipeline
  (`InitialUnitsSpawnSystem.EnqueueConfiguredInitialBuildingSpawnRequest` is the reference)
  with objective-role rebinding after spawn completion.
- **Shared combat execution** — prefab units currently carry
  `CampaignMissionCombatSuppressedTag` so the fog-aware overlay engagement stays the single
  combat path. Reconnecting `UnitEngagementSystem` requires fog-aware acquisition first
  (hidden contacts must stay undamageable); validate balance live.
- **Field/Established backbone structures** — matrix expects 7 (F) / 10 (E) starting
  structures; the compiler spawns Barracks + Ground Staging (+ Helipad for Established Air).
  MATCH_SETUP.md lists the facilities (oil pump, refinery, Fuel Bladder, ammo depot,
  watchtower; Established adds 2nd Barracks, 2nd watchtower, Satellite Dish, Helipad).
  Needs pads in the layout, visual keys, and real economy function.
- **Air motion for S003's Air Mobile roster** — attack pass/return/landing/refuel are open
  (see S003 SLICE_NOTES.md "Remaining gaps"). Field start cannot queue offensive air until
  Helipad + readiness are paid for — that gating already works.

### 5. Certification

Only after the above: the guarded flips (`Flip S003 Playable If Evidence Ready` is already
written into the manifest row) and the acceptance matrix in the S003 report folder. Do not
mark Accepted from asset existence or Editor-only fixtures.

## Rules you must keep

- macOS: `Tools/CI/invoke_unity_macos.sh` for every Unity run; GUI licensing; never
  `-batchmode`; never kill Unity/Hub/licensing processes; keep Hub open and signed in.
- Windows: `Tools/CI/InvokeUnityExecuteMethodValidation.ps1` with explicit log + timeout +
  required pass marker.
- No per-scenario C# controllers, no per-S-ID switch cases, no forced Victory, no army
  injection, no pre-written evidence rows, no repo-path reads on player paths.
- Legacy prototypes (indices 0/1/3) and the stress probe (2) stay untouched; expanded
  content never goes through `NormalizeForBaseAssault` collapse.
- One publication row per catalog ID; evidence under
  `Design/AgentReports/SkirmishExpansion/S003/`.

## Key file map

| Area | Files |
|---|---|
| Launch | `Assets/Game/Scripts/Composition/SkirmishLaunchProjection.cs`, `Composition/Skirmish/SkirmishExpandedLaunchProjection.cs`, `SkirmishExpandedLaunchResolver.cs` |
| Packaged content | `Configs/Skirmish/SkirmishExpansionCatalogConfig.cs`, `Resources/SkirmishExpansionCatalog.asset`, `Resources/SkirmishExpansion/InitialSetupMatrix.csv`, `Configs/Skirmish/SkirmishSetupMatrixTable.cs` |
| Layout/binding | `Configs/Skirmish/SkirmishMapLayoutConfig.cs` (worldBinding), `SkirmishMapLayoutBuilder.cs` (Configs: pin + ProjectPad), `SkirmishMapLayoutValidation.cs`, `Editor/Skirmish/SkirmishMapLayoutBuilder.cs` (derivation) |
| Spawn | `Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs`, `SkirmishVisualSpawnService.cs`, `SkirmishProductionService.cs` |
| Movement/combat | `Runtime/Skirmish/SkirmishWorldMovementService.cs`, `SkirmishExpandedEngagementService.cs`, `Components/Skirmish/SkirmishExpandedSessionComponents.cs` |
| HUD | `UI/Shell/Ecs/UiShellEcsGateway.Skirmish.cs`, `UiMatchHudResourceReadModelSystem.cs`, `Runtime/Skirmish/SkirmishExpandedHudCopy.cs`, `Configs/Skirmish/SkirmishExpandedCopyProjection.cs` |
| Catalog/publication | `Resources/SkirmishBattleCatalog.asset`, `Configs/SkirmishBattleCatalogConfig.cs`, `Configs/Skirmish/SkirmishPublicationValidator.cs`, `Editor/Skirmish/SkirmishPublicationFlipMenu.cs` |
| Tests | `Assets/Tests/Editor/SkirmishExpansion/*` (focused suites + `RunFocusedValidation` entry points) |
