# Skirmish enemy AI design and implementation plan

Author: Menu UI handoff agent. Intended implementer: **Verify skirmish mission maps**, the S005 owner on the main checkout. Audit date: 2026-10-11.

Improve the existing expanded Skirmish opponent into a coordinated, observable and fair commander. Start with the reusable Base Assault behavior exercised by S005. Keep the same maps, forces, economy, combat rules and victory conditions. The highest-value changes are meaningful group assignments, local defense, repeatable paid reinforcement, scouting and bounded decisions with real hysteresis.

**Implementation status: authorized by the owner on 2026-10-11.** The owner explicitly released the prior change hold in the requesting chat: “yes release the hold with the agent plan”. Implement this plan in the S005 owner chat, **Verify skirmish mission maps**, coordinating changes with its active validation runs. The earlier [audit response](ENEMY_AI_AUDIT_INPUT_20261011.md) preserves the historical hold; that hold is now superseded by this direct owner authorization. No further permission is needed to begin the planned work. The audit itself changed no runtime, test, configuration or scene files.

## Evidence and limits

The audited checkout was `main` at `c88c79c5453532a3dca9c9c2fd0c9d37603daed7`. The six-run baseline identifies complete source fingerprint `d5afc88f225920451037f6e1c9f7e9d83dfc567e3bbdadfb5b6d65d9ee6774f4`. The adjacent [audit manifest](ENEMY_AI_AUDIT_MANIFEST_20261011.json) pins the files read, their hashes and the baseline proof summaries. The implementation agent is working concurrently; recheck affected files before editing.

The [final baseline matrix](native-final-d5af-normal-aria-matrix.json) contains three seeds in English and Persian. All six matches ended in player Victory at 232.394–252.282 simulation seconds, player base 800 HP and enemy base 0 HP. All sampled positive player-base health values in the six retained traces were 800. This proves those completion journeys and motivates a pressure audit. It does not prove a specific AI defect, that the player took no unit losses, or a universal difficulty/win rate.

The existing telemetry mostly reports player/base state; it cannot attribute the weak pressure to idle groups, route failure, spending, target selection or effective player defense. Instrument those causes before tuning. Native1185's missing viewport marker does not by itself establish fog invisibility. Native1182's rejected opening-order assertion does not establish stale enemy state. Preserve these validator failures separately; do not change enemy tactics to make a faulty driver pass.

The implementation agent reports no enemy AI fixes underway. Its active work is native input validation, observers and mission capability evidence. The design below combines that response with a static audit; new native AI behavior and performance have not been validated.

## Governing design

Use the existing [AI and ARIA contract](../../../Roadmap/Skirmish_Expansion/AI_AND_ARIA.md), [objective policies](../../../Roadmap/Skirmish_Expansion/OBJECTIVE_IMPLEMENTATION.md), [match setup rules](../../../Roadmap/Skirmish_Expansion/MATCH_SETUP.md) and [technical architecture](../../../Roadmap/Skirmish_Expansion/TECHNICAL_ARCHITECTURE.md). Preserve the [Campaign map reuse policy](../../../MapVariants/CAMPAIGN_MAP_REUSE_POLICY.md). This plan makes those existing requirements implementable; it does not authorize new maps or silently publish new configurations.

- Enemy decisions use faction 2's legal observations, frozen last-seen records, own forces/economy and public objective geometry. Unknown enemy cash, queues, moving units and future orders remain unknown.
- Enemy actions use normal faction-aware command, production, research, construction and logistics services. ARIA continues using its own public observations and visible input actuator. Do not give either planner the other's privileged state or disable attacks while ARIA plays.
- Preserve S005's current Field/R1, Standard/Regular package: original 450 Materials, fixed prices/caps/production and 1080-second deadline. Preserve all other authored packages and designated-base identity. No damage, health, firing-rate, fuel, resource or time bonuses.
- Improve reusable behavior for expanded Base Assaults, with S005 as the pilot. Keep legacy sessions isolated. Do not add switches on scenario number, memorized winning coordinates or special behavior for a validation seed.
- New FC, BT and CE policies, unexposed sizes/difficulties and future Support remain separately gated. Add reusable extension points where necessary, but do not implement or claim 120 complete opponents in this change.
- No HUD redesign is required. Use existing controls and player-readable world behavior. Developer diagnostics remain outside shipping guidance.

