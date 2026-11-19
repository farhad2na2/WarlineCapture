# Operations readiness survey (2026-11-19)

Read-only survey. No Unity run, no asset or code change; this file is the only output. Branch
`missions/integration` at HEAD `4425058fe`. Paths are repository-relative; `file:line` refers to HEAD.

## 1. What an Operation is

- **A different mode with its own meta-loop, not a Skirmish variant.** It is a persistent city
  "stabilization run" (`Design/Roadmap/Operations/PLAN.md`, "The player experience"). The player
  inspects six districts on a dashboard (SCN-11) and picks a mission offer, which costs AP. The
  player deploys into one 3D tactical mission, returns with results, and the district state
  changes. Days advance only when the player ends the day. A full run is meant to take 20–35
  in-game days and 30–45 deployments.
- **Each of the 60 missions is a single tactical match.** It is not multi-stage across matches.
  Inside the match it plays an ordered objective graph, for example O001
  `SCAN(a,b,c) -> INTERACT(evidence,15) -> EXTRACT(squad,exit.ground)`. Results are Victory,
  Partial (with a Conclude action), Defeat and Withdraw. Finite enemy waves A and B are triggered
  by objective progress.
- **Structure: 6 districts × 10 missions (D01–D06 = O001–O060).** Slot 10 of each district is a
  FINALE.
