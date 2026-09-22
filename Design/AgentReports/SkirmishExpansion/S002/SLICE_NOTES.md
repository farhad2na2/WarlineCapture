# Skirmish mission 4 (Desert Base Base Assault expanded)

Catalog identity **S002** is named once here: Desert Base · Base Assault · Ground Maneuver · Established Base.
Handoff ordinal 4. First visit: Regular / Standard. Seed sample: `104731`.

Operations package 0 landed on `main` as `54b64e906` and is merged here with a
merge commit. That tree was not edited in this slice.

`origin/main` later moved to `b4161fb4` (Demo 2 asset-adoption docs only) and
is merged here. Demo 2 Industrial Basin logistics yard is **not** imported
into Skirmish mission 4 and does not replace this Desert Base milestone.

`origin/main` then moved to `10371a2cf` (Campaign Chapter 2 Mission 1
Gridlock, including `848a3cfe` / `73d2113c`) and is merged here with a
merge commit. This slice does **not** implement Gridlock. Shared
campaign / narrative / audio / localisation files were auto-merged;
`V3UiLocalizationCatalog` was not re-edited. Gridlock contracts stay on
main’s copies. Operations trees were not edited.

Game Design’s S002 EN/FA catalog keys landed on
`cursor/skirmish-s002-localization-35c1` at `87fffc598` and are merged here.
This slice did not edit `V3UiLocalizationCatalog`.

## What landed

### Shared contracts ticket (SK-00)
- New `Game.Skirmish.Contracts` assembly (`noEngineReferences`, auto-referenced).
  Consumer assemblies also list it explicitly (see `ASMDEF_REFERENCES.md`).
- Typed IDs/enums for catalog/definition/setup, size, difficulty, army, start,
  objective, roles, outcomes, checkpoint header, and reason codes.
- Legacy prototype map helper (0/1/3 + reserved stress 2).

### Compiler and session ticket (SK-01)
- Twelve config types under `Assets/Game/Scripts/Configs/Skirmish/`.
- `SkirmishResolvedSetup`, `SkirmishLaunchPayload`, session/ownership/objective
  components, and composition launch helper.
- Shared authored assets under `Assets/Game/Configs/SkirmishExpansion/Shared/`.
- `SkirmishSetupCompiler.TryCompile` compares army/start/logic/size vectors to
  `INITIAL_SETUP_MATRIX.csv` with field-specific reasons.
- `SkirmishDefinitionBuilder` + `SkirmishSetupCompilerValidation`.
- Session init / spawn / cleanup systems. Skirmish mission 4 Standard Regular
  starting forces/structures bind `UnitSourcePrefabKey` from the role catalog, so
  `SpawnVisualPending=0` when keys bind. GameObject instantiate is now in the
  SK-02 visual spawn service.
- Catalog entry gained optional `DefinitionId` / `ContentVersion` /
  `ReadinessManifestId` fields. Original `SCENARIO_CATALOG.csv` column order is
  unchanged. Publication lives in `SkirmishPublicationManifest.asset`.

### Ground roster and production visual ticket (SK-02)
- Typed `SkirmishRoleOverlay` + `SkirmishRoleOverlayCatalog` for infantry and
  Skirmish mission 4 ground vehicles (rifle/gunner/rocketeer/car/APC/tank plus
  Ground Maneuver-legal support). Overlays bake into the compiled snapshot with
  MATCH_SETUP Materials costs. `CapabilityCertified=false`.
- `SkirmishRosterProjectionSystem` applies overlays once (health/damage/range/
  producer/target domains). Blanket prototype rifle tuning is not used on
  expanded sessions.
- Infantry production accepts **4 members per squad**. Vehicles require Ground
  Staging. Ground Maneuver still rejects offensive air.
- Ground Staging is a typed producer + compiled starting structure (visual key
  `Building_GroundStaging`). Not a new art prefab.
- Spawn expands force quantity, binds `Faction` + `UnitSourcePrefabKey` for
  every Skirmish mission 4 starting member. `SpawnVisualPending=0` for Standard
  Regular when keys bind.
- `SkirmishVisualSpawnService` instantiates GameObjects from
  `SkirmishVisualPrefabCatalog` (live unit registry when loaded, otherwise
  named presentation stand-ins). Starting tank / APC / infantry and Ground
  Staging become visible objects; they are no longer ECS stubs only.
- Expanded S002 launch (`TryCompileAndQueue` / `TryQueue`) binds an explicit
  scene `UnitPrefabRegistryAuthoringConfig` when the caller supplies it, so a
  live match prefers real unit prefabs over presentation stand-ins. Ledger-only
  tests still omit the registry so they do not leak GameObjects.
- Ground Staging is persisted at
  `Assets/Game/Prefabs/Skirmish/Building_GroundStaging.prefab` with authored
  config `Assets/Game/Configs/SkirmishExpansion/Shared/SkirmishGroundStaging.asset`.
  `SkirmishGroundStagingPrefabAccess` loads that prefab in the Editor, then a
  Resources key, then the builder. The optional menu
  `Tools/Warline/Skirmish/Rebuild Ground Staging` still rewrites the same path.