## Confirmed source gaps

Line references below describe the audited bytes and may move during implementation. Paths are repository-relative under `Assets/Game/Scripts/` unless stated otherwise.

| ID | Priority | Finding and evidence | Consequence and required change |
|---|---|---|---|
| AI01 | High | `Runtime/Skirmish/SkirmishEnemyStrategySystem.cs:29–68` evaluates on every Playing simulation update. No strategic timer or reaction timer is consulted. | Bound strategic work and command submission; distinguish decision cadence, deliberate response delay and responsive unit combat. Repeated identical commands currently still cross group/member service boundaries even though movement has a same-destination optimization. Do not claim measured frame cost until profiled. |
| AI02 | High | `Configs/Skirmish/SkirmishDifficultyConfig.cs:20–24` exposes reaction/scout/opening values, but repository search finds no consumers of these properties outside that class. Resolved setup stores the difficulty ID, not those settings. | Compile and version a complete AI policy snapshot. Enforce existing opening/reaction/scouting rules. Merely selecting a difficulty ID does not currently establish differentiated enemy decisions. |
| AI03 | High | `SkirmishEnemyStrategySystem.cs:164–195` turns every later RecruitCounter decision into AttackBase after one successful queue using `CounterCommitted`. | Replace the permanent latch with per-intent idempotency, pending production and live/in-transit capability accounting. Permit replacement and a different counter later without restoring the old purchase-every-frame bug. |
| AI04 | High | `SkirmishEnemyStrategySystem.cs:197–220,348–368` commands one first tank or first structure-capable group. The reserve test is a scalar supply threshold, not an actual allocation. | Allocate assault, escort, home reserve, scout and recovery duties to eligible whole groups. Rifles should support the attack against valid mobile targets, not be ordered to damage an immune building or remain tactically unused by the strategic planner. |
| AI05 | High | `SkirmishStrategyScoring.cs:52–60` calls home threatened from global visible hostile counts and own total counts, without home proximity, incoming damage or local capability. `SkirmishEnemyStrategySystem.cs:222–230` implements DefendHome/Reserve/Hold as Hold on the first rifle group. | Detect local danger to own assets, send enough compatible defenders to a reachable interception/rally point, maintain a reserve and return to prior objectives when danger clears. Keep the existing legal visible-infrastructure intrusion response. |
| AI06 | High | `SkirmishStrategyScoring.cs:151–158` subtracts 15 from a new candidate but never compares it with the current valid candidate; early returns choose the action first. | Evaluate feasible alternatives and retain the current plan unless the best alternative exceeds its current score by 15, the plan is invalid, or immediate terminal danger requires interruption. |
| AI07 | Medium | `SkirmishStrategyScoring.TryAfford:117–139` assumes Barracks/GroundStaging exist, hardcodes 48/8 caps and omits reserved capacity. `SkirmishProductionService.EvaluateQueue:186–231` already reads actual producers, faction stocks, live/reserved caps and readiness. | Use the authoritative non-mutating eligibility projection to build candidate feasibility. Do not maintain a second optimistic legality model. The execution service remains the final authority. |
| AI08 | High | `Scout` exists in `SkirmishStrategyContracts.cs:3–11` but is neither selected nor executed by this enemy planner. Hidden-base handling uses a legitimate public destination, but there is no route allocation or last-seen investigation policy. | Add affordable purposeful scouting, route evidence and last-seen investigation. Preserve the correct distinction between public terrain advance and precise visible-target Attack. |
| AI09 | Medium | `SkirmishEnemyStrategyComponent` stores failure count and personality, but the enemy planner has no bounded recovery policy or personality weighting. Spawn assigns `Seed % 3`; travel/exposure score fields are unused by the scorer. | Add bounded retry/backoff and observable recovery. Use an independent deterministic strategy seed stream and bounded personality weights. Include feasible route cost and known exposure in scoring. |
| AI10 | High | The enemy executor only purchases Rocketeer/AntiAir counters and sends Attack/Move/Hold. It has no own replenishment, coordinated retreat, staged research/economy or aircraft operational policy. | Implement ground recovery first, then faction-correct paid economy and supported air/transport capability stages. Treat these as missing strategic orchestration, not proof that shared aircraft/logistics mechanics are broken. |
| AI11 | High | `SkirmishNativeCheckpointParts.cs:247` already registers enemy state. Its `Valid<T>` checks layout hash and byte size at lines 395–400. `SkirmishSetupCompiler.ComputeHash:533–545` hashes the difficulty ID but no AI policy version/settings. | Plan policy identity and checkpoint migration before changing state layouts. Preserve accepted queues, cooldowns, assignments and randomness across Continue; never silently reset AI purchases or reinterpret old component bytes. |

