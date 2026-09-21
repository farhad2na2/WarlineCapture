# Four Skirmish objective implementations

2026-09-21 proposed technical contracts. These implement the established rules in [BATTLE_CATALOG](BATTLE_CATALOG.md); they are not four new mission engines. Each of the 20 [map/objective packets](Scenarios/README.md) binds these systems to six army/start combinations.

## Common session and terminal rules

Start the clock only after map/grid/catalog readiness, complete validated starting spawn, and player briefing confirmation. Use simulation ticks with a persisted fractional accumulator, not wall-clock UI timers. Pause freezes objectives, research, AI decision timers, movement and the deadline consistently. Standard deadlines: BA/BT/CE 1080 s; FC 1200 s. War 1500 s, Large War 1800 s for all four objectives.

Each tick: resolve shared damage/death and passenger state → collect all objective facts → advance eligible dwell/capture/tickets → evaluate terminal events → evaluate deadline if still nonterminal → freeze result. Destruction on the delivery/evacuation tick precedes credit. Surrender is accepted at the input boundary before the next simulation tick: if Playing, immediately concede and stop ARIA; if the previous tick already froze a result, reject it without changing that result. For BA/FC opposing wins on the same tick are Draw, regardless of iteration order. BT/CE use their player-success/defender-denial predicate; normal valid facts cannot both deliver the required survivors and make that same threshold impossible.

One `SkirmishOutcomeSystem` owns terminal results and simulation freeze. State/reason enums append explicit new values rather than repurposing serialized old ones: `TicketsDepleted`, `TicketsAtDeadline`, `BreakthroughEvacuated`, `BreakthroughImpossible`, `ConvoyDelivered`, `ConvoyImpossible`, `ObjectiveDeadline`; preserve legacy values. Startup/content/checkpoint failure is a technical state, not a combat defeat or a paid reward event.

All systems consume stable subject IDs and typed components. Missing role bindings fail startup. No `Find("Truck")`, hardcoded map coordinates, player/enemy name matching or ordinal-based rule dispatch. Use `FactionIdentity` for neutral/player/hostile ownership. Objectives may use actual world occupancy while exposing only public zone progress; they never reveal hidden unit identity/transform to ARIA.

## BA — Base Assault

**Owners:** `SkirmishBaseAssaultObjectiveSystem`, shared fact projection, outcome arbiter.

Inputs: original designated player/enemy main-base stable IDs, live health/destruction facts and clock. The main Barracks is assigned by authored role `base.player` / `base.enemy` at startup, not the latest or first produced Barracks. Extra producers are not extra lives.

| World state | Result |
|---|---|
| Enemy main base dead, player alive | Victory / MainBaseDestroyed |
| Player dead, enemy alive | Defeat / MainBaseDestroyed |
| Both dead on current tick | Draw / BothBasesDestroyed |
| Both alive at deadline | Draw / TimeLimit |
| Either army wiped but its base alive | Continue; normal production/recovery remains possible |

UI: both base health/status, deadline, public focus actions and correct hidden-target behavior. A known base marker is not a live hidden-health feed; use last observed health outside vision and explain its age. The result uses authoritative base facts while the planner only knows visible/last-seen facts.

Enemy policy: reserve roughly 25% of combat Supply when home is safe, send a main group on one observed viable approach, add a smaller flank when an affordable reserve remains, rebuild production/logistics if strategically viable. ARIA chooses direct pressure versus supply/route control from current public state, buys counters, and verifies orders/delivery. Percentages are initial planner weights, not invulnerable scripted troops.

**Mandatory fixtures:** original base vs replacement/extra Barracks, both deaths in opposite event ordering, base death at deadline, paused deadline, surrender, full-army wipe with working producer, result callback replay, unseen enemy health, map exit obstruction. Normal wins must involve actual damage through normal commands.

## FC — Frontline Control

**Owners:** `SkirmishFrontlineObjectiveSystem`; `SkirmishCaptureZoneComponent[3]`, `SkirmishFactionTicketsComponent[2]`.

Inputs: three typed ground capture footprints, initial Neutral owner, eligible dismounted living combat infantry counts by faction, public zone IDs, 500 tickets per side. Use an authored footprint on reachable ground (initial design radius 16 m, adjusted in map data to avoid counting across walls/cliffs). Pilots/civilians/embarked passengers/vehicles/aircraft do not capture. More infantry does not accelerate the timer.

Capture algorithm per zone:

1. Both factions have eligible infantry: Contested, progress freezes, empty grace resets. Ownership remains until neutralization completes.
2. Only owner infantry present: defend; discard opposing partial neutralization once friendly control is uncontested. No bonus ticket/income grant.
3. Only non-owner infantry present: accumulate eight eligible seconds to neutralize an owned hostile zone, then eight more to capture it. From Neutral, one eight-second capture. Contest pauses this accumulated work; empty grace/decay follows step 4. A change of capturing faction discards the previous faction's partial progress.
4. No eligible infantry: retain owner, retain partial progress for ten empty seconds, then decay partial progress at one second per elapsed second until zero. Save grace and phase. Continuous contest freezes progress, not ticket drain.
5. Determine majority by **owned zones after this tick's transitions**. Contested owned zones still count for their current owner. At least two owned zones drains opponent tickets by one/second; three zones still drains one/second. Accumulate fractions precisely across frames/checkpoint, clamp at zero, and apply both factions' events before terminal evaluation.

