# Operations gap table and O001–O003 plan (2026-11-19)

This is offline prep under the owner decisions of 2026-11-19 (`decisions_pending.md`, last section). No Unity run, no asset or code change, no
commit. The only outputs are this note and two new drafts in `Tools/Missions/drafts/operations/`. Every `file:line` refers to HEAD `b9da928d7`.
"P2" means the pure-C# abstract tactical model in `Assets/Game/Scripts/Operations/Tactical/`. "Native" means the shipping ECS world.

## 1. Rule verbs

The "missions" column counts the mandatory graphs in `Design/Roadmap/Operations/Missions/D01–D06` that use the verb. The top three verbs gate
most of the catalog: HOLD (43 missions), PROTECT (39) and CLEAR (33).

P2 compiles only 8 verbs. `OperationsTacticalRules.IsPackageVerb` (`OperationsTacticalRules.cs:112`) excludes VISIT, RESCUE, AIRLIFT, STOP and
BREACH, and the compiler rejects them with `verb_not_in_package` (`OperationsTacticalCompiler.cs:134`). The native command modes are
`None, Select, Move, Attack, Hold, Stop, Build, Special, Scan, Board` (`Contracts/TacticalCommandContracts.cs:3-15`). There is no Interact or
Repair mode.

Effort is in agent-days of code and tests. Unity lane time is listed separately in section 4.