## Player experience

Regular should feel like a readable opponent with a purpose: a protected base, a scouting patrol, an escorted attack, a response to a real raid and a regroup after losses. The player can intercept a scout, defend a lane, exploit a thin flank, disrupt observed supply or punish overextension. A successful defense is allowed to keep the player base undamaged.

The enemy should not send isolated structure attackers one by one, retreat its entire army for a harmless distant scout, pursue hidden live coordinates, or freeze permanently after losing its first anti-building group. It can lose honestly, and it need not attack into certain destruction. Whenever it waits, telemetry must distinguish a deliberate reserve or recovery from missing behavior.

Difficulty changes decision quality and coordination according to the existing contract:

| Policy | Recruit | Regular | Veteran | Commander |
|---|---|---|---|---|
| New strategic information response | 5–8 s | 3–5 s | 1.5–3 s | 0.8–2 s |
| Proactive scouting interval | 45–60 s | 25–40 s | 15–25 s | 10–20 s |
| Earliest deliberate Field BA/FC offensive departure | 90 s | 60 s | 45 s | 30 s |
| Earliest deliberate Established departure | 60 s | 40 s | 25 s | 15 s |
| Pressure when affordable | One front | Main front and occasional flank | Two fronts and mobile reserve | Two or three purposeful fronts |

These numbers come from MATCH_SETUP, not new tuning. Opening restraint prevents proactive offensive departure before the stated minimum; it does not force an attack at that instant. Local defense is always allowed. The short Commander response range requires a due-event scheduler; a fixed four-second polling delay alone cannot meet it. Do not delay unit firing/pathing or player input. Do not expose additional difficulty choices merely because their policy tests pass.

## Proposed implementation architecture

Names below are proposed additions, not claims that these classes already exist. Keep the current ECS/Contracts/Configs/Runtime dependency boundaries and existing assembly definitions.

### Compile an immutable policy

Add a typed `SkirmishEnemyPolicySnapshot` to resolved setup, containing policy version, response/scout ranges, Field and Established opening restraint, decision cadence, hysteresis margin, reserve target, retry budget, allowed operational capabilities and bounded personality weights. Resolve from the selected difficulty and supported objective/army/start data through the existing catalog/config owners. Validate min/max ranges and unsupported combinations.

Include policy version and canonical serialized settings in setup/content identity. Do not rely on live ScriptableObject lookups after launch. Preserve old setup identity handling explicitly for compatible saves and invalidate old acceptance for changed AI behavior. Reuse the authored route metadata in `SkirmishMapLayoutConfig` (waypoint IDs, measured travel times, width and domain flags); compile the needed immutable route view rather than hardcoding a second route map in Runtime.

### Build a legal observation snapshot

Add a faction-scoped `SkirmishEnemyPerceptionBuilder` or extract the existing `Perceive` method. On each bounded decision, project:

- Own living groups: stable group ID, role/target domains, member health, position, active order, ammo/fuel/service availability, transport/airborne status, supply and reservation state.
- Own base/facilities: legal own health and recent damage, producer/research queues, stocks, live/reserved caps, logistics state and valid rally/service pads.
- Visible hostile contacts: only after `SkirmishFogService.IsVisibleToFaction` passes; classify threat by target compatibility, distance/travel to own assets and observed combat contribution. A small anti-structure team can matter more than eight distant rifles.
- Last-seen contacts: frozen position, observation time/source and confidence from the existing knowledge/ledger service. Expired or destroyed/unresolvable contacts lose entity-target authority. Do not refresh them from hidden transforms, health or queues.
- Public objective terrain and authored legal routes. Public fixed-base location allows an approach Move; exact Attack still requires current legal visibility.