Enemy tickets zero or main base destroyed is player win; reciprocal is loss; opposing terminal facts in one tick Draw. At deadline greater tickets wins, equal Draw, unless a terminal fact already resolved. Capture never grants Materials/Oil/Fuel. A factory/Barracks replacement does not replace the designated base objective.

UI: three owner/ring/progress/contested states, both ticket counts, drain arrow, deadline and focus for each point. Queue/ticket audio is rate-limited. ARIA/public objective model includes which zones are owned and losing tickets; no hidden defenders are disclosed.

Enemy policy: maintain two sustainable zones and a mobile reserve; choose cheaper reachable recovery over sending everything to one distant point. ARIA must split/select groups through the Army panel, send dismounted infantry, protect supply and stop abandoning a near-complete capture for an irrelevant target. Air can move/support infantry but never substitutes for capture.

**Mandatory fixtures:** 8+8 vs 8 neutral capture, exact ten-second empty grace, enemy arrival on final tick, mixed factions, vehicle/passenger exclusion, capture ownership update and ticket zero same tick, both main bases dead, one base dead while reciprocal tickets zero, higher/equal tickets at deadline, save at 7.9 seconds/carry ticket remainder, zone no-build protection, 60+ squads across three public points.

## BT — Breakthrough

**Owners:** `SkirmishBreakthroughObjectiveSystem`, shared transport facts and group controls.

At compilation select the first three player infantry squads as **12 designated rifle soldiers**, preserving total infantry/Supply. Standard Field changes its 2R+K to 3R; enemy retains the original 2R+K. In larger/Established packages the first three are already rifles. The rest of the force is ordinary support. Save each of the 12 stable IDs; reinforcements cannot replace lost designated soldiers.

Corridor A/B are alternative ground control footprints. Any living dismounted player combat infantry may hold one; enemy infantry presence or no friendly infantry resets its continuous 20-second hold. Vehicles cannot hold/contest. Once either reaches 20 seconds, `ExitOpen=true` **permanently for this attempt**; losing the point later does not close the exit. Destroying a gate/watchtower is neither necessary nor sufficient for the timer unless physical traversal requires its removal.

When exit is open, a designated living **dismounted** soldier inside the exit footprint gets an individual three-second uninterrupted dwell. Leaving, boarding, or death resets/invalidates that dwell. At completion mark the stable ID Evacuated, remove it from battle command/targeting, and release capacity without adding a death. Evacuated tombstones remain in the save/result. A transported soldier has to unload at a legal point before credit; flying over exit grants nothing.

Let `E=evacuated`, `A=alive not evacuated` among the original twelve. `E>=8` wins. `E+A<8` loses immediately. Otherwise deadline loses. Main-base destruction disables its real facilities but does not end BT; the defender must still stop the evacuation. Holding both corridors grants no extra resource or lower evacuation requirement. Defender's extra two towers are disclosed and stay outside unavoidable spawn fire.

UI: 12 marked identities grouped into three squads, `evacuated / 8 required`, remaining alive, each corridor 0–20 s, latched exit state, focus controls and hard deadline. Group cards retain designation after boarding/splitting. Clear warning when another casualty would make victory impossible.

Enemy policy: split initial garrison 40/40/20 between corridors A/B/mobile reserve, by whole squads; vehicle/AA allocations follow map packet compatibility. Keep reserve free, observe which route player actually uses, intercept at reachable ground without omniscient aircraft/passenger data. Additional troops are paid normal production. ARIA scouts both routes, protects the designated squads, uses other infantry to open a corridor when useful, and explicitly unloads/evacuates enough IDs. No one required hero soldier.

**Mandatory fixtures:** 12 exactly designated/no extra infantry; fifth designated death fails, four deaths still winnable; duplicate exit events; transported exit denied; carrier death passenger outcome counted once; third-second death denied; corridor contest reset; latched exit after losing corridor; alive base not required; new rifle recruit not designated; evacuated units never reappear after checkpoint; same packet works on both ground corridors and a legal transport approach.

## CE — Convoy Escort

**Owners:** `SkirmishConvoyObjectiveSystem`, normal movement/selection/damage and proposed shared vehicle repair.

Add three player objective trucks with stable roles `convoy.1/.2/.3`, outside combat/Supply totals and inside a separate +3 objective-support allowance. They use a mode-owned validated Cargo Truck definition: no weapon, no logistics job, no Fuel consumption, no storage-transfer output, no sale, no passenger capacity, no replacement/recruitment. They remain selectable and movable through normal controls; no autonomous departure before an order. Initial stock and ordinary logistics trucks are separate.