- `SkirmishGroundStagingBuilder` builds a modular yard: separate
  **VehicleQueue** and **LogisticsQueue** surfaces, **SpawnPad** / **RallyPad**,
  Health / Selection / BuildControls sockets, and EN/FA identity
  (“Ground Staging” / «سکوی زمینی»). It is not an Expert Tent rename.
- `SkirmishVisualSpawnSystem` attaches missing visuals after ledger spawn.
  Paid produce also instantiates when a catalog is bound. Cleanup destroys
  the GameObjects.
- `SkirmishProductionService.TryProduce` / `TryReleaseDeath` is the
  production-to-death path for the Skirmish mission 4 ground set (ledger +
  overlay + cap).

### Ground capacity reservation ticket (SK-03)
- `SkirmishEconomyStockComponent` seeds Materials/Oil/Fuel from the compiled
  snapshot (Standard Established: 900/240/700).
- `SkirmishCapacityLedger` + reservation buffer: Reserved → Live → Released.
  Instant Skirmish mission 4 produce visits Reserved then Live in the same call.
  Starting grants count as Live. Death releases once.
- Shared eligibility now also checks Materials/Fuel/caps when
  `EnforceStocks=true`. Recruitment `FuelCost` stays 0 on the Skirmish
  mission 4 ground overlays (fuel is an operation cost, not a buy currency).
- Research scaffolding: designated HQ researches readiness and infantry
  weapons; Ground Staging researches vehicle protection; aircraft
  efficiency is present but needs a living Helipad/Airport (`MissingProducer`
  on this Ground Maneuver slice). Category upgrades are one level, 200 M /
  45 s, +10% infantry damage, +10% vehicle max health (percentage preserved),
  −10% air fuel. Replacement Barracks can research without becoming the
  victory base. Completed grants persist after the building is lost.
- Mid-produce cancel: Reserved refunds 100% Materials and releases the
  cap; Producing refunds 75% and unlocks the producer queue; Live/launched
  deliveries are `NotCancellable`. A failed dispatch before spawn refunds
  100%. Destroying the only living producer loses in-progress research and
  Producing reservations (no refund) and refunds still-Reserved queues.
- Fabrication / refinery profiles and physical Oil/Fuel/Materials haul stay
  open. They would need separate facility recipes and truck delivery and
  would block this research/cancel slice.

### Army control, fog, and movement ticket (SK-04)
- Starting Skirmish mission 4 Standard Regular forces form persistent tactical
  groups: four-person infantry squads and singleton ground vehicles. Group IDs
  stay stable through losses. A produced tank or rifle squad opens a new group.
- Legal player selection is one group or an explicit multi-group union. Enemy
  and empty groups are rejected. Shared `SelectedUnitTag` is the selection
  truth so existing command owners can see the same set. Clearing a selection
  writes group records first, then removes tags, so the army-group buffer is
  never held across that structural change.
- Paging is independent of group IDs (Standard page size 4). Page 0 holds the
  first four player groups; page 1 slot 3 is the starting tank for this roster.
- Shared fog seeds from the intel config (`SharedFog=true`,
  `DevelopmentFullVision=true` for this ground slice). Precision Attack is
  legal only against **Visible** contacts. Turning off development full vision
  ages a previously seen enemy to LastSeen and rejects Attack
  (`HiddenContact`). Reveal restores Visible.
- Move / Hold / Attack stamp the selected groups. Ground Move excludes air
  members instead of silently forcing them onto a ground destination.
- Live Standard ground movement now drives `LocalTransform` (and the bound
  GameObject when a visual exists). Infantry advance at 4 m/s, tanks / APC /
  other ground at 8 m/s or the shared `UnitMove.Speed` when that component is
  present. Hold clears the intent. Pause freezes the stepper.
- Shared path is reused, not rewritten: if `UnitMove` and a `GridConfig` exist,
  Move/Attack write `UnitPathRequest`. When `UnitPathFollow` or
  `UnitPathRequest` already owns the unit, the local stepper only syncs the
  visual. Editor match worlds without a grid still walk in world space.
- A cheap Army drawer projection writes the current player page into
  `SkirmishArmyDrawerSlot` (group, role, alive count, selected, last order).
  It is not a full drawer HUD.

### Base Assault objective depth (SK-06)
- `SkirmishBaseAssaultFacts` + `SkirmishObjectiveFactProjectionSystem`.
  Terminal evaluation uses **original designated** `base.player` / `base.enemy`
  identities only. Replacement Barracks do not count as extra lives.
- Field-army wipe is non-terminal. Pause freezes the deadline clock.
  Surrender is accepted only while Playing. Same-tick both-designated-dead is
  Draw / BothBasesDestroyed.