Do not expose omniscient debug census data through this snapshot. Fog checks must precede reading hidden contact details. Missing information is explicit, not zero enemy strength. Base terminal facts can stop the planner; they do not disclose live enemy-base health.

### Schedule decisions and compare candidates

Use the authoritative paused simulation clock. Initial periodic strategic reassessment is every four simulation seconds, consistent with OBJECTIVE_IMPLEMENTATION; stagger by a separate deterministic AI seed stream. Track significant observed events and schedule a response within the selected difficulty window. Coalesce events to avoid a callback or replanning storm. Lifecycle invalidation and terminal shutdown are immediate; tactical autonomy stays responsive.

Evaluate feasible DefendHome, Scout, Assemble/Assault, Reinforce, Recover and Hold/Reserve candidates. Initial utility is the existing contract: objective gain 40, prevent terminal loss 100, observed counter advantage 20, travel penalty up to 30 and known-exposure penalty up to 30. Preserve mandatory-subject value 80 for future policies without applying BT/CE semantics to BA. Illegal/unaffordable candidates are excluded with typed reasons.

Recompute the current plan's utility on the same snapshot. Switch only if the alternative exceeds it by 15, the current plan becomes invalid, or terminal danger supersedes it. A minimum commitment window is a tuning option for non-emergencies, not a substitute for that comparison. Record winner, runner-up, rejected candidates and switch reason. Tie-break with stable IDs and a saved deterministic stream, not Unity entity iteration order or global random state.

Keep a semantic action key per group: assignment revision, order kind, route/waypoint, stable target identity and destination tolerance. Do not resubmit an unchanged accepted order merely because another decision tick ran. A new visible target, obstruction, group loss or invalid route can invalidate it. Track submission, acceptance, progress and completion separately.

### Allocate groups and routes

Add a session-owned `SkirmishEnemyGroupAssignment` buffer. Each eligible group gets one current duty and a reason: HomeReserve, Scout, MainAssault, Escort, Flank, Recover or Service. Do not split the existing four-person squad into artificial units or recount embarked passengers. Independent group duty is not a second combat owner.

Reserve approximately 25% of available combat Supply while home is safe, rounded to useful whole groups. Use actual healthy capabilities, not empty cap slots or logistics trucks. Keep a viable structure-damage package plus escort before deliberate assault. If the army is too small to field a reserve, scout and two attacks, prioritize local defense and one viable group; never invent troops to satisfy percentages.

For the main attack, select anti-structure capability and complementary mobile-target defense. Rifles screen hostile infantry; Rocketeers/Breachers/Siege/Tanks attack only compatible targets; AA protects against observed air. Do not require a Tank when the profile/start has none. Rally on legal measured pads, then leave as a coherent force. Reinforcements join a rally or reachable active group; avoid a stream of unsupported replacements.

Choose routes from the current logical map layout and shared navigation. Reject width/domain incompatibility, unreachable paths and illegal bounds. Score travel cost and known hostile exposure. Last-seen threats can raise uncertain risk without revealing current occupancy. Never read hidden dynamic blockers as strategic enemy intel through a planning diagnostic. Use a second validated approach when the reserve and escort budget permit; absence of a viable second route is a source-fit/evidence gate, not authority to build a road.

Track path acceptance and actual movement progress. A blocked group tries a bounded alternate waypoint/route and then regroups or reports the reason. Initial no-progress diagnostic threshold: the greater of 10 simulation seconds or twice the strategic interval, excluding legitimate production/rally/service waits. Calibrate against measured routes; do not teleport, force transforms or reset shared navigation globally. Preserve the shared movement system's command ownership.

### Defend locally and recover

Build threat zones from the own designated base, production and supply facilities. Include own observed damage, hostile target compatibility and estimated local strength. Stop treating every visible unit anywhere on the map as a home raid. Distinguish a harmless scout, a mobile anti-structure raid and a terminal base threat.

Assign the minimum feasible compatible response from local reserves, then recall further groups only as needed. Use normal Move/Attack to reachable intercept points; Hold is appropriate after reaching a defensible position, not the complete response. Preserve the existing visible hostile structure intrusion behavior and its reachability fallback. Clear defense only after danger and a short policy stabilization window have passed, then resume the prior viable task.