| Verb | Missions | P2 today | Native today | Reusable code (file:line) | Must build | Effort |
|---|---|---|---|---|---|---|
| SCAN | 24 | Yes: `OperationsTacticalSession.cs:785` (6 s, 8 m, hidden until observed) | O001 only, inside the monolithic `OperationsReconObjectiveSystem.cs:22-175`. Shared Scan command: `Systems/ScanIntelCommandSystem.cs:12` | `SkirmishPublicScanFeedback.cs:10,32` (accepted/completed receipts); O001 site buffer `OperationsReconComponents.cs:90` | Graph-driven SCAN rule keyed by node and site ID, lifted out of the recon system. Reconcile duration: P2 6 s (`OperationsTacticalRules.cs:81`), native and brief 15 s | 1–1.5 |
| VISIT | 4 | No | No | `SkirmishBreakthroughReducer.cs:54,100` (corridor progress); hostile waypoint follower `OperationsReconPatrolSystem.cs:23` | P2 verb, then a native ordered-waypoint rule: 8 m points, at least 2 surviving members, no reset. First needed by PATROL O007 | 2 |
| HOLD | 43 | Yes: `OperationsTacticalSession.cs:820` (12 m, no hostile, reset on contest; plus a P2-only 5 s re-issue "AFK cut", `OperationsTacticalRules.cs:88`) | None in Operations | `SkirmishFrontlineReducer.cs:53-92` (zone presence, contest, empty grace); `CampaignMissionDefenseRuleUtility.cs:8-15` | Native zone rule: dismounted eligible infantry, no living hostile in 12 m, continuous timer. Decide whether the re-issue rule exists natively; the brief does not have it. Needed by O002 and O003 | 1.5–2 |
| CLEAR | 33 | Yes: `OperationsTacticalSession.cs:746`; the compiler rejects a wave/clear cycle (`OperationsTacticalCompiler.cs` `ValidateWaves`) | None as a node. O001 finite waves: `OperationsReconWaveSystem.cs:23` | Death and destruction facts: `SkirmishBaseAssaultObjectiveSystem.cs:75`, `SkirmishObjectiveFactProjectionSystem.cs:96`; role tags: `OperationsReconComponents.cs:20` | Role-bound hostile identities, including pending wave members; destruction only | 1 |
| INTERACT | 16 | Yes: `OperationsTacticalSession.cs:855` (6 m, hostile in 12 m interrupts) | O001 RecoverEvidence only: `OperationsReconObjectiveSystem.cs:74-80`; action enum `OperationsReconComponents.cs:9` | O001 evidence carrier logic (same file, :36-108) | Interact command mode (approved new mechanic); generic channel; `OperationsEvidenceCarrySystem` (carry, drop on death, recover, no duplication). First new use: O004 | 2–3 |
| REPAIR | 12 | Yes: `OperationsTacticalSession.cs:289,891` (40 Materials once, 75%, pause under threat; 18 s in P2, `OperationsTacticalRules.cs:95`, against 45 s in the brief) | None; no Repair mode | `VehicleFieldRepairService.cs:114,143,196` (quote, enqueue, tick, interrupt on damage); `VehicleRepairRules.cs:13-29` (quote, refund, took-damage); `SkirmishMaterialsService.cs:93` (transactional spend); `SkirmishNativeRepairCheckpoint.cs` | Repair command mode; site repair, not vehicle repair; one charge per node; restored-site retry rule; checkpoint. Bind the repair-specialist role (P0 roster ledger: unbound). O003 | 3–5 |
| ESCORT | 11 | Yes: `OperationsTacticalSession.cs:347` (Go, route lock), `:376` (Hold), `:931` (10 s unload, delivered once) | None in Operations | `SkirmishConvoyReducer.cs:41-83` (same shape: 3 trucks, 2 required, destination dwell); `SkirmishConvoyTruckRuntime.cs:14,59`; `SkirmishConvoyTruckCommandEligibility.cs:14`; `SkirmishConvoySpawnHooks.cs:13,48`; `SkirmishNativeConvoyCheckpoint.cs`; route follower `CampaignMissionPatrolOrderSystem.Convoy.cs:16` | Convoy Go/Hold group order through normal movement; route choice locked at the branch; an empty truck does not count; 10 s unload zone. O002 | 3–4 |
| RESCUE | 6 | No | No. Only ambient citizens (`Components/CitizenComponents.cs:7`) and Campaign civilian counts (`CampaignMissionAttemptFactComponents.cs:47`) | Boarding: `TransportBoardingApproachSystemHelper.cs:97,167`; `SkirmishPublicBoardingPresentation.cs:47,165` | `OperationsCivilianEscortOrderSystem` (approved new mechanic): release by 10 s interact, escorted follow, stop on threat, transfer, boarding identity, checkpoint. O005 | 5–8 |
| AIRLIFT | 4 | No | Campaign M04 only: `CampaignMissionExtractionRuleUtility.cs:12`; guidance is hard-coded to `saga.ch01.m04.airlift` (`CampaignMissionGuidanceProjectionSystem.Extraction.cs:12`) | Air pickup cell: `TransportBoardingApproachSystemHelper.cs:21`; `SkirmishPublicOriginalTransportProjection.cs:14`; unload: `SkirmishOriginalUnloadPresentation.cs:25,100` | Generic extraction facts separated from M04; LZ secure, board, fly, unload, count delivered identities; usable Fuel; D06 source fit. O018 | 5–8, plus source fit |
| STOP | 4 | No | No | Hostile convoy route progress: `CampaignMissionPatrolOrderSystem.Convoy.cs:16-25`; subject status: `SkirmishConvoyReducer.cs:7` | Three non-firing hostile trucks, route released at 120 s; escape is an irreversible failure. INTERDICT O008 | 2–3 |
| BREACH | 7 | No | Campaign M05: `CampaignMissionBreachRuleUtility.cs:8-12`, `CampaignMissionRuntimeSystem.Breach.cs:91,130` | `SkirmishBaseAssaultObjectiveSystem.cs:75,107` | Generic gate and path-blocker facts, then a traverse into the interior zone. O006 | 3–4 |
| EXTRACT | 27 | Yes: `OperationsTacticalSession.cs:986` (2 original infantry, secure exit, evidence at exit) | O001 exit count: `OperationsReconObjectiveSystem.cs:86-105` | `SkirmishBreakthroughReducer.cs:7,100` (evacuated status); `SkirmishOriginalUnloadPresentation.cs:25` | Lift out of the recon system; add an unloaded-transport arrival variant | 1, plus 2 for transport |
| PROTECT | 39 | Yes: `OperationsTacticalSession.cs:772` and `LatchIntactProtects` (pairs with a HOLD on the same anchor) | None (O001 has no protected site) | Building identity and destruction: `CampaignMissionRuntimeSystem.Breach.cs:130`, `SkirmishBaseAssaultObjectiveSystem.cs:75` | Protected-site entities with health; same-tick loss beats success; a site-alive fact persisted from the final world state. Open gate: can a bound map building be damaged, or does each site need a certified prop on a legal pad (see section 5)? | 1.5–2 |

## 2. Families