- `SkirmishOutcomeSystem` remains the only terminal writer.
- Hidden-health HUD: **Visible** shows live current health; **LastSeen** keeps
  the last observed value and ages it (pause freezes the age); **Unknown**
  displays Hidden 0. A known Barracks marker is not a live hidden-health feed.
  The shell reads this projection on expanded sessions only.
- Pause / result / replay on the live expanded session now go through the
  SK-10 codec path. Presentation no longer writes a legacy
  `SkirmishReturnRequest` or Quick Game save for expanded Replay/Restart.
  Custom and legacy receipts still cannot satisfy expanded completion.

### Launch path
- `SkirmishExpandedLaunchResolver.TryCompileAndQueue` compiles Skirmish
  mission 4 (size/difficulty/seed) and queues the immutable snapshot. No new
  `MatchSceneView` catalog-identity switch. Map hint remains Desert Base
  (index 0) for scene load only.

### Enemy strategy and public ARIA skills (SK-05)
- `SkirmishEnemyStrategySystem` scores Base Assault with shared
  `SkirmishStrategyScoring` (objective 40 / prevent-loss 100 / counter 20,
  hysteresis 15, ~25% Supply reserve). It issues only
  `SkirmishProductionService.TryProduce` (faction 2) and
  `SkirmishArmyCommandService.TryIssueGroupOrder` — no private producer or
  designated-base mutation. Enemy Materials/capacity live on
  `SkirmishEnemyStockComponent` / `SkirmishEnemyCapacityComponent`
  (`NextReservationId` starts at 1000) so player stocks stay untouched.
- Perception is fog-honest: visible player combat only
  (`SkirmishFogService.IsVisible`). Hostile Materials stay unknown unless
  `KnowsHostileMaterials` is set. Full-vision Skirmish mission 4 therefore
  sees the starting tank and recruits a rocketeer (120 Materials) when
  affordable, then attacks the designated player Barracks when that contact
  is Visible.
- Live `OnUpdate` resolves Ground Maneuver through a cached army profile
  (no per-tick `CreateInMemory`). Tests pass the authored
  `ArmyGround` ScriptableObject.
- `AriaSkirmishPlanSystem.Step` routes expanded sessions through
  `StepExpandedBaseAssault`. It only targets presented `AriaTouchTarget`
  controls (Recruit / SelectSquad / Attack / Hold / Inspect / Handback).
  Three failed attempts at the same action choose an alternative or
  Handback. The ARIA assembly still has no gameplay mutation API.
- Shadow tip `bb30f8ce3` failed
  `ExpandedAriaPlanTargetsPublicControlsWithoutGameplayMutation`:
  default `AriaPlayPhase.Manual` returned before the planner copied
  Recruit `Id=41` into `AriaPlayObservationComponent.TargetId` (Intent
  stayed the default Recruit = 0). Expanded planning now publishes the
  presented control while idle/Manual; Blocked, Starting, and in-flight
  Touching still skip. Prototype consent (`Manual` until Observing) is
  unchanged.
- Shared eligibility (`SkirmishProductionEligibility` with
  `EnforceStocks=true`) is the affordability boundary for UI, enemy, and
  ARIA. `SkirmishAriaPublicProjection` reads the player wallet only.
- Not yet: Frontline Control / Breakthrough / Convoy Escort policies,
  four difficulty behaviour profiles, structures/research/transport/scout
  camera skills, EN/FA teaching copy, or a counted normal-speed ARIA win.

### Checkpoint, result and replay scaffolding (SK-10)
- Expanded Base Assault session facts use Skirmish-owned DTOs
  (`SkirmishCheckpointDocument`, result receipt, replay request). Stable object
  IDs and setup hashes are stored; ECS handles are not.
- `SkirmishCheckpointCodec` JSON-encodes a checksummed wire document and writes
  temporary-then-replace files. Incompatible schema or content versions are
  rejected (`IncompatibleSave`).
- `SkirmishCheckpointService` captures/applies pause, stocks, designated Base
  Assault flags, living actors and reservations, then checks conservation
  before the session is treated as restored.
- `SkirmishCheckpointSystem` / `SkirmishResultSettlementSystem` /
  `SkirmishExpandedSessionControlSystem` are the live hooks. Pause captures a
  checksummed document, settlement is once per
  session/definition/version/difficulty/size, and Replay destroys attempt-owned
  actors then queues a **new** session id with fresh Regular Standard stocks
  (900 Materials). Custom and legacy receipts cannot satisfy expanded
  completion.
- This is live Base Assault pause/result/replay wiring, not device, OS
  interruption, War/Large War, or full recovery certification.

