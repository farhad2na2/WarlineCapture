# Skirmish implementation work packages

Proposed programming assignments, 2026-09-21. Start at [IMPLEMENTATION_HANDOFF](IMPLEMENTATION_HANDOFF.md). These tickets make the existing E0–E8 milestones executable; they do not claim implementation or authorize unrelated changes. All named new classes are proposed. Read applicable repository instructions before editing source, assets or running Unity.

## Assignment boundaries and dependency order

Use one owner per shared subsystem and one content owner per map/objective packet. A content agent consumes shared systems; it does not fork the production, objective, AI or save implementation for its six scenarios. Coordinate edits to `MatchSceneView`, the catalog builder, shared UI contracts, localization catalog and system registration. Preserve the unrelated in-progress Industrial Basin/library changes; inspect the current diff and integrate through the existing owner rather than replacing it.

| Ticket | Existing milestone | Inputs that must exist before closing this ticket | Outputs / primary owner |
|---|---|---|---|
| SK-00 | E0 | Current source and instructions | Baseline, contract types and capability inventory |
| SK-01 | E1 | SK-00 | Definitions, compiler, expanded launch |
| SK-02 | E1/E4/E6 | SK-01 | Roles, producers, combat/air capability certification |
| SK-03 | E1/E3/E5 | SK-01 | Economy, reservations, research, capacity |
| SK-04 | E2/E4 | SK-01 | Army control, visibility, movement/transport |
| SK-05 | E3/E4/E5 | SK-02/03/04 for the capabilities being exercised | Enemy strategy and public ARIA skills |
| SK-06 | E3 | SK-01/04 and a functional ground slice | Expanded Base Assault |
| SK-07 | E5 | SK-01/04 | Frontline Control |
| SK-08 | E5 | SK-01/04, certified transport for assisted route | Breakthrough |
| SK-09 | E5 | SK-01/03/04 | Convoy Escort and shared service-pad repair |
| SK-10 | E5/E7 | SK-01; all state owners for final round-trip tests | Checkpoints, result, replay and migrations |
| SK-11 | E0/E3–E6 | SK-01; relevant movement/air/objective contracts for certification | Five maps and twenty objective layouts |
| SK-12 | E7 | SK-01/05/10; readiness rows from SK-13 for publication | Library, briefing/HUD, EN/FA and publication enforcement |
| SK-13 | E8 | Relevant SK-02–11 capabilities and SK-12 publication validator | Per-entry acceptance and evidence |

This is a capability DAG, not a requirement to finish all advanced units before a playable ground slice. SK-10/11/12 scaffolding starts from SK-01 schemas. SK-12's validator is available to SK-13 before any row is published; certified rows are its final input, so no circular implementation dependency exists. SK-05 consumes the public objective schema first, then integrates each objective's actual state as SK-06–09 land.

First demonstrable slice: expanded S001 with Standard ground control/economy/Base Assault and honest capability gating. It is a development reference until the **whole G profile**, including recon/transport/siege, passes. Next, prove one case for each objective and both starts; finish air/counters and restore paths; then author all six variants per packet. Release only individually certified entries, grouped into up to 24 per map. War/Large War require their own device/recovery gates.

## SK-00 — pin baseline and contracts

- Read the source seams in [TECHNICAL_ARCHITECTURE](TECHNICAL_ARCHITECTURE.md). Record base commit, dirty-source ownership, asset hashes and actual prototype mapping 0/1/3. Preserve Editor-only index 2.
- Create the proposed `Game.Skirmish.Contracts` boundary and typed IDs/enums/payloads listed there. Define reason codes for unsupported role/map/size/difficulty, insufficient Materials/Fuel/capacity, missing producer/readiness, stale command, blocked spawn and incompatible save.
- Use [ROSTER_SOURCE_AUDIT](ROSTER_SOURCE_AUDIT.csv) as the source inventory, not a certification list. Resolve each actual prefab/config reference and verify the claimed existing producer before implementing its adapter. Do not infer abilities from names.
- Deliver a baseline report with compiler/architecture status, three legacy smoke results, source/config hashes, target device identity and the performance measurements required by ACCEPTANCE. If the current tree cannot compile, isolate the owner/failure before attributing it to this expansion.
- Close when contracts have no UI/runtime dependency cycle, the 74 inventoried configurations have explicit dispositions, and reproducible probes report actual entities and counters. A missing capability becomes a named SK-02–11 task; it is not an invitation to ask the user to redesign the mode.

## SK-01 — typed definitions, compiler and launch