Recover expensive or isolated units when a legal safe destination exists and expected survival improves. Preserve needed AA/escorts during a withdrawal. Use native Return/service/repair only if the corresponding mechanic is implemented and certified; otherwise regroup without healing. A defeated assault should trigger replacements, a different approach or a disclosed resource limitation, not a permanent deadlock. No compulsory attack into certain loss solely to hit a pressure metric.

### Replace the one-time production latch safely

Desired counters depend on legally observed composition, own living counters and reserved/in-transit deliveries. Reassess after actual delivery, cancellation, death or changed evidence. Store a purchase intention with role, stable request ID, desired capability, observation provenance, reservation ID, phase, timestamps and bounded retries.

Use `SkirmishProductionService.EvaluateQueue` for faction-correct feasibility and the native production adapter for real requests. Extend `SkirmishNativeProduction.RequestEnemyCounter` into a general typed enemy intent adapter only as needed. Its current owner-wide poll removes the first completed request without matching a role-specific intention; do not start multiple intentions through it until request/role correlation is implemented. Initially allow one outstanding strategic purchase per faction and use stable request IDs.

Lifecycle: Proposed → RequestPending → Accepted/Reserved → Producing → InTransit → Delivered, or a recorded Reject/Cancel/Destroyed branch owned by the existing systems. Pending is not failure; queue acceptance is not delivery. A changed threat cannot make a late acknowledgment count as a different purchase. Retries with the same token cannot duplicate cost, capacity or troops. At most three identical rejected attempts without relevant new evidence, then backoff or another legal action; do not retry every frame.

Reserve resources for a cheapest relevant counter and a logistics replacement when viable, as the objective contract requests. Avoid hoarding all funds while a base is about to fall. Rebuild missing non-designated production only through supported paid construction on legal pads, and only if return on investment fits remaining mission time. Rebuilding a Barracks never resurrects the designated objective. Research and new production capacity compete with immediate defense; they are not an unconditional tech rush.

Economy and logistics remain physical: paid suppliers, available Oil, refinery throughput, Materials/Fuel delivery, queue capacity and current readiness. The AI may issue supported faction-aware logistics commands; it must never write stocks or move cargo directly. If no supported adapter exists, implement a narrow shared command adapter with the same eligibility/transaction owner, not a second economy simulator.

### Add air and transport only through supported capabilities

For Combined Arms/Air Mobile, progress from ground/local defense into affordable certified producers/research. Use Drone reconnaissance from the actual visibility service. AA/Fighter response requires legal observed air; offensive aircraft require valid launch, fuel, target-domain, return-slot and second-sortie conditions. Transport needs a real passenger manifest, capacity reservation, safe reachable destination and normal board/unload commands. Reuse current airfield and shared flight owners; no instant spawn, free fuel, hidden target selection or free readiness.

Air/transport are a later work package, not a prerequisite for proving the first ground-strategy improvement. An isolated aircraft mechanics pass does not certify the enemy's end-to-end strategic use. Keep future Support disabled until its existing gates pass. Record unsupported capability paths rather than routing every profile through a ground-only strategy and claiming completion.

### Persist the plan and close it cleanly

Before expanding `SkirmishEnemyStrategyComponent`, define a compatibility approach. Prefer a new versioned planner component/buffers while preserving old component bytes, then an explicit validated migration/defaulting path for known old saves; alternatively implement a documented schema migration. Do not accept mismatched raw layouts, discard the save, reset resources or overwrite a newer checkpoint. If a version cannot safely migrate, use the existing explicit incompatibility flow and retain the file.

Register new components/buffers with the native checkpoint owner. Persist remaining reaction/opening/scout timers, strategy PRNG state, current plan/utility, assignments/routes, pending purchase tokens and retry state. Use the codec's stable identity remapping for entity references; do not retain raw Entity handles outside it. Reconcile restored intentions against restored reservations and actual units before submitting anything. Saving during a pending counter purchase must not grant a second counter or freeze it forever.