At least two live trucks reach destination and dwell five uninterrupted seconds. Moving outside resets dwell. Normal enemy damage applies during dwell; death that tick prevents delivery. On completion mark Delivered, remove from active combat/commands and retain a tombstone, without loss or a resource grant. `delivered>=2` wins. `delivered+live<2` loses; equivalently two original trucks destroyed. Deadline with fewer than two delivered loses. Main-base destruction disables production but does not override the convoy result.

Two truck-compatible routes share the origin/destination but have different middle segments and safe holding pockets. A player can mix routes or change an ordinary movement order at any time; no forced route-lock/teleport. Optional route buttons only issue waypoint movement through shared controls. Recompute on changed blockers; never secretly delete a wreck. Starting trucks use staggered parking bays so opening a briefing cannot trap them at a single exit. Defender gets two disclosed towers plus its own normal roster; no timed free enemy waves.

Repair specification when the audited runtime lacks a real repair command: implement `VehicleFieldRepairSystem` plus `VehicleRepairRequestComponent/VehicleRepairStateComponent` in the shared Runtime/Components boundaries. An eligible friendly ground vehicle/truck must be stationary within 12 m ground-plane distance of its friendly `service_pad` center, with no **legally visible** hostile combatant within 20 m of the vehicle. The gate must not reveal concealed enemies through a refusal. Repair up to 25% maximum health for 20 Materials over 20 simulation seconds; prorate cost/duration for smaller missing health (ceil cost, at least one tick). Reserve cost once; store the accepted repair fraction, paid amount and remaining ticks. Apply the accepted fraction of current maximum health only at completion, clamped to missing health; no Fuel grant. New movement, incoming damage, loss of the service pad or a visible hostile entering the radius cancels; cancellation before work refunds 100%, afterward 75% rounded down; vehicle destruction loses cost. Pause freezes work. A health upgrade preserves current percentage; the stored repair fraction avoids a free extra heal or duplicate charge. UI shows progress/refusal and enemy AI/ARIA use the same legal action. This is proposed work; the existing Materials `Repair` spend enum alone is not that mechanic.

Enemy policy: initial whole-squad patrol groups cover route A/B (40/40%) with 20% mobile reserve, regroup and intercept only observed convoy movement, buy affordable counters. ARIA moves a scout/escort ahead, uses Go/Move/Hold for trucks, protects the second surviving truck, and uses service-pad repair where beneficial. Do not demand armor in A profile or offensive air in G.

**Mandatory fixtures:** first/second/third delivered counts, two deaths before departure, one delivered plus one live plus one dead, five-second death/exit reset, original truck identity vs logistics replacement, sell/board commands rejected at shared boundary, Fuel exempt only for objective trucks, no capture eligibility, blocked route and alternate route, repair interruption/refund, save during 4.9-second dwell, result after base loss, HUD selects each truck through overlap.

## Planner implementation and shared policies

Extend existing `AriaSkirmishPlanSystem` and its public observation rather than adding mission-specific scripts. Proposed reusable plain helpers use the approved suffix, for example `SkirmishObjectivePolicyUtilitySystemHelper` and `SkirmishRoleCounterUtilitySystemHelper`. A switch on **four objective kinds** is legitimate; branches on S-ID, map scene name, or a memorized winning coordinate are not.

Public observation fields: objective kind/version and progress; own roster/groups/queues/stock/research; legal visible or explicitly last-known contacts; visible route/zone markers and inspected map topology; current selection, control rectangles, camera/transition state and action refusal reasons. Expand the current fixed five-squad model to indexed/paged public army read models with stable group IDs. Keep omniscient diagnostics separate from planner input.

Initial policy evaluation every 4 simulation seconds plus significant public event, staggered; critical base/convoy/evacuation danger can invalidate a plan immediately. Candidate scoring (initial integer weights): objective gain 40, prevent terminal loss 100, preserve mandatory BT/CE subject 80, observed counter advantage 20, travel-time cost -1/second (cap -30), known exposure penalty up to -30, unaffordable/illegal action excluded. Keep the current valid assignment unless an alternative exceeds it by 15 points or terminal risk changes. This bounds oscillation; tune in logs without changing unit stats. Supply planning reserves enough Materials for one cheapest relevant counter and one logistics replacement when not in immediate terminal danger.

Execution skills: observe → focus/camera if needed → select through actual controls → issue one normal action → verify visible acceptance and world/progress consequence → complete/replan. Maximum three retries of the same rejected intention without new evidence, then choose another legal action or explicitly hand back; never press forever. Preserve visible cyan-hand feedback, touch pacing, Stop/physical takeover and no automatic next battle from AI_AND_ARIA. Enemy AI uses the same objective/counter semantics with its own legal observation and regular command boundary; it does not use ARIA's touch actuator.

Difficulty windows, scouting cadence and opening restraint stay in MATCH_SETUP. No difficulty modifies damage/health/income. Every battle packet requires an actual ARIA Victory; Draw in BA/FC does not count. Use the existing three-seed × two-language Regular acceptance and the additional per-exposed-difficulty win gate documented in ACCEPTANCE. Do not infer 120 wins from one shared policy test.