- Add the twelve config types, `SkirmishResolvedSetup`, `SkirmishLaunchPayload`, session/ownership components and composition helpers from TECHNICAL_ARCHITECTURE. Create shared objective/army/start/size/difficulty/economy/intel/upgrade assets under `Assets/Game/Configs/SkirmishExpansion/Shared/`.
- Implement `SkirmishDefinitionBuilder` and `SkirmishSetupCompilerValidation`. Import stable catalog columns by validated schema. Keep the original catalog CSV unchanged; treat the new manifest as planning input, not proof of readiness.
- Compile all 120 definitions at all three sizes. Compare every numeric value and designated role to the 360 setup vectors. Reject missing data, duplicate IDs, overflow, insufficient runway/anchor capacity and unsupported capabilities with field-specific reasons.
- Extend the catalog with definition/version/readiness references. Change expanded launch to pass the resolved immutable snapshot through existing scene readiness. Keep the old normalizer/index adapter exclusively for legacy sessions. Add no new per-S-ID `switch` cases.
- Add `SkirmishSessionInitializationSystem`, `SkirmishScenarioSpawnSystem`, `SkirmishSessionCleanupSystem`. Activate after all required entities/anchors exist; on failure remove only attempt-owned objects and return a visible failure. Repeated load/replay must neither duplicate grants nor leak entities.
- Evidence: all 360 compiler vectors; round-trip stable selection; missing-definition failure; cancelled transition; duplicate callback; same-seed setup hash; legacy 0/1/3 and stress 2 still dispatch correctly.

## SK-02 — roles, production and advanced platforms

- Implement `SkirmishRosterProjectionSystem` and typed role data. Use the exact canonical source configs and producer table in [ROSTER_AND_ECONOMY_IMPLEMENTATION](ROSTER_AND_ECONOMY_IMPLEMENTATION.md). Attach immutable base-stat overlays once; remove blanket repeated small-prototype tuning from expanded sessions.
- Implement `SkirmishGroundStagingBuilder` and the specified modular Ground Staging prefab/config. It exposes a vehicle queue and separate logistics queue, valid ground spawn/rally pads, health/selection/build controls and EN/FA identity. It is not an Expert Tent rename or hidden spawner.
- Route production through the existing `BuildingProductionRequestBoundary` and SK-03 reservations. Infantry purchases create four individual squad members, with valid partial-loss and delivery accounting. Bind drone production to the expanded intel station; preserve old Airport recipes outside this mode.
- Certify in increments: five infantry roles + car/APCs/tank; legal AA and radar; helicopter transport/attack/recon; siege/jets/transport plane. For each: target domains, damage/armor interaction, range, movement, fuel, producer, delivery, selection, order feedback, death, cost and counter. Reuse functioning systems and add missing ECS behavior; do not claim certification from a renderer or prefab label.
- Aircraft need spawn/idle/flight/attack/return/landing/refuel states, capacity reservations and loss/cancel behavior. Preserve actual aircraft dimensions and typed pad compatibility. Weaponless recon/transport must never inherit an attack weapon.
- Evidence: complete production-to-death runs for every enabled role, A/G exclusions and C inclusion, affordable counters before hostile offensive air, role stats unchanged by difficulty, vehicles no longer rejected by substring population policy. Record exact accepted role tank capacities in setup evidence.

## SK-03 — economy, cap reservations and research

- Implement `SkirmishCapacityReservationSystem`, `SkirmishCapacityLifecycleSystem`, `SkirmishResearchSystem` and their typed states/receipts. Extend the shared eligibility boundary so UI, enemy and ARIA all hit the same cost/cap/facility rule.
- Enforce infantry/ground/air/Supply/support/building/barrier caps from MATCH_SETUP and the setup vectors. Count queued, in-transit and live quantities exactly once. Embarking changes location, not population; delivered/evacuated objective tombstones release live capacity exactly once.
- Author separate fabrication and refinery profiles with the reconciled input/output/time table. Check storage before consuming inputs; real trucks transfer real Oil/Fuel/Materials. Expose producer and carrier waiting reasons. The second Established refinery adds capacity, not free input.
- Implement readiness and three category upgrades, prerequisites, grants, queue locks, cancellation/destruction refunds and percentage-preserving vehicle health. Derive upgraded values from base stats once, including units produced later. Ground Staging supplies the vehicle upgrade and initial designated HQ supplies readiness; a replacement can research without becoming a replacement victory base.
- Evidence: simultaneous queue race at cap; partial/failed delivery; producer/carrier death; replay; full output storage; paid research cancellation/destruction; upgraded half-health vehicle; equal factions' receipts; measured first counter/vehicle/air timings. Use the exact refunds in the owning contracts.