Pause freezes timers and spending; Continue restores the same logical attempt. Replay gets a new attempt with clean AI state and the selected setup/seed. Result, Adjust Setup and Main Menu remove pending requests and owned state once. No post-result purchase, command or callback may affect a later attempt. Preserve already valid enemy opening orders in lifecycle assertions while checking that old-attempt orders do not leak.

## Files and responsibilities

| Area | Existing owners to change or reuse | Proposed work |
|---|---|---|
| Policy/config | `Configs/Skirmish/SkirmishDifficultyConfig.cs`, `SkirmishResolvedSetup.cs`, `SkirmishSetupCompiler.cs` | Compile policy/settings/route identity; preserve catalog and version semantics. Add Field restraint in the proper policy owner. |
| Contracts | `Skirmish/Contracts/SkirmishStrategyContracts.cs` | Explicit legal knowledge, candidate/assignment and typed decision/rejection records. Keep ARIA contracts compatible. |
| Session state | `Components/Skirmish/SkirmishExpandedSessionComponents.cs`, `Runtime/Skirmish/SkirmishScenarioSpawnSystem.cs` | Versioned planner state, separate seed stream, initialization and group-duty buffer. |
| Strategy | `Runtime/Skirmish/SkirmishEnemyStrategySystem.cs`, `Configs/Skirmish/SkirmishStrategyScoring.cs` | Thin scheduler plus pure candidate scoring and faction observation; extract group planning rather than grow one giant method. |
| Commands/routes | `SkirmishArmyCommandService.cs`, `SkirmishWorldMovementService.cs`, `Configs/Skirmish/SkirmishMapLayoutConfig.cs` | Reuse normal commands and shared navigation; compile legal routes, deduplicate intents, observe acceptance/progress. |
| Production/economy | `SkirmishNativeProduction.cs`, `SkirmishProductionService.cs`, `SkirmishResearchService.cs` | Correlated async intentions, authoritative eligibility, real paid research/logistics and delivery observation. Preserve transaction owners. |
| Fog | `SkirmishFogService.cs`, `SkirmishFogService.Ledger.cs` | Reuse visibility/ledger; add projections only where necessary. Keep hidden transforms/cash out of the planner. |
| Save/lifecycle | `SkirmishNativeCheckpointParts.cs`, `SkirmishNativeCheckpointCodec.cs`, existing snapshot/recovery/production checkpoint owners | Schema compatibility, identity remapping, in-flight intent reconciliation and terminal cleanup. |
| Tests | `Assets/Tests/Editor/SkirmishExpansion/SkirmishExpandedAriaTests.cs` and existing fog/production/checkpoint/movement suites | Preserve shared legality tests; replace one-counter-only assertions with bounded production lifecycle requirements. Add dedicated enemy planner/allocator tests. |
| Native evidence | Existing S005 input harness and passive observers | Add enemy telemetry without supplying hidden information to player drivers. Separate completion, challenge, performance and device gates. |

## Delivery sequence

The owner has released the hold; begin with Step 0, then follow the dependencies below. The S005 owner remains responsible for the main Editor and runtime integration. Keep source stable during each native run. Use narrow commits; do not stage unrelated reports or thousands of raw artifacts. Preserve lossless evidence indexes and failed runs under the existing evidence workflow.

| Step | Deliverable | Exit criterion |
|---|---|---|
| 0 Baseline | Passive enemy decision/group/production telemetry and baseline comparison on current behavior | At least one baseline normal run attributes missing pressure to observable decisions or explicit unknowns. Existing six proofs are preserved; no behavior tuning in this commit. |
| 1 Foundation | Compiled policy, timers, independent randomness, bounded scheduling, semantic order keys, checkpoint strategy | Difficulty/opening/pause/Continue legality tests pass. Same observations produce reproducible planner decisions. No per-frame duplicate strategic commands. |
| 2 Ground opponent | Real candidate comparison, local defense, group reserve/escort/main attack, repeated paid counter/replacement | Ground behavior scenarios and a native passive-player control prove feasible sustained objective pressure; normal opponent play still has fair counterplay. |
| 3 Recon and recovery | Actual scouting, legal last-seen use, route alternatives/flank, retreat/regroup, route-failure recovery | Hidden-information invariance, route constraints and bounded recovery tests pass; native two-approach evidence is recorded. |
| 4 Economy and combined arms | Supported paid infrastructure/research/logistics recovery and air/transport orchestration | Native purchase/delivery/flight/service/return/cargo evidence proves each enabled behavior. No unsupported feature is exposed. |
| 5 Acceptance | Frozen candidate, refreshed affected mission evidence, balance and device report | All applicable gates below pass or remain explicitly pending; only certified configurations may be published. |

