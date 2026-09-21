# Expansion acceptance and performance plan

Updated 2026-09-21: expanded catalog cases remain pending; small-scenario Editor victories are separate evidence. Targets below are proposed unless explicitly inherited from the repository performance contract. No new Editor gameplay, Android performance or player-comprehension evidence was gathered for this planning task. The [programming handoff](IMPLEMENTATION_HANDOFF.md), [individual briefs](Scenarios/README.md) and [work-package suites/evidence schema](AGENT_WORK_PACKAGES.md) make these gates executable assignments.

## Gameplay acceptance matrix

| ID | Exercise | Pass condition |
|---|---|---|
| X01 | Fresh setup, old saved setup, unavailable device size, replay/change setup | Only supported combinations deploy; clear migration; one session, correct preset and unchanged campaign progress. |
| X02 | Every roster entry: queue, arrive, select, order, fight/use ability, die | Correct role, producer, cost, quantity, target domain, EN/FA identity, visual feedback and final disposition. |
| X03 | Simultaneous queues at several producers and nearly full capacity | Alive + queued + in-transit reservation respects all limits; no overbooking, missing refunds or double release. |
| X04 | Cancel, producer death, blocked delivery, destroyed carrier, no landing space | Explicit recoverable outcome with correct units/resources and visible reason. No permanent queue or camera lock. |
| X05 | 24 infantry squads plus vehicles: group, split, page, pin, reinforce | Stable IDs/cards; useful group selection in one tap; no need to drag-select overlapping individual soldiers. |
| X06 | Army movement through a junction, gate, narrow exit and rubble | Formation adapts; compatible routes; orders remain interruptible; blocked units recover or explain failure. |
| X07 | New Move/Hold/Attack/Retreat during attack approach or delivery | New command replaces old intent; acknowledgments and world outcome agree; no delayed old-order resurrection. |
| X08 | Build on mountain, plateau, slope edge, water, road, sidewalk and valid ground; rotate, confirm | Whole footprint/exit rules agree between preview and spawn; ordinary buildings reject prohibited terrain; no silent relocation. |
| X09 | Gate placement and destruction; structure/vehicle destruction | Correct opening/carriageway placement; immediate marker and target cleanup; authored wreck state and explicit reuse policy. |
| X10 | Supply raid, fuel shortage, rebuild, exchange and producer expansion | Economy matches displayed balances and rates; shortage/recovery understood; planned force achievable within match duration. |
| X11 | Equal-spend rifle, fire-support, anti-armor, armor and mixed compositions | Each role has a useful context and a practical counter; pure one-type spam does not dominate every tested strategy. |
| X12 | AI facing rush, defense, armor concentration, flank and supply raid | Observed affordable counter-production, reserves, route changes and replacement. No unexplained spawns or hidden bonuses. |
| X13 | Hidden enemy, last-seen contact, scan, radar, aircraft and AI reaction | All UI/targeting/AI obey one information model; no live markers or precision attacks leaking unseen entities. |
| X14 | Helicopter/jet sortie, refuel, transport overlap, anti-air interception | Real entry/attack/return behavior; valid counters; usable group boarding; clear fuel and target restrictions. |
| X15 | Capture/contest/abandon zones, ticket zero, timeout, simultaneous base death | Exact documented capture and outcome rules; clear progress at every waiting state; one terminal result. |
| X16 | Full EN/FA matches: win, defeat, timeout and surrender | Understandable objective, no mixed-language voice, no glyph/overlap errors; audio and simulation stop correctly at result. |
| X17 | Ten restart/replay/setup cycles; background/audio focus; process interruption | No entity, queue, resource, selection or pause leakage; save behavior matches the supported recovery policy. |
| X18 | Shared-system changes followed by affected M1–M5 journeys | Accepted campaign orders, placement, transport, comic language, ARIA and result behavior preserved. |
| X19 | Standard/War/Large War worst-case performance scenarios | Each exposed device/preset combination passes its separately recorded performance and interaction gate. |
| X20 | Unfamiliar-player sessions without coaching | Players can recruit a counter, command two fronts and explain objective progress; serious repeated confusion reopens the gate. |
| X21 | Readiness stages and facility prerequisites, including unavailable cards and shortcuts | Exactly the supported units unlock; requirements are clear in EN/FA; anti-armor and anti-air are obtainable before the relevant threat. |
| X22 | Facility/category upgrade purchase, timer, completion, cancellation and facility loss | Correct Materials accounting and documented recovery/refund behavior; effects apply once to existing and future eligible units; player/AI parity. |
| X23 | New match/Replay versus checkpoint resume; Normal/Established Base/Full Arsenal setup | New battle resets to selected starting conditions; same-battle recovery preserves upgrades/progress without duplication; only supported options deploy. |