## SK-04 — army UI, legal visibility and movement

- Add group/member and known-contact data plus `SkirmishArmyGroupSystem`. Extend selection/army UI with stable groups, paging, multi-select, split, rally and compatible order intents. The old five-squad touch slots must not silently hide the rest of the army.
- Publish public capability/selection models through the UI gateway. Reject stale revision commands visibly. Combat group select excludes logistics by default; convoy objective selection is explicit and reachable even under overlapping escorts.
- Share fog/intel between attack eligibility, picking, auto-acquisition, minimap, enemy perception and ARIA's public projection. Unknown enemy cash/queues/transforms remain unknown. Last-seen markers are distinguishable and expire; dead live markers disappear immediately.
- Implement missing navigation/transport behavior through shared owners only after profiling: route capacity, blocked-path retries, wreck bypasses, manual-order priority, passengers retaining IDs, compatible load/unload, aircraft pad reservation and recovery. No teleporting convoy or evacuees through a blocked route.
- Evidence: full touch army control, both map routes, high-density congestion, boarding and carrier loss, hidden/stale contacts, Stop/takeover during selection/camera/boarding, shared Campaign regression for changed selection/pathing/transport.

## SK-05 — enemy strategy and ARIA skills

- Add `SkirmishEnemyStrategySystem`; extend `AriaSkirmishPlanSystem` and public contracts with the generic skills/state transitions in [OBJECTIVE_IMPLEMENTATION](OBJECTIVE_IMPLEMENTATION.md) and [AI_AND_ARIA](AI_AND_ARIA.md).
- Share pure affordability/role/objective scoring; keep perception and execution boundaries separate. Enemy issues legal commands; ARIA operates actual visible controls. Introduce no backdoor producer/objective/gameplay mutation API to the ARIA assembly.
- Implement public affordance discovery, visible action, acceptance observation and bounded retry for recruitment, structures/research, groups/camera, scout, attack/hold, transport, capture, convoy repair/escort and evacuation. Three unsuccessful attempts at the same action require a fresh diagnosis/alternative or explicit handback.
- Implement the four objective policies and four difficulty behavior profiles. Use seeded personality and legal observed events, score diagnostics and hysteresis. Remove prototype route exceptions only after the shared route policy wins their regression runs.
- Evidence: all objective policies, resource parity, withheld seed/map-route samples, no hidden-state dependency, takeover cancels pending touches, no input after result, correct EN/FA teaching, full normal-speed wins. Finish general skills before trying to repair a particular S-ID with a scripted solution.

## SK-06–09 — objective owners

Each owner implements `SkirmishObjectiveFactProjectionSystem` integration plus its objective system, typed state, HUD projection, legal planner goals, checkpoint codec and fixture table. `SkirmishOutcomeSystem` is the only terminal writer; objective systems submit candidates after same-tick damage/movement facts. Use exact algorithms, priority and fixtures in [OBJECTIVE_IMPLEMENTATION](OBJECTIVE_IMPLEMENTATION.md).

| Ticket | Types / obligations | Required distinguishing evidence |
|---|---|---|
| SK-06 BA | `SkirmishBaseAssaultObjectiveSystem`, designated base roles | Original vs replacement base; reciprocal same-tick death draw; both alive deadline draw; lost field army is not automatic loss |
| SK-07 FC | `SkirmishFrontlineObjectiveSystem`, zone/ticket components | Three ground polygons; 8 s neutralize + 8 s capture; contested freeze; empty grace/decay; 500 tickets; majority drain; reciprocal terminal arbitration; deadline comparison |
| SK-08 BT | `SkirmishBreakthroughObjectiveSystem`, designation/corridor/evacuation records | Exactly twelve original soldiers within the starting census; player-only Standard Field substitution; continuous 20 s hold; latched exit; live dismounted 3 s dwell; eight delivered; five losses impossible |
| SK-09 CE | `SkirmishConvoyObjectiveSystem`, truck/dwell records; `VehicleFieldRepairSystem` and repair request/state | Three support trucks; five-second dwell; two deliveries win/two losses fail; independent logistics; alternate route; death beats arrival; paid stationary service-pad repair and cancellation |

Repair is a shared ground-vehicle command with the exact range/cost/duration/threat/refund rules in OBJECTIVE_IMPLEMENTATION. Its owner coordinates with SK-03 receipts and SK-04 contextual UI; no private convoy heal. BA/FC do not inherit BT/CE base-loss exceptions. BT/CE do not win by destroying the enemy base. Objective progress uses match ticks and freezes at pause/result.

## SK-10 — checkpoint, result and replay