### Measured Desert Base layout (SK-11)
- `SkirmishMapLayoutBuilder` / `SkirmishMapLayoutValidation` author the
  Desert Base Base Assault envelope the compiler already consumes:
  600 × 420 m, origin (−300, −210), cell size 2 m, west→east. Normalised
  MAP_IMPLEMENTATION candidates are projected to stored world transforms.
  Legal pads (Barracks, Ground Staging, infantry/vehicle spawn, rally,
  service, air return, supply, expansions) sit on those anchors and do not
  share a cell. Three independent routes keep distinct interiors:
  highway (`route.skirmish.db.main`), north ruins (`flank_a`), south sweep
  (`flank_b`).
- Regular Standard Skirmish mission 4 binds spawn and Ground Staging to
  those pads. Infantry open on the infantry pad, tanks / APC / car on the
  vehicle pad, designated Barracks on the base pad, Ground Staging on the
  staging pad with vehicle spawn / rally sockets. War / Large War and
  Custom / legacy snapshots do not take this bind.
- Transit times are **provisional** (authoring speeds: infantry 4 m/s,
  ground 8 m/s, air 16 m/s) until a device walk of the shared Desert Base
  scene. First-contact assumes both sides advance and meet at mid-route.

| Route | Length | Infantry first contact | Infantry one-way | Ground first contact | Width |
|---|---|---|---|---|---|
| Highway (main) | 384 m | 48 s (target 45–75) | 96 s | 24 s | 12 m |
| North ruins (`flank_a`) | 666 m | 83 s (target 75–105) | 167 s | 42 s | 6 m (infantry / APC) |
| South sweep (`flank_b`) | 608 m | 76 s (target 75–105) | 152 s | 38 s | 12 m |

- Not yet: baked navigation probes, wreck blockage, War/Large War staging
  growth, or the other four map packets. No scene geometry was edited.

### Library, copy and publication ticket (SK-12)
- Quick Custom library cards and briefing resolve the existing
  `skirmish.s002.*` keys for Regular Standard without making the row Ready.
  Status on the card is **In Progress**. Launch stays disabled. War / Custom
  do not take the first-visit briefing.
- Expanded HUD objective and result lines use the same keys
  (`objective`, `result.victory` / `defeat` / `draw_bases` / `draw_deadline` /
  `surrender`). `MatchSceneView` was not rewritten.
- `SkirmishPublicationValidator` requires matching definition/content/setup
  hashes and Regular Standard. An asset’s existence cannot set Playable.
  Manual, ARIA, edge, recovery and device evidence are all required. The
  authored row stays **InProgress**.
- The three legacy prototypes remain Playable only through compatibility
  version 1. A different version does not stamp those overrides.
- `V3UiLocalizationCatalog` was not edited in this slice.

### Acceptance evidence scaffolding (SK-13)
- `acceptance.md` lists the first-visit matrix: one manual Regular Standard
  victory (`104731` EN), the eighteen Regular Standard ARIA sample slots
  (seeds `104731` / `130365` / `155923` × EN/FA × three attempts), and
  edge / recovery / device placeholders. It also explains how to fill
  `runs.csv`. Recruit / Veteran / Commander and War / Large War stay later.
- `runs.csv` is header-only with the mandated fields. No Victory rows are
  pre-written. Pure Editor reducers do not replace a normal-speed match.
- `SkirmishAcceptanceCensusCapture` compiles Regular Standard and records
  the starting-force census plus stable `code_hash` / `config_hash`. The
  Editor probe `Tools/Warline/Skirmish/Capture S002 Regular Standard Census`
  logs those hashes and does **not** mark Playable.
- `AriaPlayEditorValidation`, `SkirmishExpandedAriaWatchValidation`, and the
  existing Industrial Basin watch probe now accept the immutable
  definition / size / difficulty / seed / locale payload and log the
  selected configuration. They do not inject an army or award Victory.
- `Tools/Warline/Skirmish/Launch S002 Regular Standard Game View` queues
  Regular Standard seed `104731`, dumps Game View PNGs under repo-root
  `_Evidence/` (and a copy under the S002 report `_Evidence/`), and stays
  in Play Mode. `Flip S002 Playable If Evidence Ready` writes Playable
  only when hashes match and those evidence files exist. Focused tests
  call the dry-run path only.
- Tip `45109b2bf` Game View failed with `startupFailure=Timeout` because
  `SkirmishSessionInitializationSystem` added session components while
  still iterating the live query. Init (and spawn, which had the same
  pattern) now snapshot entities first, then apply structural changes.
  Publication is still **InProgress** until Programmer 1 Game View
  evidence. The Gridlock main merge does not flip that row.

### Publication
- Status: **InProgress**. Closer to a playable ground slice (compiler +
  overlays + designated Base Assault facts + capacity + legal army groups +
  live Standard ground movement + legal enemy/ARIA BA skills + registry
  GameObject visuals / Ground Staging yard + hidden-health HUD + live
  pause/result/replay through the SK-10 codec + HQ/Ground Staging research
  scaffolding and 75% mid-produce refunds + measured Desert Base pads and
  routes + library/HUD copy wiring + acceptance scaffold / hash census)
  but **not Playable**, not ARIA/War certified, not Accepted. Programmer 1
  validated tip `f73637f59` (eight suites). This slice adds a ninth
  acceptance-scaffold marker. Game PM authorised Programmer 1 to flip
  Playable after Game View evidence via the guarded menu; focused tests
  do not persist that write.