For X11/X12, use at least three seeds and mirrored starting conditions per strategy: rush, defensive expansion, mixed frontal attack, flank/transport, air transition. Difficulty and win-rate targets need human baselines; do not invent a precise win-rate conclusion from a few scripted matches. Preserve losses and inconclusive games in the report.

For X20, start with at least five unfamiliar players across the supported languages. Proposed thresholds: four of five identify the win condition and issue a useful first order within 30 seconds, recruit a useful unit within 60 seconds, and select/reposition two groups without coaching. Report individual failures and revise UI; a tiny sample is directional evidence, not statistical proof of broad usability. Phone touch sessions are required.

## Performance ladder

| Scenario | Count / setup | What it must expose |
|---|---|---|
| P0 | Existing approximately 50-combatant cap, exact current preset | Regression baseline; loading, idle, command, production and result. |
| P1 | 100 real combat units, mixed infantry/vehicles | Mass order latency, two routes, dense engagement, UI selection and reinforcement. |
| P2 | 200 then full War cap, including ground and air | Several simultaneous fronts, counters, target acquisition, supply/delivery and all-units-visible rendering. |
| P3 | 350 combat units, complete mixed roster | Large War headroom, 30-minute thermal stability, wrecks, audio/VFX pressure and peak memory. |
| P4 | 500 combat units plus bounded support | Engineering saturation test: identify bottleneck, queue growth and safe limits. Does not authorize exposing this size. |

Each case includes idle, mass move, concentrated battle, supply traffic, simultaneous arrivals, rapid camera pan/zoom, large selection, destruction and result cleanup. Include all units onscreen as well as distributed fighting. Aircraft, helicopters and visual variants are represented; substituting every unit with a cheap infantry mesh is insufficient.

Run warmup and measured phases separately. Pin exact source and asset hashes, seed, requested/actual live counts, device/OS, quality, resolution, build type and temperature context. Use the [performance contract](../../Architecture/performance_regression_contract.md) and [tracked budgets](../../Architecture/performance_regression_accepted_baseline.json). Editor measurements identify regressions; physical development builds diagnose and release builds certify.

Inherited gates include Android p95 frame time **less than 33 ms** on baseline/recommended and **less than 25 ms** on high-end. These do not establish 60 FPS; a 60 FPS preset needs its own sustained evidence. The older general technical-target document differs in places; the tracked contract/config takes precedence. Do not replace unknown memory limits with invented figures or use the small-mode Editor frame budget as phone evidence.

Additional proposed expansion gates:

- Normal-speed combat p99 frame time below 50 ms after warmup, with no repeated 100 ms-plus stalls. Record all maxima and explain any exclusions. Device-tier approval is needed before treating these proposals as release budgets.
- Visible input acknowledgment within 100 ms p95; accepted group movement starts within 300 ms p95 and 750 ms p99 when a route is free. Separately record last-member start, route queue age and arrival spread. A congested route must be visibly identified; it is not a reason to conceal queue latency.
- No accumulating path backlog during a sustained battle. Report requests, completions, retries, cancellations, oldest age and formation recovery; no starvation of recent player orders.
- No recurring new managed allocation in the modified steady-state systems after warmup. Keep the existing scenario GC limits and measure spawn/destruction bursts separately.
- No monotonic memory/entity/marker growth over ten replays; peak/resident and graphics memory recorded. Apply existing accepted device/package limits and retain unknown limits as measurement tasks.
- Match simulation time tracks real elapsed time at selected speed; no performance recovery by secretly slowing the clock or skipping offscreen combat.
- Full proposed match duration under thermal load. Quality changes can reduce shadows/VFX/detail, but keep gameplay, selection, hit detection and animation transitions correct. No aircraft blue/black flicker during camera movement.