Do not defer checkpoint identity, legality or duplicate-purchase protection until after tactical tuning. These protect every later step. Retain the ability to revert a narrow policy change through source control; avoid a hidden per-seed fallback that weakens the opponent during acceptance.

## Validation and success criteria

### Focused automated tests

Add behavior-level tests that detect the audited failures. Isolated fixtures may construct state for focused tests, but must be labeled as such and never counted as normal mission wins.

1. **Knowledge invariance:** changing hidden player position, health, cash, orders, queue or readiness while holding the legal observation constant yields the same strategy/actions. Last-seen expiry, hidden movement/death, faction isolation and observed reappearance behave correctly. Public-base terrain approach remains legal while hidden entity Attack is rejected.
2. **Timing:** each difficulty obeys its reaction/scout ranges and Field/Established restraint; a local raid can be defended during restraint. Pause and result produce no new decision, spend or command. Commander event handling meets its shorter window without faster firing or simulation.
3. **Hysteresis:** alternatives within 15 points retain a valid plan, an alternative above the margin switches, an invalid current plan changes, and terminal danger overrides. Equivalent entity-query order and locale do not change the strategy seed stream.
4. **Force allocation:** every living eligible group has exactly one duty or explicit exclusion. Main attack contains a structure-capable group plus feasible support. Reserve counts combat supply, not unused cap or logistics. No-Tank starts, casualty loss, newly delivered groups, boarded soldiers and insufficient forces are handled without duplication or permanent idleness.
5. **Defense:** distant observed infantry does not falsely trigger a base emergency; a small nearby anti-structure raid does. Defenders reach an intercept rather than only receiving Hold. A harmless scout does not recall the whole army. Safe clearance resumes a prior viable task.
6. **Production:** a pending request is not resubmitted; accepted does not mean delivered; exact request/role acknowledgment is correlated. Counter death or a newly observed threat permits another legal purchase. Caps include reservations; absent/destroyed producers, research locks, full queues, insufficient resources, refunds and late callbacks retain native invariants. Player stocks never change from enemy purchases.
7. **Navigation:** infantry/vehicle/air constraints and illegal pads reject cleanly; accepted but stalled orders get bounded recovery. An unchanged accepted command is not reissued. A valid alternative is used without terrain edits or transform writes; exhaustion produces an honest reason.
8. **Recovery and aircraft:** withdrawal does not heal/refuel; native service rules do. Fuel, occupied return pad, lost producer, second sortie and passenger conservation are exercised for each enabled capability. Unsupported roles are explicitly gated.
9. **Checkpoint/lifecycle:** save mid-response, scout, defense, rally and pending production; restore assignments/timers/PRNG/reservations without extra spend or free troops. Test old/current schema, corrupted references, result shutdown, Replay, Adjust Setup, Main Menu and stale request rejection across attempts.
10. **Shared regressions:** current fog, movement, economy, research, selection, capacity, checkpoint and outcome suites; existing expanded BA profiles and affected Campaign/legacy journeys. Update tests that accidentally demand the old weak behavior, while retaining their no-cheat/no-spam purpose.

### Native behavior and challenge tests

Run these through the existing normal launch and real command boundaries, with source/config hashes and normal simulation speed. Predeclare test scripts/seeds and retain every outcome.