## Remaining ticket gaps

| Ticket | Gap |
|---|---|
| Ground roster and production visual ticket (SK-02) | Air roles; combat certification; live match still uses stand-ins when the scene registry is not supplied to launch |
| Ground capacity reservation ticket (SK-03) | Fabrication/refinery profiles and physical haul; enemy-side research receipts; checkpoint serialisation of research timers |
| Army control, fog, and movement ticket (SK-04) | Transport boarding, formation/columns, full Army drawer HUD, terrain-blocked sight; path reuse still needs a live `GridConfig` + `UnitMove` |
| Enemy strategy ticket (SK-05) | Frontline Control / Breakthrough / Convoy Escort policies; four difficulty profiles; structures/research/transport/camera skills; EN/FA teaching; counted full-speed ARIA wins |
| Base Assault objective depth (SK-06) | World-damage fixtures; EN/FA last-seen copy; device-facing pause chrome |
| Checkpoint ticket (SK-10) | Device/OS interruption evidence, airborne passengers, in-flight queues, War/Large War recovery |
| Measured layout ticket (SK-11) | Device-measured navigation on the shared Desert Base scene; War/Large War staging reservations; City Crossroads / Mountain Pass / Industrial Basin / Airfield Plains packets |
| Publication and localization ticket (SK-12) | RTL/long-copy/device HUD review; expose certified extra sizes only after SK-13 evidence |
| Acceptance evidence ticket (SK-13) | Counted normal-speed manual and ARIA wins, filled `runs.csv` rows, device/recovery evidence. Scaffold, hashes, Game View launch, and a guarded flip menu are in place |

## How to validate in Editor

Use Programmer 1’s **shadow project only**: `D:\Projects\WarlineCapture-Skirmish`
(git worktree, own `Library`). Check out `cursor/skirmish-shadow` or this
short-lived `cursor/skirmish-s002-…` branch there. Do **not** open or lock the
shared `D:\Projects\WarlineCapture` checkout.

1. Keep Unity Hub open and signed in.
2. Compile/check the new assemblies. `Game.Skirmish.Contracts` stays its own
   assembly. Consumers that `using Game.Skirmish.Contracts` reference it
   explicitly: `Game.Components`, `Game.Configs`, `Game.Runtime`,
   `Game.Composition`, `Game.Editor`, and `Game.Tests.Editor`.
3. From the shadow worktree, run the Windows wrapper (preferred on Programmer 1):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-definitions.log" `
  -RequiredPassMarker "[SkirmishExpandedDefinitionTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedObjectiveTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-objectives.log" `
  -RequiredPassMarker "[SkirmishExpandedObjectiveTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedEconomyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-economy.log" `
  -RequiredPassMarker "[SkirmishExpandedEconomyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedArmyTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-army.log" `
  -RequiredPassMarker "[SkirmishExpandedArmyTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-aria.log" `
  -RequiredPassMarker "[SkirmishExpandedAriaTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedVisualTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-visual.log" `
  -RequiredPassMarker "[SkirmishExpandedVisualTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedCheckpointTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-checkpoint.log" `
  -RequiredPassMarker "[SkirmishExpandedCheckpointTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedCatalogTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-catalog.log" `
  -RequiredPassMarker "[SkirmishExpandedCatalogTests] result=Passed"
```

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishExpandedAcceptanceTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-acceptance.log" `
  -RequiredPassMarker "[SkirmishExpandedAcceptanceTests] result=Passed"
