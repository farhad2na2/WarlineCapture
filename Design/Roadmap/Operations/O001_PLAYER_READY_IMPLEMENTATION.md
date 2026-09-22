# O001 player-ready implementation plan

Date: 2026-09-22. Status: **Implementation in progress; O001 remains not player-ready.**

Current user-approved target: **Unity Editor readiness only**. Android builds, mobile/device certification and phone release acceptance are out of scope for this implementation pass. Keep those release gates recorded below as deferred; they must not block the Editor milestone or be claimed as passed.

Current checkpoint: disk-backed profile transactions and shared ECS recon rules are implemented; the normal Operations menu now deploys the authored 16-versus-20 finite-force candidate into the shared city world. A button-event smoke verifies real simulation progress and opening-squad visibility. Full outcome/replay validation, active-world checkpoint restoration, tactical review, localization/ARIA acceptance and player/device gates remain open. See [implementation evidence](../../AgentReports/Operations/O001_IMPLEMENTATION_PROGRESS_20260922.md).

Owner: Operations integration lead. Contributors: gameplay, UI/ARIA, environment, persistence and QA owners. Scope: **operation.o001 — Street Signals**, using the same shared RTS simulation, input, presentation and save boundaries as Campaign and Skirmish.

Source baseline: `02da3227dce8c2908743f663aa4ae49c5b3d8cd9`. Evidence: [O001 readiness review](../../AgentReports/Operations/O001_READINESS_REVIEW_20260922.md). Recheck source and dirty files before implementation; the baseline is not a claim that another checkout is unchanged.

## Why this plan is needed when a plan already exists