- **12 families.** Ledger counts: RECON 6, ESCORT 7, REPAIR 6, FINALE 6, BREACH 5, DEFENSE 5,
  RESCUE 5, SEIZE 5, INTERDICT 4, PATROL 4, RAID 4, AIRLIFT 3. They are composed from 13 rule
  verbs: SCAN, VISIT, HOLD, CLEAR, INTERACT, REPAIR, ESCORT, RESCUE, AIRLIFT, STOP, BREACH,
  EXTRACT and PROTECT (`Design/Roadmap/Operations/MISSION_IMPLEMENTATION.md`, "Rules and classes
  to implement once").
- **Sizes.** Each mission uses a fixed scenario task force, with no base building or production.
  - Player packages: FP_LIGHT 16, FP_SERVICE 22, FP_GROUND 36, FP_AIR 22, FP_COMBINED 40 units.
  - Enemy packages: EP_CELL 20, EP_RAIDERS 30, EP_MECHANIZED 36, EP_AIRFIELD 34, EP_FINALE 44.
  - Repair is the only Materials spend.
  - Deadlines are 720–1200 s; the pacing target is 8–15 minutes, and 15–20 for finales.
  - There is no army-profile × start matrix, so the ledger's `army` and `start` columns stay empty
    for O-rows.
- **Maps.** Each district is a logical layout on an existing Campaign source
  (`Design/MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md`, "Mode source selection"):

  | District | Name | Source |
  |---|---|---|
  | D01 | Old Quarter | Window on the shared desert world. `Assets/Game/Resources/Operations/OldQuarter.asset`: source `opmap.skirmish.desert_base_01`, playable 1672–1912 × 680–856 |
  | D02 | Civic Center | Citywide Alert or Command Node |
  | D03 | Industrial Belt | RefineryDistrict or Armor Break |
  | D04 | River Crossing | AshLinePort or Trust Under Fire, with two existing land crossings |
  | D05 | Highland Approach | Armor Break |
  | D06 | Airport Perimeter | CityEdgeAirfield or Grounded Signal |

  An older amendment says D05 needs new highland terrain
  (`Design/Roadmap/Operations/Missions/README.md`, "Future-map planning amendment" of 09-29).
  The policy of 10-03 supersedes it.
- **How it differs from Skirmish.**
  - No base building, economy or production.
  - Neutral civilians, cargo, evidence and repair sites are part of the mission.
  - Partial outcomes exist, and results carry consequences into the city run.
  - Each mission has authored narrative.
  - The acceptance contract is much heavier (see risk R5).

## 2. What already exists

### Catalog and definitions

- **Design catalog: `Design/Roadmap/Operations/MISSION_CATALOG.csv`, 60 rows.** It is
  identical to the 60 O-rows of `missions.csv` on family, packages and deadline (0 mismatches
  checked).
  - O001–O003 are `Authored`, O004–O060 are `Planned`.
  - The catalog marks O002 and O003 `AriaWon`. That claim comes from the host model only.
- **Briefs for all 60 missions:** `Design/Roadmap/Operations/Missions/D01…D06_*.md`, each about
  330 lines, with graph, partial/failure rules, mastery, consequences and gates.
- **The C# catalog** is in `Assets/Game/Scripts/Operations/Contracts/OperationsCatalogIndex.cs`,
  60 entries.

### Pure-C# model, assemblies under `Assets/Game/Scripts/Operations/` (no engine references)

- **P0 Contracts:** IDs, schemas, save DTOs.
- **P1 Strategic:** city, days, AP, offers, consequences.
- **P2 Tactical:** an abstract greybox session with Scan, Hold, Interact, Repair, Escort and
  Extract, a graph compiler and finite waves.
- **P3 Loop:** in-memory launch, settlement and checkpoint.
- **P4 Content:** O001–O003 graphs (`Loop/OperationsAuthoredMissions.cs`), EN/FA copy, ARIA
  planner and input-skill model.
- **Capture:** a Play-Mode renderer that is not the shipping UI.

### Native shipping runtime: O001 (RECON) only

- **ECS systems:** `Assets/Game/Scripts/Runtime/Missions/OperationsRecon{Objective,Patrol,Startup,Wave,Checkpoint…}.cs`.
- **Config:** `Assets/Game/Scripts/Configs/OperationsReconMissionConfig.cs`, a single
  `ResourcePath = "Operations/StreetSignals"` (`:22`).
- **Builder:** `Assets/Game/Scripts/Editor/OperationsReconMissionAssetBuilder.cs`.
- **Composition:** `Composition/OperationsMissionPresentationSystem*.cs`,
  `MatchSceneView.Operations.cs`, `OperationsReconSpawnCompositionSystemHelper.cs`.
- **Persistence:** `Persistence/SaveService.Operations.cs`, `OperationsProfileCommandService.cs`.
- **Hard-coded to O001:**
  - `MatchSceneView.Operations.cs:9`: `IsOperationsSession => MissionId == "operation.o001"`.
  - `OperationsMissionPresentationSystem.cs:392,411`.
  - `UiShellEcsGateway.MissionHudRestrictions.cs:23`.

### Menu and UI

- `UIRoute.Operations` installs `OperationsMissionScreenView` (`UI/Shell/MenuOverlayRoutePresentation.cs:34,112-114`).
- Dashboard: `UI/Screens/OperationsDashboardScreenView.cs` and its prefab builders.
- The O001 HUD has a briefing, a camera tour, a checklist, ARIA Play/Stop, Scan/Recover controls
  and a pause flow with Save & Exit, Withdraw and Conclude. The visual direction was approved by
  the owner on 2026-09-24 (`Design/AgentReports/Operations/O001_PLAYER_EXPERIENCE_20260924/README.md`).

### ARIA

- The shared-world Watch path for Operations is
  `UI/Screens/MatchHudAssistantUiSystemHelper.Watch.cs:34-40,230-…`. It drives
  `OperationsMissionScreenView` buttons, so it is specific to O001.
- Harness: `Assets/Game/Scripts/Editor/OperationsReconLaunchSmokeValidation.cs`.
  - Entry points: `RunAriaVictory`, `RunAriaVictoryPersian`, `RunManualVictory`, `RunManualPartial`,
    `RunLifecycle`, `RunCheckpointResume*`, `RunWideLayout*`.
  - Seed and difficulty come from `WARLINE_VALIDATION_O001_SEED` and `…_DIFFICULTY` (`:169-175`).
  - Markers: `[OperationsReconLaunchSmokeValidation] result=…` and `[OperationsReconManualAcceptance]`.
- **Historical results (2026-09-24, on a tree three months older):**
  - ARIA won Regular seeds 1102, 2102, 4102 and 5102, and Farsi seed 9102. Seed 3102 ended in
    Defeat.
  - A manual run reached Partial; a full human Victory is still pending.
  - Save/resume across Editor processes passed.
  - Ledger O001 = `assets-unverified, unverified on current tree`.

### Authoring tools

- `Tools/Missions/quickcheck.py`, with `Tools/Missions/OperationDraft.cs` and `OperationsHost.cs`,
  already covers Operations offline:
  - catalog parity for all 60 entries;
  - host-model runs of O001–O003;
  - `--operation-draft` external graph drafts, compiled and played as public-command cases in the
    abstract model;
  - a Campaign source packet comparison.
- Example drafts: `Tools/Missions/examples/o004-quiet-archive.json` and `old-quarter-source.json`.
- No `Tools/Missions/drafts/o*.json` exist.
- `Tools/Operations/check_p0…p4.py`, `check_aria_evidence.py` and `check_mobile_ready.py` are
  host checks.
- The `Invoke-Operations*.ps1` wrappers target a Windows shadow worktree
  `D:\Projects\WarlineCapture-Operations`. That conflicts with today's single main-checkout lane
  (`CONTEXT.md`).

### Tests

- 65 files under `Assets/Tests/Editor/Operations/`.
- Others: `Assets/Tests/Editor/OperationsReconWorldTests.cs`, `OperationsReconCheckpointTests.cs`,
  `OperationsScenarioSourceBindingTests.cs`, `OperationsDashboardScreenTests.cs`,
  `Persistence/OperationsProfilePersistenceTests.cs`.

### Ledger

- `missions.csv` has 60 O-rows: O001 `assets-unverified`, O002–O060 `planned`.
- Their `blocker` column still says "waits until all 120 Skirmish rows are done". That is stale
  since `daily/2026-11-17-directive.md` §4.

## 3. Carry-over from Skirmish

### Carries over directly

- **Lane discipline:** `invoke_unity_macos.sh`, the startup check, `disk_guard.sh`, `state.json`
  and `STATUS.md`.
- **Certification and regression rules:** certify-by-family (directive §3) and the long-session
  runtime regression. The regression runs once per runtime candidate, and the runtime is shared.
- **Measured maps:**
  - The `LayoutDumps/S002/dump.json` whole-world bitmap covers the D01 Old Quarter window.
  - `S050/dump.json` covers Citywide Alert, a D02 candidate.
  - `Tools/Missions/pad_preflight.py` already knows both sources (`:32-36`).
- **Shared native mechanics to reuse for Operations rule verbs:**

  | Skirmish mechanic | Code | Operations use |
  |---|---|---|
  | Convoy truck runtime and command eligibility | `Runtime/Skirmish/SkirmishConvoy*.cs` | ESCORT and STOP cargo identities |
  | Frontline capture reducer | `SkirmishFrontlineReducer.cs` | HOLD, SEIZE, DEFENSE |
  | Breakthrough evacuation | `SkirmishBreakthroughReducer.cs` | EXTRACT |
  | Base Assault | `SkirmishBaseAssaultObjectiveSystem.cs` | BREACH, CLEAR |
  | Enemy objective commander | | enemy behaviour |
  | Native objective checkpoint | | checkpoint |
  | Base-alert legibility | | threat warnings |
  | ARIA visible-touch input layer | | ARIA input |

  Each needs an Operations adapter; none is drop-in.

### Missing for Operations

- **ARIA queue:** `run_aria_queue.py` accepts only `S###:seed:locale` jobs and the
  `SkirmishS002AriaRunHarness` entry. Operations has a separate one-mission entry point.
- **Drafts:** the Skirmish draft, check and dump tools (`check_draft.py`,
  `layout_dump_to_draft.py`) use the Skirmish format and `INITIAL_SETUP_MATRIX`. Operations uses
  FP/EP packages and graph drafts.
- **Evidence path:** `runs.csv` is written by the Skirmish harness only.
- **Gate 3:** `[SkirmishGateUnits]` and `gate3_objective.py` are Skirmish-only.
- **Layout dumps** are missing for D03–D06 sources: RefineryDistrict, AshLinePort, Trust Under
  Fire, Armor Break, CityEdgeAirfield and Grounded Signal. Skirmish MP actually runs on Citywide
  Alert, and IB on the shared world, so Armor Break and RefineryDistrict were never measured.

## 4. Concrete first steps, in order

The Skirmish trigger is not met yet. Only BA is accepted (S002 `auto-accepted`). FC, BT and CE
have wins but no accepted row: W31b deadline, gate-3 failures and CE below the 20% floor. Steps
A–E can run now; F onward follows the trigger or a ruling.

### Without Unity, start now

| Step | What | Who / effort |
|---|---|---|
| A | Ledger hygiene. Replace the stale O-row blocker text with "production waits for first accepted FC/BT/CE (directive §4)". Note that catalog `AriaWon` for O002/O003 is host-model only. Fix the stale Skirmish map labels (MP = Citywide Alert, IB = shared-world window). | Haiku, 20 min |
| B | Offline baseline with `quickcheck.py`: catalog parity for 60 IDs, host O001–O003, and the O004 example draft. Writes only to `Logs/MissionQuickcheck/runs/`. Authoring feedback only, not evidence. | Codex, 30 min |
| C | Runtime generalization plan as a brief, not a document. Map every O001 hard-code above to a mission-keyed registry: config per mission ID, a session predicate from the catalog, HUD restrictions from data. List which `OperationsRecon*` systems become generic SCAN, INTERACT, EXTRACT, PROTECT and waves. | Opus, 2–3 h read |
| D | D01 and D02 source packets offline. Bind O002–O010 roles from the D01 brief to legal cells in the S002 whole-world dump inside the Old Quarter window (widen the window by a cloned logical asset if needed, as the S004 precedent does). Draft D02 on Citywide Alert from the S050 dump. Output: anchor and route lists plus quickcheck drafts `Tools/Missions/drafts/o002…o010.json`. | Codex batch, about 1 day for D01, 1 day for D02 |
| E | Family-to-mechanic gap table with code pointers: which verbs can adapt Skirmish convoy, frontline or breakthrough code, and which need new mechanics (REPAIR and INTERACT command modes, civilian follow, airlift extraction facts). | Codex read-only, 0.5 day |

### Needs Unity (main lane, after Skirmish priority)

| Step | What | Who / effort |
|---|---|---|
| F | Re-verify O001 on the current tree. Compile, then `RunAriaVictory` on seeds 1102/2102/3102/4102/5102, `RunAriaVictoryPersian` on 9102, then `RunManualVictory`, `RunCheckpointResume` and `RunLifecycle`. About 9 Editor launches, roughly 1.5–2 h of lane time. This is the cheapest way to learn how much three months of shared gameplay edits broke. | Sonnet lane shift |
| G | Generalize the native runtime (step C) in the Codex lab, with focused Unity tests, then merge. Then build O002 (ESCORT, on convoy truck code) and O003 (REPAIR, which needs a new Repair command and a repair-specialist role). | Opus or Codex lab, about 3–5 days for the registry plus O002, and 3–5 more for REPAIR |
| H | Layout dumps for the D03–D06 candidate sources. One short run each, about 6 runs. | Sonnet, 1 h of lane |
| I | Extend the ARIA queue to `O###:seed:locale` jobs on the Operations harness, with evidence rows. | Codex lab, 1–2 days |
| J | Per-family native systems and ARIA skills for the remaining 9 families. This is P5 in `Design/Roadmap/Operations/DELIVERY.md`. | Several weeks; the largest item |

### Needs an owner or orchestrator decision (`decisions_pending.md`)

1. **The Operations definition of done.** `Design/Roadmap/Operations/ACCEPTANCE.md` §"ARIA
   coverage matrix" asks for:
   - 5 seeds per mission with at least 4 of 5 wins;
   - a win on every exposed difficulty;
   - EN and FA wins;
   - a full city run.

   That is about 300 Regular attempts plus 180 difficulty wins. Options: adopt the program's
   family-certification rule (directive §3) and gates 1–4 instead, or keep it. Also decide whether
   each mission must settle into the city and return to the dashboard, or whether a
   mission-to-result-to-return cycle is enough until P7.
2. **New shared mechanics and roster roles count as game-rule changes.** They are: Repair and
   Interact command modes, civilian follow, the repair-specialist, anti-air-infantry,
   recon-infantry and support-infantry roles (`P0_ROSTER_FEATURE_LEDGER.md`: still unbound), and
   evidence carry beyond O001. Also needed: a waiver or design that avoids each one.
3. **Mission UI review (`AGENTS.md`).** New family HUDs (escort Go/Hold, repair progress, civilian
   release) count as new mission UI, so they need ImageGen mockups and an owner visual review.
   O001's approval covers only the RECON HUD.
4. **Tooling permission.** `MISSION_PLAN_2026-11-16.md` forbids new tools and evidence schemas
   without asking. Steps D and I extend existing tools; confirm that this is allowed.
5. **Production order.** Options:
   - free intro O001–O003 first (recommended; `DELIVERY.md` B12/product contract);
   - or by Skirmish-overlap families (ESCORT↔CE, SEIZE/DEFENSE↔FC, EXTRACT↔BT, BREACH↔BA) once
     each Skirmish type is accepted.

## 5. Risks

- **R1. Only one of 12 families runs natively, and the runtime is hard-coded to O001.**
  - Mission identity is checked by string equality in 4 places, and there is one config resource
    path.
  - The 13 rule verbs exist only in the abstract P2 model. Host "AriaWon" for O002/O003 proves
    nothing in the shared world.
- **R2. The base `TacticalCommandMode` lacks Repair and Interact.**
  - Its values are `None, Select, Move, Attack, Hold, Stop, Build, Special, Scan, Board`
    (`Assets/Game/Scripts/Contracts/TacticalCommandContracts.cs:3-15`).
  - Civilian escort, generic airlift extraction and breach path-blocker updates are not
    implemented.
  - Recon, support, repair-specialist and anti-air-infantry roles are unbound.
  - These are new game rules and need a ruling.
- **R3. O001 evidence is stale.** It was produced on 2026-09-24, before three months of
  shared-runtime edits, including today's combat VFX. Since then the S026 long-session P0 found an
  allocator error and a settlement exception at the deadline. O001 could be broken now.
- **R4. Source fit is unmeasured for D03–D06.**
  - D04 needs two existing land crossings.
  - D06 needs an LZ and air access. AIRLIFT missions O018, O045 and O058 need a transport
    helicopter flight route and LZ capacity.
  - The Skirmish Airport question (owner chose to investigate, 2026-11-19) may affect D06.
  - Do not re-run `CH05M01CitywideAlertConfigBuilder`, which would overwrite CH05-M01; see
    `notes/airport-map-feasibility-2026-11-19.md`.
- **R5. Lane capacity.**
  - Operations missions target 8–15 minutes, against 2–6 minutes for Skirmish ARIA wins.
  - The ACCEPTANCE matrix as written needs at least about 60 hours of match time on the single
    lane before retries.
  - Gate 2 pacing (wins at 20–70% of a 720–1200 s deadline) is untested for Operations.
- **R6. Strategic loop integration is partial.**
  - The dashboard and profile commit work for the O001 offer only
    (`OperationsMissionPresentationSystem.cs:411`).
  - Multi-day offers, intro and full scope, and first-clear deduplication are planned (P4R R4–R6,
    product contract), not shipped.
- **R7. Docs and tools are stale.**
  - The Windows shadow-worktree wrappers conflict with the single main-checkout rule.
  - Older plan amendments (D05 terrain, Demo 2 environments) are superseded but still present.
  - `missions.csv` Skirmish map labels for MP and IB are wrong.
  - Workers must be told explicitly which document wins:
    `CAMPAIGN_MAP_REUSE_POLICY.md`, then `MISSION_PLAN_2026-11-16.md`.