```

Required markers (same eight suites plus acceptance scaffold/hash plumbing):

- `[SkirmishExpandedDefinitionTests] result=Passed`
  (adds `CompilerAcceptsTypedDesertBaseLayout`,
  `RegularStandardBindsMeasuredPadsAndRoutes`,
  `CustomAndLegacyDoNotBindMeasuredLayout`,
  `SessionInitializationProjectsRolesOutsideLiveQuery`)
- `[SkirmishExpandedObjectiveTests] result=Passed`
  (adds `HiddenHealthProjectsVisibleLastSeenAndUnknown`)
- `[SkirmishExpandedEconomyTests] result=Passed`
  (adds `RecruitmentFuelCostStaysZeroOnS002Overlays`,
  `MidProduceCancelRefundsReservedThenSeventyFivePercent`,
  `FailedDispatchAndProducerDestructionUseSpecifiedRefunds`,
  `CategoryResearchFromStagingAndHqAppliesOnce`,
  `ReplacementHqCanResearchWithoutBecomingVictoryBase`)
- `[SkirmishExpandedArmyTests] result=Passed`
  (adds `StandardGroundUnitsAdvanceWorldTransformOnMove` and
  `ArmyDrawerProjectsCurrentPlayerPage`)
- `[SkirmishExpandedAriaTests] result=Passed`
- `[SkirmishExpandedVisualTests] result=Passed`
- `[SkirmishExpandedCheckpointTests] result=Passed`
  (adds `LivePauseSettlesOnceAndReplayReseedsFreshSession` and
  `CustomAndLegacySessionsAreNotExpandedCompletion`)
- `[SkirmishExpandedCatalogTests] result=Passed`
  (`RegularStandardResolvesS002BriefingAndHudKeys`,
  `WarAndCustomDoNotResolveFirstVisitBriefing`,
  `AssetExistenceDoesNotSetPlayable`,
  `HashMismatchAndWrongMatrixRejectPlayable`,
  `LegacyPrototypeCompatibilityIsVersionedAndDoesNotPublishS002`,
  `CompleteEvidenceWouldAllowPlayableButAuthoredRowStaysInProgress`)
- `[SkirmishExpandedAcceptanceTests] result=Passed`
  (`CheckedInRunsCsvUsesMandatedHeader`,
  `AcceptanceMarkdownListsRequiredMatrix`,
  `RegularStandardCensusCapturesStableHashesWithoutPlayable`,
  `FirstVisitPayloadRejectsWarAndUnknownLocale`,
  `AriaPlayAndWatchFacilitiesLogSelectedConfiguration`,
  `PendingRunRowKeepsEmptyResultAndCensusProbeDoesNotPublish`,
  `PlayableFlipRequiresHashesAndEvidencePaths`)

Optional compiler / Ground Staging rebuild:

```
Tools/Warline/Skirmish/Rebuild Expanded Definitions
Tools/Warline/Skirmish/Rebuild Ground Staging
Tools/Warline/Skirmish/Rebuild Desert Base Layout
Tools/Warline/Skirmish/Validate S002 Publication
Tools/Warline/Skirmish/Capture S002 Regular Standard Census
Tools/Warline/Skirmish/Log S002 Regular Standard ARIA Watch Payload
Tools/Warline/Skirmish/Launch S002 Regular Standard Game View
Tools/Warline/Skirmish/Flip S002 Playable If Evidence Ready
```

A live Editor match on Desert Base Regular Standard should show starting
tank / APC / infantry GameObjects and a Ground Staging yard (registry prefabs
when the unit registry is supplied to expanded launch or already loaded;
otherwise named presentation stand-ins, including the authored Ground Staging
prefab when that asset imports). Entities must not stay invisible. Regular
Standard spawn and Ground Staging sit on the measured legal pads
(player staging −192, 0; Barracks −228, 0), not the old 40 m placeholder
extent.

4. Legacy smoke: Quick Custom still offers Skirmish 1 Desert Base prototype
   (index 0), Skirmish 2 City Crossroads (index 1), and Farhad’s Skirmish 3
   Industrial Basin (index 3). Editor stress remains index 2.
   Do not expect Skirmish mission 4 on the old playable catalog override.
5. Expanded seed path: Regular Standard `104731` (also `130365`, `155923`).

## Localization follow-up

Shared `V3UiLocalizationCatalog` was not edited in this slice. Game Design
merged `skirmish.s002.*` EN/FA rows (plus `SkirmishS002UiStrings.json`) at
`87fffc598`. Required keys:

| Key | EN | FA |
|---|---|---|
| `skirmish.s002.title` | Desert Base · Base Assault · Established | پایگاه صحرا · حمله به پایگاه · پایگاه برقرار |
| `skirmish.s002.brief` | Use the starting tank and APC to contest the highway. Keep one infantry squad at home. Press now or scout the south sweep before siege. | با تانک و نفربر شروع‌شده بزرگراه را بگیر. یک دسته پیاده در خانه بماند. یا سریع فشار بده یا جنوب را شناسایی کن. |
| `skirmish.s002.objective` | Destroy the original enemy Barracks while your original Barracks survives. | سربازخانه اصلی دشمن را نابود کن، در حالی که سربازخانه اصلی خودت سالم است. |
| `skirmish.s002.warning.offensive_air` | Ground Maneuver cannot queue attack helicopters or jets. | پروفایل زمینی حمله هوایی تهاجمی را صف نمی‌کند. |
| `skirmish.s002.warning.replacement_base` | A replacement Barracks is not the designated victory base. | سربازخانه جایگزین پایگاه پیروزی تعیین‌شده نیست. |
| `skirmish.s002.result.victory` | Enemy main base destroyed. | پایگاه اصلی دشمن نابود شد. |
| `skirmish.s002.result.defeat` | Your main base was destroyed. | پایگاه اصلی تو نابود شد. |
| `skirmish.s002.result.draw_bases` | Both original bases were destroyed. | هر دو پایگاه اصلی نابود شدند. |
| `skirmish.s002.result.draw_deadline` | Time expired with both original bases standing. | زمان تمام شد و هر دو پایگاه اصلی سالم ماندند. |
| `skirmish.s002.result.surrender` | Surrender accepted. | تسلیم پذیرفته شد. |

Owner: publication and localization ticket (SK-12) / localization catalog
builder. The keys now exist in both EN and FA tables via the merged Game
Design commit; that does **not** mark Skirmish mission 4 Playable.

## Landing status (Programmer 1)

- Publication: **Playable** on tip `2b5f0f6e9` (flip commit `7f0e1def7`).
- Game View self-gate: Regular Standard seed `104731` Playing PNG + sidecar under `Design/AgentReports/SkirmishExpansion/S002/_Evidence/`.
- Visual spawn follow-up: `SkirmishVisualSpawnSystem` snapshots session entities before `AttachMissing` (`2b5f0f6e9`) so live Playing no longer throws structural-change during `AddComponentObject`.
- Still open (do not treat as ARIA/War certified): counted Regular Standard ARIA matrix (seeds 104731 / 130365 / 155923 x en/fa-IR), the human slot `S002-manual-rs-104731-en`, edge/recovery placeholders, and device review. `runs.csv` is still header-only. Publication Playable is not an ARIA win.

## S002 ARIA win harness

Expanded attack orders now apply overlay damage. A unit on Attack stops while a
legal visible combatant is in range, then continues to the ordered structure.
Rifles still cannot damage a Barracks. Hidden contacts take no damage. ARIA
pages to the assault cards (tank, car, APC, rocketeer), additive-selects them,
and presses the visible Attack button. That button calls
`SkirmishArmyCommandService.TryAttack` on the visible enemy base. No forced
Victory, injected army, extra resources, or hidden wallet.

Live recorder: `Game.Editor.SkirmishS002AriaRunHarness`.

### Tip `Abort,timeout` / elapsed stuck at 0 (Windows live)

Observed on tip `9d20bb4b`: Playing for the full ~1500s wall budget with
`match.ElapsedSeconds` frozen at 0. Expanded spawn marks Playing before
`SimulationActive` is armed; LoadingGate fail-open previously cleared the
loading pending flag **without** setting `SimulationActive=1`, so the objective
clock stayed paused and the harness waited until wall-clock timeout.

Fixes on this branch:

1. LoadingGate fail-open now sets `SimulationActive=1`.
2. Expanded session control projects objective-clock elapsed onto
   `SkirmishMatchState.ElapsedSeconds` (RulesSystem is skipped for expanded).
3. Harness waits for `SimulationActive` before starting ARIA, arms simulation
   after gameplay bootstrap completes/fails, and aborts with
   `end_reason=simulationNotAdvancing` if elapsed stays 0 for 45s after Playing
   (instead of burning 1500s).
4. Validation profile root uses `Path.GetTempPath()` (Windows-safe).

### Outcome stayed None while elapsed rose (Launch104731En)

The ~1470–1500s stop is `SkirmishS002AriaRunHarness.WallClockBudgetSeconds`.
The harness calls `Finish(abort: true)` when `SkirmishMatchState.Phase` is
still not `Finished`. Live `en-3` jsonl never left `phase=Playing` /
`outcome=None`. That is not a `SkirmishPhase.Finished` Draw.

`SkirmishRulesSystem` returns immediately for a non-legacy expanded session, so
the legacy 900s `MatchDurationSeconds` writer never runs. The expanded
deadline (Standard 1080s) is evaluated by `SkirmishBaseAssaultObjectiveSystem`.
`SkirmishOutcomeSystem` is what copies that terminal fact onto
`SkirmishMatchState`, which is the only phase the harness watches.

That copy called `EntityManager.AddComponentData<SkirmishResultComponent>`
inside the outcome query, before `session.Phase = Finished`. A structural
change during iteration throws and does not apply, so the result component
stayed missing and the match phase stayed `Playing` on every later frame.
The objective clock kept advancing in an earlier system. `IsDesignatedAlive`
returning true when no role/health pair exists does not block the deadline;
it only keeps a missing barracks from counting as destroyed. A barracks at
0 HP is dead. An unspawned base stays alive so the match does not Draw before
roster projection.

The outcome writer now sets the session phase during iteration and adds the
result component only after the query ends, then projects `match.Phase` /
`match.Outcome`. Fact projection likewise adds `SkirmishBaseAssaultFactComponent`
outside its clock query, heals a 0 deadline from the resolved setup, and
publishes the terminal objective the evaluator already computed. A passed
deadline is Draw / TimeLimit, not a counted win.

Causes, checked in the expanded Base Assault path:

1. The assault planner treated one NEXT tap as done. The page did not change,
   the hand then waited with no target, and the fallback Attack used the rifle
   cards. Rifles cannot damage a Barracks.
2. Mission HUD masks left the expanded NEXT card and the vehicle page
   non-interactable, so `AriaTouchTarget.Available` stayed false.
3. The touch watchdog blocked that constant GoalId after 180s. Blocked made
   `AriaPlayModel.Active` false, the watch HUD published an empty observation,
   and the planner never restarted. A stale frame can also drop the driver to
   Manual. The harness started ARIA once.
4. A tank that reached the enemy pad set `Engaged` for every in-range
   combatant and stopped short of Barracks range. A pending `UnitPathRequest`
   without `UnitPathFollow` also froze the local step on the live grid.
5. Enemy `Evaluate` runs every tick and `TryProduce` is instant, so one visible
   tank printed rocketeers until the stock was gone. The assault order is one
   counter recruit, then the tank. Rifles are not ordered onto the Barracks:
   they cannot hurt it, and that wave killed the column before either base fell.
6. Re-issuing the same Attack every tick cleared shot cooldown, so the ordered
   group fired every frame.
7. Fact projection advanced the clock, but a 0 deadline or a terminal fact that
   never reached `SkirmishMatchState` left `outcome=None` past 1080s. The clock
   system now heals the deadline from the resolved setup and publishes the
   terminal fact the evaluator already computed. Draw is TimeLimit, not a
   counted win.

Counted `AriaWon` still requires a finished Victory from
`SkirmishS002AriaRunHarness` at `normal_speed=1` with `input_violations=0` and
`human_interventions=0`. These edits do not stamp Victory, inject units, skip
the match, or add resources. `runs.csv` stays header-only in git.

### Re-run Launch104731En on Windows Skirmish shadow

Keep Hub signed in. Open `D:\Projects\WarlineCapture-Skirmish` on this branch
tip, focus the Game View, then either:

- Menu: `Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 en`
- Or executeMethod (no `-quit` wait for the match — leave the Editor running):

```powershell
# Prefer the live menu while the shadow Editor is already open.
# Focused harness proof (does not play the match):
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS002AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS002AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