| Family | Missions (first) | Verbs in the graphs | Native today | Reuse | Must build beyond the verbs | Effort after the verbs |
|---|---|---|---|---|---|---|
| RECON | 6 (O001) | SCAN, INTERACT, EXTRACT | O001 only, hard-coded (section 3) | O001 systems | Known-search-zone projection; generalize O001 into the shared rules | Included in the registry work |
| ESCORT | 7 (O002) | ESCORT 7, HOLD 7, PROTECT 7, SCAN 5 | None | Skirmish convoy (table 1) | Cargo identities, route-branch lock, loss and arrival facts; Go/Hold HUD per mockup | 2 (HUD, ARIA, config) |
| REPAIR | 6 (O003) | CLEAR 6, REPAIR 6, HOLD 6, PROTECT 2 | None | Vehicle repair, Materials | Cost reservation, site state, Repair HUD per mockup | 2 |
| RAID | 4 (O004) | SCAN, CLEAR, INTERACT, PROTECT, EXTRACT | None | RECON rules | Confidence gate (Intel ≥ 40 offer rule), carried evidence | 1–2 |
| RESCUE | 5 (O005) | RESCUE 5, EXTRACT 5, HOLD 4 | None | Boarding helpers | Survivor accounting, release/follow HUD per the approved O005 mockup | 2–3 |
| BREACH | 5 (O006) | SCAN, CLEAR, INTERACT, BREACH, PROTECT, EXTRACT | Campaign M05 only | M05 breach facts | Path-blocker update, protected-object checks | 2 |
| PATROL | 4 (O007) | VISIT 4, CLEAR 4, HOLD 4 | None | Breakthrough corridors | Waypoint progress, blocked-route handling | 1–2 |
| INTERDICT | 4 (O008) | STOP 4, SCAN 4, HOLD 3 | None | Campaign convoy routes | Hostile transport routing, escaped-target terminal checks | 2 |
| DEFENSE | 5 (O009) | CLEAR 5, HOLD 5, PROTECT 5 | O001 finite waves only | `OperationsReconWaveSystem.cs:23`, `CampaignMissionDelayedWaveSystem.Defense.cs:8`, `SkirmishBaseThreatAlertSystem.cs:33-42` (warnings) | One wave director with objective-triggered phases (P2 already compiles the default 50/25/25 split) | 2 |
| FINALE | 6 (O010) | HOLD 6, PROTECT 6, REPAIR 6, EXTRACT 6, ESCORT 4, BREACH 2, RESCUE 1, CLEAR 1, AIRLIFT 1 | None | All certified rules | No new engine; composition and local milestone settlement; 15–20 min pacing | 1–2 each |
| SEIZE | 5 (O017) | HOLD 5, CLEAR 4, PROTECT 2 | None | `SkirmishFrontlineReducer.cs:53-92` (parallel zones) | Parallel capture nodes, occupation UI, counterattack wave | 1–2 |
| AIRLIFT | 3 (O018) | AIRLIFT 3, CLEAR 3, HOLD 3, PROTECT 3 | Campaign M04 only | Transport helpers | Fuel, landing and passenger checks; D06 LZ source fit | 2, plus source fit |

## 3. O001 hard-coding to remove

| Site | Today | Replace with |
|---|---|---|
| `Configs/OperationsReconMissionConfig.cs:22` | `ResourcePath = "Operations/StreetSignals"`, loaded at `Composition/OperationsMissionPresentationSystem.cs:68` | Per-mission config registry under `Resources/Operations/Missions/<code>`, keyed by mission ID |
| `Configs/OperationsReconMissionConfig.cs:23,41-62` | Identity is fixed to `operation.o001`; validation hard-codes FP_LIGHT/EP_CELL counts, 3 scans, 720 s | Validate against `OperationsCatalogIndex` (family, packages, deadline) and the compiled graph |
| `Composition/MatchSceneView.Operations.cs:9` | `IsOperationsSession => MissionId == "operation.o001"` | True for any catalog `operation.*` ID with a registered config, or when the Operations launch root exists |
| `Composition/OperationsMissionPresentationSystem.cs:392` | `ContentAccessRuntime.Evaluate("operation.o001")` | `Evaluate(definition.missionId)`. `ContentAccessAuthority.cs:188-190` already lists O001–O003 as free intro |
| `Composition/OperationsMissionPresentationSystem.cs:393,398` | "Street Signals" copy; a new run always uses seed 1102 | Title and copy from mission localization; the run seed from the run or offer, not the O001 fixture |
| `Composition/OperationsMissionPresentationSystem.cs:411` | Offer found by `missionId == "operation.o001"` | The offer the player selected on the dashboard |
| `UI/Shell/Ecs/UiShellEcsGateway.MissionHudRestrictions.cs:23` | Restrictions model built with the literal `"operation.o001"` from an `OperationsReconMissionComponent` query | Mission ID from the running mission component; family-specific HUD flags from data |
| `Editor/OperationsReconLaunchSmokeValidation.cs:169-175,193` | O001 seed, difficulty and offer | A mission-parameterized harness (for example `WARLINE_VALIDATION_OPERATION=O002`) with the mission code in its pass marker |
| `UI/Screens/MatchHudAssistantUiSystemHelper.Watch.cs:34-40` | ARIA Watch drives O001's `OperationsMissionScreenView` buttons (`ScanButton`, `OperationsMissionScreenView.cs:52`) | Visible controls generated from active node state (Scan, Recover, Go/Hold, Repair, Release) |
| `Tools/Missions/run_aria_queue.py:20,24` | Jobs are `S###:seed:locale` on the Skirmish harness | Accept `O###:seed:locale` jobs on the Operations harness, with evidence rows (allowed tool extension) |