The original [architecture](ARCHITECTURE.md), [delivery packages](DELIVERY.md), [mission brief](Missions/D01_old_quarter.md#o001--street-signals) and [acceptance contract](ACCEPTANCE.md) already required real mission deployment, shared gameplay, visible input, persistence and ARIA wins. Those requirements were not missing.

P0–P4 delivered Operations-owned contracts, rule/transaction models, authored graphs, a planner and Play Mode capture tooling. Their scoped work deliberately left the shipping scene, UI, profile and assembly integration points closed. In particular, P3's launch request explicitly has `InvokesSharedSceneView=false`, and its loop store is an in-memory model. The label “real mode loop” and P4's conflicting playable statements overstated what those deliveries established for a player.

This plan supplies the missing integration completion package, **P4R**, before expansion to more mission families/maps. It implements the original product intent; it does not replace Operations with a second combat engine or lower acceptance to a screenshot. Historical captures remain useful evidence of the model and renderer.

## What “ready” means

| Milestone | Required proof | Current O001 status |
|---|---|---|
| Rules/presentation prototype | Graph runs, terminal result settles, capture renders | Verified at the review baseline |
| Manual playable in Editor | Normal menu/briefing/deploy, actual input, full win or loss, working result/return/replay | Pending |
| Internally ready alongside existing Campaign/Skirmish baselines | Manual EN/FA, real-input ARIA, recovery, mode isolation, reviewed tactical experience and presentation | Pending |
| Player-release ready | Internal gate plus unfamiliar-player review and supported-device acceptance on the exact candidate build | Pending |

A device pass is not required to call an Editor route manually playable. It is required to claim phone release readiness. Compare Operations with the actual accepted Campaign/Skirmish flows and evidence, not with every planned expansion feature in those modes. O001 completion does not certify O002/O003 or the full city run.

## Preserve the mission contract

- Keep `operation.o001`, `scenario.operations.o001`, district D01 and the published identity rules. The planned map is `opmap.operations.old_quarter`; reuse an existing map ID only if the physical layout is identical, following the map identity contract.
- Preserve the mandatory sequence: three distinct courtyard scans → recover relay evidence → secured ground extraction with evidence and at least two original eligible infantry.
- Preserve Partial: at least two scans and two original infantry extracted, with no overriding mandatory failure. Conclude is available only when that predicate is true. Withdraw is a separate confirmation and consequence.
- Preserve evidence identity, recoverable carrier-loss behavior, `evidence.d01.relay`, `success.o001`, the RECON consequence vector, AP and exactly-once reward rules.
- Make the central decision real: split scouts to search faster, or keep infantry together for protection. Courtyard/street visibility, enemy pressure and travel routes must support both approaches.
- Wave A follows the first **individual completed scan**, not completion of all three. Wave B follows evidence pickup. Keep finite roster accounting and truthful warnings; do not force unavoidable casualties or make killing every enemy a new victory requirement.

### Tuning decisions to resolve before acceptance

The source brief targets 8–15 minutes but has a 720-second hard deadline. The shared initial scan specification is 15 seconds per site, and O001's evidence interaction is 15 seconds; the prototype uses 6 seconds for both. The review win took 39 simulated seconds.

Use the original interaction values as the initial integration baseline. Proposed first playtest target: **8–10 minutes with the existing 12-minute deadline**, with room for efficient play. This is a proposal, not an approved balance change or a timing-only acceptance rule. Record the final pacing decision in the owning brief/config before acceptance; if a shorter introductory mission is preferred, revise the brief explicitly. Do not stretch the experience using idle channels alone. Measure travel, scouting, decisions, combat, recovery and extraction separately at normal simulation speed.

Audit forces as well: the prototype authors five player infantry and three hostiles, while the shared FP_LIGHT/EP_CELL specifications describe 16 and 20 respectively. The integration content ledger must state the exact roster, initial/reserve split and any deliberately unused reserve or mission override. Neither a package name nor a compiler budget cap proves that the intended force was deployed. Resolve the discrepancy against the owning specification; do not silently inflate or reduce forces to manufacture a pass.

## Implementation sequence and dependencies

`R0 → R1 → R2 → R3 → R4 → R5 → R6`

These are completion gates, not calendar estimates. Start persistence design and map layout during R1/R2 so they constrain the real integration. Do not wait for all six districts or all twelve families. Each gate records commit, evidence and outstanding defects before advancing its status.

### R0 — Reconcile contracts and assign the shared integration work

Owner: integration lead with gameplay/persistence owners. Dependency: current source audit.

- Inventory existing Operations contracts and reducers; identify which can remain pure code, which require ECS adapters, and which prototype simulation code must remain test-only.
- Assign one integration owner for shared assembly references, map identity, launch dispatch, save envelope, UI gateway and localization. [P0_SHARED_SEAMS](P0_SHARED_SEAMS.md) is the checklist of historically untouched areas, not a reason to omit them from P4R.
- Record mode tags, session ownership, command path, tick ownership and transaction acknowledgment boundaries before implementation. Follow the existing [SOLID/ECS contract](../../Architecture/gameplay_solid_ecs_contract.md).
- Confirm the roster ledger, pacing proposal, objective/wave triggers and required role bindings. Keep unresolved design decisions explicit rather than embedding new rules in a presenter.

Exit: a concrete integration/file map, baseline Campaign/Skirmish validation selection, and reconciled mission content specification. A new document alone does not close R1–R6.

### R1 — Connect normal deployment to the shared world

Owner: integration/composition and UI owners. Dependency: R0.

- Add directional assembly references and narrow Operations ID validation as specified in ARCHITECTURE; preserve dependency rules and reject malformed identities.
- Build the O001 definition, graph and ScenarioSetup through Editor tooling. Bind the D01 map's real metadata, roles and anchors. Missing content must fail visibly; do not substitute a generic greybox or Campaign mission.
- Connect SCN-11 dashboard and SCN-12 district/briefing to typed Operations read models and commands. Show mission purpose, force, costs, prerequisites and consequences; unavailable entries must not launch prototype fallbacks.
- Route the immutable Operations payload through `MatchSceneView.OperationMapLaunch.cs` and the existing map readiness/scene-load path. Require a single active mode and session-owned spawns. Start mission time only after readiness and player control.
- Connect deployment reservation to the persistence boundary designed for R4. Early development may use an isolated profile, but its limited durability must be explicit until R4 passes.

Exit: from the normal main menu, a tester selects O001, reads its briefing, deploys into the shared Match world and sees the correct force/objectives. Cancel does not spend AP; repeated Deploy does not reserve twice; load failure safely returns with one rollback/refund. Existing Campaign and Skirmish launch smoke checks pass.

### R2 — Complete one mission through actual controls and shared gameplay

Owner: gameplay and UI owners. Dependency: R1 and a navigable D01 greybox.

- Reuse established unit selection/group selection, camera, movement/pathfinding, combat/damage, enemy AI and visibility. The standalone loop's queued Death attack is not the production combat implementation.
- Implement the Operations objective/fact/wave systems in their owning ECS boundaries. Consume real arrival, visibility, death, interaction and extraction facts; do not copy an independently advancing prototype world into the shipping world.
- Expose Scan, evidence interaction/recovery and Extract through the normal visible interaction controls. Validate eligibility, range, line of sight, interruption and cancellation; show progress and rejection reasons. Keep public search zones distinct from hidden hostile observations.
- Bind live objective markers, eligible-group focus, selected-unit feedback and pause/withdraw/Conclude. Fix duplicate-scan counting, carrier loss/recovery and minimum-survivor failures against real entities.
- Make terminal state immutable; stop further orders, waves and gameplay guidance after the result. Render committed result facts and implement a real Continue action, clean return and replay.

Exit: one normal-speed manual O001 victory from menu to dashboard, plus actual defeat, withdrawal and reachable Partial routes. No direct order API, health/position edits, fact injection, time acceleration or forced settlement in the claimed playthrough. Capture inputs and chronological world evidence. Regression fixtures may inject state, clearly labelled separately.

**This is the first manual-playable milestone.** Do not postpone it behind final art or additional Operations missions.

### R3 — Deliver the intended Old Quarter tactical experience

Owner: environment and gameplay/design owners. Dependency: R2.

- Develop the map into readable desert streets and three distinct courtyards using qualified project assets. Provide useful cover/sightline breaks, alternative approach lanes, truthful boundaries and a reachable ground exit. Decorative obstacles must agree with navigation and visibility.
- Bind finite initial guards, patrols and reinforcement routes to the real map. Correct wave A's per-site trigger and wave B's pickup trigger. Warn before arrival, stage outside occupied/visible secure space and clean up all pending spawns at terminal state.
- Tune spacing, patrol exposure, threat timing and extraction pressure so split-scout and grouped approaches both have a meaningful advantage and cost. A clean fast victory is legitimate; the ordinary intended route must nevertheless exercise the mission's risk/decision design. Do not make waves mandatory kills merely to prolong play.
- Capture normal-speed traces for both approaches, including hesitation/recovery, and reconcile final timings/force counts with the owning brief and content version. Re-run model and live objective checks after tuning.

Exit: reviewed world captures, role/route/nav/visibility checks, and two legitimate normal-input wins showing different exposure/timing/protection. The reviewer can explain why protecting scouts matters and what the evidence means for the district.

### R4 — Make saves, settlement and mode transitions durable

Owner: persistence with gameplay/composition owners. Dependency: R1/R2 integration; design starts earlier.

- Integrate the versioned Operations profile envelope and revision-checked serialized commit boundary. Preserve Campaign, Skirmish, settings and wallets; handle unknown/corrupt versions without destructive migration.
- Persist the deployment reservation, immutable launch snapshot, terminal result, settlement receipt and return acknowledgment. AP, district changes, evidence/milestones and account rewards commit together. Results only display acknowledged transactions.
- Audit existing `SkirmishCheckpointService`/codec/runtime coverage before reuse. Serialize the actual shared simulation state O001 needs: units/health/orders, visibility, evidence/carrier, objective/wave clocks and latches, RNG and stable identities. Do not treat replaying the prototype's order log as proof of restoring the real combat world.
- Publish checkpoint bytes before the profile reference, retain the last valid checkpoint, and restore stable IDs rather than ECS handles. Follow ARCHITECTURE's same-attempt restart, withdrawal and technical-failure rules without reward or AP exploits.
- Verify mission cleanup and transitions Campaign → Operations → Skirmish and back: no stale orders, actors, UI, rewards, audio, camera requests or wave ownership.

Exit: real disk round trips and controlled process-restart tests before/after reserve, during a scan, while carrying/dropping evidence, before/after result settlement and before return. Exact-once cost/reward behavior passes duplicate/stale/conflicting callbacks. Existing saves retain their other-mode progress.

### R5 — Reach the established HUD, localization and ARIA standard

Owner: UI/localization/ARIA with QA. Dependency: R2–R4.

- Use the established shell/HUD visual system, interactive hit targets, command feedback, minimap/camera controls, sound and guidance conventions. Keep the capture-only IMGUI shell outside the player route.
- Remove coach/progress overlaps; support actual phone safe areas and aspect ratios. Provide readable active/locked/completed objectives, carrier/extraction status, threats, refusal reasons, outcome and district deltas. Do not expose debug IDs in player copy.
- Integrate EN/FA into the shipping localization catalog, including RTL layout and numeric/timer legibility. Guidance must remain optional and must not block valid actions or narrate after terminal state.
- Connect Operations public observations and actions to the existing ARIA visible-input/Watch path. The planner may reuse objective reasoning, but must select/focus/issue actions through the same working controls as a player. No hidden-target reads, direct order injection or settlement calls. Human input interrupts and replans safely.
- Record a full manual EN and FA journey and the O001 ARIA coverage from ACCEPTANCE: Regular seeds 1102/2102/3102/4102/5102 with at least 4/5 legitimate wins including 1102; FA seed 9102; one win on each additionally exposed difficulty (Recruit 6102, Veteran 7102, Commander 8102). Unverified difficulties stay unavailable. Retain failures rather than selecting only successes.

Exit: normal-input EN/FA manual evidence, real-input ARIA matrix, interruption/resume evidence and layout/audio review on the same candidate. Existing host `AriaWon` fields remain historical harness evidence and do not satisfy this gate.

### R6 — Accept the candidate for players

Owner: QA/release with design owner. Dependency: R3–R5.

- Build a pinned candidate using the repository's platform wrappers; record commit, dirty inputs if any, content hashes, build settings and artifact checksum. Re-test after changes that affect accepted evidence.
- Validate real touch, safe areas, startup, full mission/repeated transitions, frame pacing, memory and thermal behavior on representative supported phones against the project's existing targets. Measure Operations itself; another mode's pass is not its device evidence.
- Observe 3–5 unfamiliar players without tactical coaching. Record whether they understand the objective, select/move/scan, interpret threats, recover evidence, find extraction and understand the district result. Log confusion, failure causes and engagement; fix blockers and repeat affected cases.
- Run affected shared architecture/save/input/combat/map checks plus the existing Campaign M1–M5 and accepted small-Skirmish journey regressions at a scope justified by shared changes. Preserve full logs and investigate new errors/leaks rather than treating a Victory image as a clean run.

Exit: all O001 required gates have passing evidence on the candidate, no unresolved progression/save/input/localization blockers, and gameplay/presentation review supports the original intent. Update catalog acceptance only now for the corresponding gates. Release O001 alone if necessary; do not mark O002/O003 accepted by inheritance.

## Integration/file ownership map

These are existing entry points or planned boundaries, not a claim that all proposed types exist. ARCHITECTURE owns final dependency and naming rules.

| Area | Existing entry points | Required responsibility |
|---|---|---|
| Contracts/model | `Scripts/Operations/{Contracts,Strategic,Tactical,Loop,Content}` | Reuse stable DTOs/reducers/tests; classify prototype-only simulation; avoid dual authoritative worlds |
| Launch/map identity | `Scripts/Composition/MatchSceneView.OperationMapLaunch.cs`, `Scripts/Configs/OperationMapIdentityRules.cs`, `ScenarioSetupConfig.cs` | Mode-tagged payload, validated identities, readiness and role ownership |
| Runtime | Existing `Scripts/Runtime`, `Scripts/Systems`, `Scripts/Components` owners | Operations ECS objectives/lifecycle/facts around shared RTS systems |
| UI | `Scripts/UI/Screens/OperationsDashboardScreenView.cs`, district views, `Scripts/UI/Shell/Ecs` and UI contracts | Bind real model/actions and root/history; no view-owned mission arithmetic |
| ARIA | `Scripts/UI/Shell/Ecs/AriaPlayInputSystem.cs`, established observation/Watch path | Operations active/terminal mode handling and actual visible input |
| Saves | `Scripts/Persistence/SaveDataModel.cs`, `SaveService.cs`, repository/commit boundary; Skirmish checkpoint code as a reference | Durable versioned profile/attempt and exact-once settlement; audit coverage before reuse |
| Content/builders | Planned `Configs/Operations/Missions/O001/`, Operations Editor builders and owning map package | Deterministic assets, localized keys, force/route/role binding and versioned content |

Paths above are relative to `Assets/Game/`. Do not add an O001-specific gameplay MonoBehaviour, static manager or new global registry. Shared edits are integral to this implementation package; coordinate ownership, preserve unrelated work and validate affected modes. This document does not itself perform or authorize unrelated implementation changes.

## Validation and evidence discipline

The existing host suites remain regression evidence: `Tools/Operations/check_p0.py` through `check_p4.py`, `check_aria_evidence.py`, `check_aria_playmode_capture.py` and `check_mobile_ready.py`. Run the affected suites; their names/pass markers do not establish human interaction or device readiness. Add meaningful live integration cases for the gates above; do not merely assert that source strings or button labels exist.

Proposed evidence suites are launch/input, objective/world integration, durable recovery/settlement, cross-mode cleanup, ARIA visible input and device/player review. These are work items, **not currently callable validation commands**. Publish entry points, required pass markers and failure behavior when implementing them.

Keep evidence under `Design/AgentReports/Operations/<candidate>/operation.o001/<difficulty>/<seed>/`. Record actual platform, code/config/content hashes, input path, language, profile setup, restart/intervention count, timestamps, objective/wave timings, losses/evidence state, transaction IDs, district before/after, logs and chronological captures. Fix the capture tool's hardcoded Windows platform stamp before reusing it for new cross-platform claims. A header string such as `ops-authored-o001-v1` is a content version, not a substitute for a computed integrity hash.

Follow current root AGENTS.md: RTK wraps repository Unity wrappers, Hub stays open/signed in, macOS uses GUI licensing with no direct executable or batchmode, and active Editors are not terminated to make room. Use the Windows Operations shadow where applicable. Respect actual project ownership and use an isolated permitted checkout when needed. A missing marker, timeout, lock or nonzero exit is a failure, regardless of a screenshot.

## Delivery tracking and scope control

| Gate | Status at plan creation | Evidence needed to close |
|---|---|---|
| R0 integration/content reconciliation | Planned | File/dependency ownership and final content decisions |
| R1 normal deployment | In progress: launch and failed-startup refund/redeploy smoke passed; cancel/input checks open | Menu/briefing/world input trace and rollback checks |
| R2 manual playable | In progress: shared objectives/HUD; full journey pending | Real-input complete journey and outcome coverage |
| R3 tactical experience | Initial authored candidate; review pending | Real map and two reviewed approaches at normal speed |
| R4 durable recovery | Transactions tested; active-world recovery pending | Disk/process recovery, exact-once settlement, cross-mode checks |
| R5 UI/EN/FA/ARIA | UI and translation draft; acceptance pending | Layout/manual journeys and real-input ARIA matrix |
| R6 release candidate | Planned | Candidate artifact, supported-device and unfamiliar-player review |

Update this table with commit/evidence links and residual issues as work lands. Update the owning brief/config/catalog together when content decisions change. Never close a gate solely because a similarly named P0–P4 host package passed.

O001 is the integration reference. Apply proven shared infrastructure to O002/O003 afterward, retaining their escort/repair-specific tests. Defer the remaining 57 missions, other district maps, new families, full city-run certification and unrelated Campaign/Skirmish expansion. They are still in the full Operations roadmap, but are not prerequisites for making this first mission work for a player.