If War fails a device gate, retain the certified Standard preset on that device, identify the measured bottleneck and iterate. Do not silently weaken the gate, hide units, or declare an Editor run mobile acceptance.

## UI and session evidence

Capture 16:9 and 20:9, 1280×720 and 2400×1080 reference layouts, plus the target phone's real safe area and touch input. Inherit the 80-pixel minimum reference touch target from [technical targets](../../AAA_Mobile_Technical_Targets.md); judge physical readability as well. Required states: dense selected army, Army panel paging, short/long Farsi descriptions, unavailable production, queue full, no fuel, contested zone, timed progress, arrival notification, result and back navigation.

For each new mechanic record a real input → acceptance/rejection → world outcome → UI cleanup sequence. Reflection-invoked commands and injected stress armies are valid technical tests only when labeled. An occluded button leaves the UI test open. Keep original profile and Editor state restored after isolated QA.

## Catalog, scenario and planner gates

- **X24 Catalog integrity:** 120 unique stable IDs, exactly 24 per map and ten per objective/army pairing; start/difficulty/size/seed are separately versioned. Four difficulties and randomized seeds never inflate the count. All 120 CSV entries begin Planned and advance individually.
- **X25 Start package accounting:** expand every scenario at every certified size into exact per-side infantry/platform/support/facility counts, balances and capacities. Assert Supply/category/support limits including CE trucks, BT designated soldiers, loaded vehicle fuel and objective garrisons. Every starting unit has compatible production/replacement rules and legal terrain. Starting resource/scenario asymmetry matches the briefing.
- **X26 Map completeness:** five maps × four objective anchor sets; both convoy routes and breakthrough corridors actually traversable. Inspect camera rotations/zoom, shadow seams, floating terrain, slopes and wrecks. Surface data and visible geometry agree. Correct CC's known raised-shelf defect before accepting that map.
- **X27 Scenario distinction:** each entry changes meaningful decisions, has two viable strategies and usable counters. Reject clone/filler combinations and replace them honestly. Roster ledger covers every supported role in at least two map/objective contexts.
- **X28 Difficulty legality:** all levels use identical stats/costs/visibility/start packages; differences trace to documented decision/coordination parameters. Verify actual reaction/scouting/retreat behavior under observed threats; no hidden free enemy production.
- **X29 Shared planners:** new content needs data and reusable objective policies, not scenario-ID branches. Run held-out routes/seeds; confirm ARIA observations exclude hidden knowledge and actuator calls are only real touch inputs. Enemy AI obeys its own legal faction knowledge.
- **X30 ARIA catalog evidence:** the three-seed × two-locale per-entry baseline and 20-seed stratified stress plan in AI_AND_ARIA.md are required. Record all losses and at least two wins per locale per entry in the minimum sample. This is finite evidence, not guaranteed winning or a statistical universal success rate. High-difficulty losses alone do not justify cheats or hidden tuning.
- **X31 Briefing/library:** filters, all five collections, clear force/resource/asymmetry preview, exact objective failure rules, side roles, device size gating, back navigation, custom-label/result identity and saved selection work in EN/FA on real phones.
- **X32 Four objective results:** exact FC ticket/capture transitions; BT corridor reset, boarding/disembarkation and eight-of-twelve accounting; CE truck role identity, routing, damage/repair, five-second delivery and impossible-win detection. Simultaneous events/deadline evaluated once; campaign states untouched.
- **X33 Economy math:** simulate paid growth/replenishment and verify real-world deliveries at all size/start pairs. Test conversion storage-full behavior, consumed inputs, queue cancellation, research destruction/refund and fuel shortage. Meet useful first-counter/vehicle/air timings without resource pulses or preloaded hidden advantages.