Architecture recommendation: follow the Skirmish reducer pattern. Skirmish uses a pure state, a tick-facts reducer
(`SkirmishConvoyReducer.cs:83`, `SkirmishFrontlineReducer.cs:92`) and a native fact projection (`SkirmishObjectiveFactProjectionSystem.cs:96`).
For Operations, the P2 compiled graph (`OperationsTacticalCompiler.cs`) and the node-phase rules in `OperationsTacticalSession.cs` would become
one pure `OperationsObjectiveGraphReducer`, fed by native facts. Today P2 moves its own actors (`OperationsTacticalSession.cs` `MoveActors`), so
it cannot be dropped into the native world as is. The same reducer then serves quickcheck, the native world and ARIA, which stops the P2,
native and brief numbers from drifting apart. Do not create an `O00xSystem` or a mission-ID switch (`MISSION_IMPLEMENTATION.md`, "Twelve families").

## 4. Ordered plan: O001 re-validation, then O002, then O003

Definition of done (owner, 2026-11-19): per mission, 3 EN and 1 FA ARIA Regular wins, the menu journey and result/return. Each family's first
mission also needs one Easy and one Hard win. ACCEPTANCE.md's Partial, Defeat, Withdraw, checkpoint-restore and exactly-once settlement checks
stay. Production in Unity waits for the trigger (one accepted Skirmish mission per objective type).

| # | Step | Needs Unity | Effort |
|---|---|---|---|
| 0 | Offline, now: the O002/O003 drafts (done, section 6). Tool requests: let quickcheck run a draft for an authored mission (section 6); keep Operations drafts out of the Skirmish draft globs. Roster ledger: bind repair-specialist, support and recon to certified keys (P0 roster ledger; approved new roles). Pick one value set per rule (scan 6 or 15 s, repair 18 or 45 s, O002 hold 12 or 30 s, O003 hold 20 or 60 s) and record it in the brief; the owner's tuning permission covers pacing | No | 1–2 d |
| 1 | O001 baseline on the current tree, before any refactor: compile; `RunAriaVictory` seeds 1102/2102/3102/4102 EN, `RunAriaVictoryPersian` 9102, Easy and Hard (`WARLINE_VALIDATION_O001_DIFFICULTY`), `RunManualVictory`, `RunCheckpointResume`, `RunLifecycle`. Keep every failure; seed 3102 lost in September | Yes | 10–11 launches, about 2–2.5 h of lane; fixes unknown |
| 2 | Mission-keyed registry and removal of the hard-coding in section 3. Split `OperationsReconObjectiveSystem` into the graph reducer plus SCAN, INTERACT, EXTRACT and evidence carry. O001 must keep identical facts and settlement. Focused tests in the Codex lab | Lab tests | 3–5 d |
| 3 | O001 regression: rerun step 1's set on the refactored runtime. This is the gate for building O002 | Yes | about 2 h of lane |
| 4 | O002 rules: HOLD, PROTECT, ESCORT (convoy adapter on the Skirmish truck runtime), CLEAR for the street watch. Config and asset builder for `OperationsMission_O002`, `OperationsObjectives_O002` and `ScenarioSetup_O002` from the draft's candidates. Put new anchors in a per-mission logical asset; do not edit `OldQuarter.asset`, because its content hash pins O001 checkpoints. Source-fit runs: truck and APC flood on both routes, a replacement for enemy_b (section 5), and clinic site health | Lab, then lane | 3–5 d, plus 1 h lane |
| 5 | O002 HUD per the approved `Design/AgentReports/OperationsMockups/o002-escort-hud-v01.png`: one top-centre objective card with purpose, progress, timer, Go/Hold Convoy and a route chip; native Show Me style; keep ARIA Play/Stop and the field guide. ARIA: native visible-input adapter for the existing P4 EscortGo/EscortHold skills (`OperationsAriaInputSkills.cs:33-36`) | Lab, then lane | 2 d |
| 6 | O002 acceptance: ESCORT is its family's first mission, so 3 EN + 1 FA + Easy + Hard wins, manual Victory, Partial (one truck, then Conclude), mandatory-loss Defeat, Withdraw, checkpoint mid-convoy, settlement once, return | Yes | 9–10 launches, about 3 h of lane |
| 7 | O003 rules: Repair command mode and site-repair system (80 local Materials, one charge per node, pause under threat), role-bound CLEAR, HOLD 60, PROTECT on the clinic and both pumps. Physical pump props (section 5). Checkpoint between pumps | Lab, then lane | 3–5 d |
| 8 | O003 HUD per the approved `o003-repair-hud-v01.png`: Repair action, progress, a Materials "80/80" counter, interruption reason. ARIA Repair skill (`OperationsAriaInputSkills.cs:27-28`) through visible input. Intro completion after all three Victories (O003 brief) | Lab, then lane | 2 d |
| 9 | O003 acceptance: same set as step 6, plus interrupt-and-resume without a second charge and save/restore between pumps | Yes | 9–10 launches, about 3 h of lane |