| Scenario | Required observation |
|---|---|
| Passive player control | With unchanged ordinary setup and a viable route, enemy leaves only after opening restraint, brings a coherent force, applies genuine base pressure and can finish the objective before the original deadline. If unable, retain the failure and identify route/economy/tactics; do not lower HP or extend time. This control demonstrates capability, not player balance. |
| Player defends competently | Enemy scouts, adapts and can regroup; an undamaged player base is allowed. Report both sides' losses, exchanges, idle causes and replacements instead of forcing a damage quota. |
| Early local raid | Compatible defenders respond within the legal reaction policy; reserve and assault assignments remain coherent. No unseen raid detection. |
| Observed armor then air | Appropriate counters are paid and delivered at different times; the first purchase does not permanently disable the second. No counter decision before legal evidence. |
| Lost first assault or counter | Affordable replacement and regroup happen; no isolated wave or permanent idle state. If unaffordable, the resource/recovery reason is visible in diagnostics. |
| Route blockage or flank opportunity | Use existing legal geometry and a compatible alternative; no hidden player-position knowledge. A second front is conditional on capacity, not forced. |
| Low fuel or lost producer | Native service/recovery and paid reconstruction or an honest alternate plan; no injected fuel or building resurrection. |
| Manual takeover and resume | Player Stop/Move/Hold, Save/Continue and result/return remain functional while enemy operations continue legally. Do not mistake new enemy orders for stale player state. |

For stable conditions, a feasible urgent duty or affordable needed replacement should not remain unassigned for more than two strategic cycles after its deliberate response deadline; log legitimate queue/travel/service constraints separately. Every issued action must have a legal reason and an accepted/rejected/progress outcome. These are planner correctness goals, not promises that every attack succeeds.

Refresh the existing S005 Regular minimum on the new behavior: three fixed seeds in EN and three in FA, at least two genuine wins per language and no input/deadlock violation, retaining all losses. Reuse 104734/130368/155926 for comparison and add a predeclared held-out deployment seed for generalization. Improving an ARIA tactic must keep ordinary input and legal observations; never weaken the enemy when ARIA is active.

The family requirement remains 20 seeds alternating Field/Established for each applicable map/objective/army stratum. S006 Established is currently unimplemented, so Field-only repeats do not close that gate. Additional exposed difficulties/sizes require the existing supplemental full-match, ARIA, recovery and device evidence. Do not invent a target win percentage from six samples or reject a legitimate loss solely to manufacture a perfect report.

### Telemetry and performance

Record decision time in simulation ticks, attempt/setup/source/policy version, independent strategy seed state, legal observation provenance/age, current and candidate utilities, assignments/group capabilities, route ID, request/action key, command acceptance/rejection, retries and progress. Sample ongoing state at a bounded interval and record transitions immediately; avoid a per-frame full-world JSON dump.

Compare baseline and candidate time to first scout, first contact, first effective pressure, group duty/idle time, formation coherence, paid spend/delivery/replacement, reserve response, route failures, casualty exchanges and both base health histories. Separate strategic idleness from native movement or production waits. Keep an omniscient audit stream isolated from planner input if one is needed for diagnosis.

Profile decision CPU p50/p95/p99, query/temporary allocation count, submitted path requests, command rate and frame p95/p99 on representative supported armies. Reuse queries and scratch storage, project once per decision and bound group-action dispatch; initially no more than one outstanding purchase intention and a small fixed command-dispatch budget per frame. Measure before adding jobs or caches. Strategic cadence must not slow the match clock or unit simulation. Apply the project's target-device frame budget; Editor measurements alone do not certify Android performance.

All Unity executeMethod validation/captures use `rtk proxy Tools/CI/invoke_unity_macos.sh` with an explicit timeout/log and required pass marker. Keep Hub open/signed in; no direct Unity executable, batchmode, IPC reset or process termination. The S005 owner controls scheduling with its running Editor and preserves full logs, actual wrapper/native exits and pre/post source equality. A timeout, missing marker or changed source is a failed or inapplicable run, not a licensing excuse or a pass.

## Handoff acceptance

The implementer should acknowledge the owner release and this plan, retain its existing acceptance report, and begin the next work package at a safe validation boundary. Report each package with changed behavior, affected files/policy identity, focused tests, native journeys, failures and outstanding gates. Do not claim all mission readiness from planner tests or a passive-player victory.

Completion requires: reusable source changes for the enabled scope; legally explained coordinated pressure; bounded counter/replacement behavior; working local defense/scouting/recovery; correct save and teardown; updated affected normal-input manual/ARIA journeys and result/return; preserved historical evidence; and separate owner visual/challenge review and real device/player acceptance. Source completion, technical checks, native playthroughs and device/player approval remain distinct statuses.