Live watch: run `Launch104731En` or `Launch130365En` from the open Editor
without `-quit`. Expect rising `elapsed` / `clockElapsed` and
`simulationActive=1`. The match should reach a real terminal Victory, Defeat,
or Draw before the 1500s wall Abort. A counted win is Victory with
`normal_speed=1`, `input_violations=0`, and `human_interventions=0`. A
stuck-zero clock must Abort with `simulationNotAdvancing`. Do not stamp
Victory. Also run `Game.Tests.Editor.SkirmishExpandedAriaTests.RunFocusedValidation`
with the same wrapper and marker style (`[SkirmishExpandedAriaTests] result=Passed`).

Menus:

- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 en`
- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 104731 fa-IR`
- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 en`
- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 130365 fa-IR`
- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 en`
- `Tools/Warline/Skirmish/Launch S002 ARIA Watch 155923 fa-IR`

Execute methods (same class): `Launch104731En`, `Launch104731Fa`,
`Launch130365En`, `Launch130365Fa`, `Launch155923En`, `Launch155923Fa`,
`LaunchFromEnvironment` (`WARLINE_S002_SEED`, `WARLINE_S002_LOCALE`).

Run them from the open Windows Skirmish shadow Editor
(`D:\Projects\WarlineCapture-Skirmish`) with Hub signed in and the Game View
focused. `normal_speed` stays 1. The
`InvokeUnityExecuteMethodValidation.ps1` wrapper passes `-quit` and will not
wait for the match. On finish or a 1500s abort the harness appends the next
open `S002-aria-rs-*` row and writes evidence under
`Design/AgentReports/SkirmishExpansion/S002/_Evidence/`. Census
`code_hash` / `config_hash` / definition version on that row come from
`SkirmishAcceptanceCensusCapture.TryCapture` + `FormatLog` for the launched
seed (same hashes as `Tools/Warline/Skirmish/Capture S002 Regular Standard Census`).
`AriaPlayEditorValidation.AcceptExpandedPayload` and the watch
`AcceptExpandedPayload` still only log the selected configuration.

`S002-manual-rs-104731-en` stays empty for a human on
`Tools/Warline/Skirmish/Launch S002 Regular Standard Game View`.

Focused Editor proof (refuses forced Victory; accepts only a finished match
outcome; does not write the repo `runs.csv`):

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File Tools/CI/InvokeUnityExecuteMethodValidation.ps1 `
  -UnityExe "<resolved Editor from ProjectSettings/ProjectVersion.txt>" `
  -ProjectPath "D:\Projects\WarlineCapture-Skirmish" `
  -ExecuteMethod Game.Tests.Editor.SkirmishS002AriaHarnessTests.RunFocusedValidation `
  -LogFile "$env:TEMP\skirmish-s002-aria-harness.log" `
  -RequiredPassMarker "[SkirmishS002AriaHarnessTests] result=Passed" `
  -GuiLicensing
```

This environment has no Unity Editor. Programmer 1 runs that marker on the
shadow project. Do not mark the ARIA matrix complete from this branch.
- Focused Editor suites green on Windows Skirmish shadow after Gridlock merge; visual + catalog re-checked on `2b5f0f6e9`.