Report visual review, automated checks, normal-input playthroughs and device acceptance separately (AGENTS.md "Mission UI").

## 5. Source-fit findings from the S002 dump (offline, 4 m bitmap)

These come from `Design/AgentReports/SkirmishExpansion/LayoutDumps/S002/dump.json`, restricted to the OldQuarter.asset playable window
1672–1912 × 680–856. The method treats scenery blocks as blocked, like `layout_dump_to_draft.is_blocked`, so it is conservative. Unity
reachability was not measured.

- **The enemy_b anchor (1900.5, 695.5) sits on a scenery block.** Its passable pocket (51 blocks) has no in-window connection to the main
  component (1,176 blocks); it connects only outside the playable bounds. A wave there cannot reach the clinic in-window. The candidate
  replacement is the south-spur road entry (1792, 684). O001 also uses this anchor, so check it in step 1.
- **The signal_c and relay_evidence anchors sit in small isolated scenery pockets.** O001's September ARIA wins reached them, so props are
  probably traversable, or the native snap or radius covers the gap. This is one reason to treat the bitmap result as conservative.
- **The window has almost no native building pads.** The only `IsPlacementValid` candidates (stride 24) are NW (1704, 840; an Oil Pump fits, used
  as the pump_west candidate) and NE (1896, 840), next to enemy_a. None was accepted on the central plaza. So the clinic is bound to an existing
  map building, and pump_east is a marker position. Any physical prop there needs its own pad measurement.
- **The SW spawn reaches the district only by the west road north to z≈826.** Both O002 routes therefore share their first 156 m and branch
  at the junction (1734, 826). route.main crosses open plaza (257.5 m) and route.safe follows the streets (333.3 m).

## 6. Draft and tool results

- Drafts: `Tools/Missions/drafts/operations/o002-clinic-supply-route.json` and `.../o003-courtyard-water-point.json`. They use the o004 format;
  measured positions and routes are under `campaign_source.physical_candidates` and `measured_routes`, which quickcheck ignores, and they are
  marked `candidate=1`. They sit in a subfolder because `Tools/Missions/test_drafts.py:23` and `check_draft.py:509` glob `drafts/*.json` as
  Skirmish drafts. Two O-drafts at the top level made the program's `test_drafts.py` gate fail with "top-level keys differ from example".
- **`quickcheck.py --operation-draft` exits 1 for both drafts by design:** `OperationsHost.cs:70` refuses a draft for a mission with an
  authored binding (O002 and O003 are authored in `OperationsAuthoredMissions.cs`). A scratch overlay compiled the same host with only that one
  guard removed. All 11 cases then passed against the full FP_SERVICE/EP_RAIDERS packages:
  - O002: Victory by route.main at tick 53, Victory by route.safe at 60, Partial by Conclude at 25, Partial at the 840 s deadline, Defeat, Withdrawn.
  - O003: Victory with split specialists at 84, Victory with sequential repairs at 104, Partial by Conclude at 30, Defeat, Withdrawn.

  Tool fix to request (allowed extension): an explicit flag that compares an external draft with the authored graph, without replacing it.
- **The authored O002/O003 in P2 differ from the briefs.** They use a subset of the packages (5 of 22 player units in both; 2 of 30 hostiles in O002, 4 in O003), Player-faction
  cargo, and hold times of 12 s and 20 s against the briefs' 30 s and 60 s. The drafts follow the briefs. Cargo is Neutral there because
  `OperationDraft.cs:56,71` counts every Player spawn against the fixed package.