- Implement `SkirmishCheckpointSystem`, `SkirmishResultSettlementSystem`, expanded save DTOs, composition restore and explicit migration. Store stable IDs/config hashes, not ECS handles.
- Serialize actual economy/cap reservations, living entities/health/fuel/orders, cargo/passengers, upgrade/research timers, visibility knowledge, objective progress/tombstones, deterministic RNG streams and terminal receipt. Restore atomically into a fresh owned session, rebuild references, verify conservation before unpausing.
- Save at a consistent tick boundary; write temporary file then atomic replace. Reject unsupported content versions safely with preserved diagnostic evidence. Never silently replace an old scenario with an expanded one or award its completion.
- Persist results exactly once per session/definition/version/difficulty/size; replay creates a new session and exact selected fresh start. Custom/Sandbox and legacy results cannot satisfy standard expanded completion. No account-resource reward system is added here.
- Evidence: round-trip every objective mid-progress and near terminal; airborne passengers; queues in flight; partial repair/research; OS interruption and interrupted file write; incompatible version; repeated callbacks; replay after both loss and victory. War/Large War stay gated until accepted recovery.

## SK-11 — maps and objective layouts

- Include D2-A01/A02 from the [Demo 2 integration guide](../../Demo2_Asset_Integration_Guide.md): resolve the verified shortlist, create project-owned variants/materials and qualify one isolated IB logistics/utility yard. Record source/output GUIDs, scope, ownership and same-load desert comparison. This art pilot does not replace expanded DB as the first gameplay slice.
- Apply accepted kits with D2-A03/A04 to the five maps as specified in MAP_IMPLEMENTATION. Treat crossing geometry, interactive objects and any future playable vehicle as separate capability work; retain vendor assets, shared Campaign anchors, current production bindings and frozen rollback output. Require D2-V1–V6 alongside SK-11/SK-13 evidence for changed content.
- Implement `SkirmishMapLayoutBuilder`/validator and typed anchors/routes in [MAP_IMPLEMENTATION](MAP_IMPLEMENTATION.md). Produce five map definitions and twenty objective layout definitions; each packet consumes the matching map/objective variant.
- Reuse DB/CC/IB identities; MP/AP are new authored layouts. Start from the documented normalized candidates, fit actual ground geometry and measure routes. Store final world transforms in assets, not policy code. Preserve Campaign anchors in shared regions.
- Author deployment grids, all producer exits, both base/expansion/air pads, FC polygons, BT corridors/muster/exit, CE three start bays/destination/holding pockets and service pads. Defender BT/CE groups follow the 40/40/20 allocation rule, with exactly two disclosed extra towers.
- Fix each map's listed geometry/path defect. Validate all size footprints, largest allowed vehicles, two independent truck routes, landing/runway clearances, safe camera bounds and complete ground-only approaches. No claim of coverage from a map screenshot alone.
- Evidence: path probes and real movement for each role class and objective layout, dense combat/wreck blockage, placement preview vs actual spawn, EN/FA camera/UI captures, measured transit times and target-device performance.

## SK-12 — library, localization and publication

- Extend catalog/config projection, `QuickCustomScreenView`, match views and the three proposed panels. Show map/objective/profile/start/size/difficulty, exact costs/starting grants, win/loss/deadline, fog and blocked capability reasons from the same resolved setup.
- Author EN/FA keys specified in each packet. Verify RTL ordering, long copy, numbers, queue timers and touch targets. First visit remains Regular/Standard; expose only certified extra sizes/difficulties.
- Implement `SkirmishPublicationValidator`. Readiness requires matching code/content hashes, manual/ARIA/edge/recovery/device evidence and allowed configuration matrix. Remove the hard-coded three-prototype publication override only through a versioned compatibility path. An asset's existence cannot set Playable.
- Separate Custom/Sandbox snapshots and completion. Preserve legacy selection migration; include explicit no-support reasons instead of silently choosing another battle.
- Evidence: filter/select/restart/back; stale/unavailable definition; every localized objective HUD/result; invalid and custom combination blocked or labeled; uncertified rows hidden/disabled accurately; legacy prototypes still selectable as their own supported versions.

## SK-13 — author and certify the remaining battle packets