## Implementation handoff gates

- **X34 Stable identity and setup compiler:** preserve S001–S120, prototype mappings S001/S025/S073 → 0/1/3 and Editor stress index 2. Verify the 117 remaining work ordinals 4–120 without assigning them runtime/save meaning. Compile all 360 scenario/size vectors from [INITIAL_SETUP_MATRIX](INITIAL_SETUP_MATRIX.csv), including BT player-only substitution, CE objective support, queue counts, defender towers, capacities and actual loaded fuel endowment. Original CSV column order and old content identities remain compatible.
- **X35 Class and state ownership:** implement the shared types in TECHNICAL_ARCHITECTURE with correct assembly direction, one writer per state and one terminal arbiter. Every scenario is definition/setup/layout data, not a separate rules controller or S-ID ARIA branch. Failure to load a definition cannot default to Desert Base.
- **X36 Full checkpoint and result conservation:** round-trip objective fractions/timers/tombstones, receipts, queues, cargo, fuel, visibility, groups, upgrades and RNG state into a fresh session. Test atomic write interruption, incompatible version and repeated result callbacks. Replay resets the exact package and does not duplicate resources/completion. Legacy wins never count as expanded wins.
- **X37 Role and producer certification:** every enabled role has an actual prefab/config, functional producer, correct target domain and full lifecycle evidence. The 74-row source inventory is not proof. Ground Staging, drone pad, support queues, aircraft return/runway and paid service-pad repair must exist before dependent entries publish. Retain supported legacy Campaign behavior.
- **X38 ARIA must play and win every entry:** complete X30's three Regular seeds in EN and FA with at least two wins per locale, plus at least one normal-speed ARIA win on each additional exposed difficulty and an EN/FA win at each additional exposed size. Packet seeds are reproducible inputs. Keep unsuccessful attempts; require no input violations, extra resources, hidden observations, forced outcomes or human tactical intervention in counted wins. Draw/launch/asset validation is not Victory. This is required per S-ID, including expanded versions of the three prototypes.
- **X39 Publication and evidence:** each entry has a manual win, required ARIA matrix, objective loss/edge fixtures, two viable approaches, recovery/localization/device evidence and matching code/config hashes under its own report path. Set only certified size/difficulty combinations Playable. A shared-system pass cannot bulk-approve 120 entries; unfinished profile capabilities cannot be relabeled full acceptance.
- **X40 Planning integrity:** the documentation generator `--check` passes for 120 manifest entries, 117 remaining work items, twenty packets, 360 setup rows and 74 inventoried source configs. All brief links and S-ID anchors resolve. This gate validates handoff consistency only; all runtime, ARIA and device gates above remain pending until executed.

Before calling the full catalog ready, all required content entries pass normal-input full matches and separate device/readability gates. The existing 104729-seed wins certify only their recorded small versions. Scenario readiness records include map/content hashes, setup/difficulty/size, seed, locale, platform/device, human/ARIA/control method, outcome, command trace and open defects. A schema-valid CSV is planning evidence only.

## External technical references

- [Unity Profiler documentation](https://docs.unity.com/en-us/engine/6000.0/manual/analysis/profiler) supports using device captures and custom profiling markers. Project-specific capacity still requires measurement.
- [Android power-efficiency guidance](https://developer.android.com/games/optimize/power) explains thermal throttling and adapting workload. It supports sustained-session testing, not a universal safe unit cap.
- [Android Dynamic Performance Framework](https://developer.android.com/games/optimize/adpf) describes thermal/CPU feedback for sustainable performance. Adopt only the integration relevant to supported devices after profiling.