1. Take one six-entry packet from [Scenarios/README](Scenarios/README.md), or a specific row from [WORK_QUEUE_004_120](WORK_QUEUE_004_120.csv). Do not interpret its work ordinal as `ScenarioIndex` or rename its S-ID.
2. Resolve its dependency/capability list. If a shared feature is missing, implement the owning ticket or hand it to that owner with a concrete failed fixture; do not shrink the roster/rules to bypass it.
3. Create each entry's two assets with Editor builders, compile all sizes, bind authored anchors and exact starting force, then complete the per-entry implementation sequence and checks. Add no scenario-specific runtime/ARIA class.
4. Run the full acceptance matrix relevant to that entry, including manual victory and the per-entry ARIA sample. Preserve every loss and failed seed. Record actual terminal Victory; launch success, Draw and forced result are failures to satisfy the win gate.
5. Write `Design/AgentReports/SkirmishExpansion/SNNN/acceptance.md`, `runs.csv`, compiler census, config/code hashes and artifact references. `runs.csv` fields: `run_id,catalog_id,definition_version,code_hash,config_hash,size,difficulty,seed,locale,device,executor,normal_speed,started_at,result,end_reason,duration_seconds,input_violations,human_interventions,trace_path,log_path`.
6. Publish only the certified matrix. All 120 scenarios must reach full planned capability coverage; temporary development gating does not count as final acceptance. Recertify changed shared mechanics across affected entries, not only the newest packet.

Copy-ready assignment:

> Implement the Skirmish packet `<map>_<objective>.md` following IMPLEMENTATION_HANDOFF, TECHNICAL_ARCHITECTURE, OBJECTIVE_IMPLEMENTATION, ROSTER_AND_ECONOMY_IMPLEMENTATION and MAP_IMPLEMENTATION. Preserve stable S-IDs and legacy prototype/stress mappings. Complete the listed SK dependencies using their owning shared classes; create typed scenario/setup assets and EN/FA copy with the repository's Unity authoring workflow. Match the 360 setup vectors and retain exact objective/army/start rules. Use legal shared commands and visible-control ARIA; prove normal manual victory and the required per-entry ARIA wins, recovery, edge cases and device gates. Record all failures and evidence under each S-ID, then publish only certified configurations. Do not mark planning, asset generation or a forced outcome as acceptance. Resolve specified design choices from these docs; report implementation defects with a reproduction and proposed narrow fix.

## Validation entry points and commands

The documentation generator exists now; the following Unity suites are **proposed deliverables**, not currently callable validation. Each owner adds its suite under `Assets/Tests/Editor/SkirmishExpansion/`, namespace `Game.Tests.Editor`, with `public static void RunFocusedValidation()` and an exact success marker only after every assertion passes:

| Proposed suite | Coverage / success marker |
|---|---|
| `SkirmishExpandedDefinitionTests` | 120 definitions/360 setups/identity/launch: `[SkirmishExpandedDefinitionTests] result=Passed` |
| `SkirmishExpandedEconomyTests` | Role/cap/economy/research/repair accounting: `[SkirmishExpandedEconomyTests] result=Passed` |
| `SkirmishExpandedObjectiveTests` | All four reducers and ordered edge fixtures: `[SkirmishExpandedObjectiveTests] result=Passed` |
| `SkirmishExpandedCheckpointTests` | State conservation, migration, terminal receipt: `[SkirmishExpandedCheckpointTests] result=Passed` |
| `SkirmishExpandedCatalogTests` | Map/schema/readiness/localization/publication: `[SkirmishExpandedCatalogTests] result=Passed` |

Pure reducer/compilation tests are useful but do not replace normal-speed full matches and device evidence. Use existing ARIA play/watch validation facilities, extending them to accept the immutable definition/size/difficulty/seed/locale payload and log the actual selected configuration. No acceptance run may inject an army, skip gameplay, increase money or call the victory reducer directly.

Documentation consistency (runs now, no Unity):

```sh
rtk proxy python3 Design/Roadmap/Skirmish_Expansion/Tools/generate_handoff.py --check
rtk git diff --check
```

After implementing a focused suite, macOS invocation follows the repository wrapper. Keep Hub open and signed in; do not add `-batchmode`, bypass the wrapper or reset IPC:

```sh
rtk proxy Tools/CI/invoke_unity_macos.sh --timeout 600 --log /private/tmp/skirmish-expanded-definitions.log -- \
  -quit -executeMethod Game.Tests.Editor.SkirmishExpandedDefinitionTests.RunFocusedValidation
rtk proxy rg -F '[SkirmishExpandedDefinitionTests] result=Passed' /private/tmp/skirmish-expanded-definitions.log
```

Preserve the full log and check both wrapper exit and exact marker. Timeout, missing marker or exceptions fail the run. Follow the platform-specific AGENTS instructions for Windows rather than translating this into a direct Unity invocation. Follow the Unity CLI skill when using connected Editor commands for asset authoring; it does not override these validation wrappers.
