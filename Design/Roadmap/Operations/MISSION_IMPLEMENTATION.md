# Shared mission implementation contract

Proposed implementation, 2026-09-21. Read this with each [district brief](Missions/README.md). Parameters below are starting test values. Each mission inherits these exact defaults unless its brief gives an explicit override. A family reference is a shared implementation dependency, not proof that any current Campaign class already supports it.

## Rules and classes to implement once

Implement typed rule configs and components under the architecture folders. An objective graph composes these rules. The shorthand used in briefs is design notation; builders compile typed configs, never parse prose at runtime.

| Brief notation | New ECS owner | Exact initial behavior |
|---|---|---|
| `SCAN(a,b,...)` | `OperationsReconObjectiveSystem` | Confirm each bound site through a legal completed Scan/recon action with line of sight and range validation. Initial interaction 15 s/site, 8 m radius. Hidden sites first need observation/known search zone. One object cannot satisfy two required sites. |
| `VISIT(a,b,...)` | `OperationsRouteObjectiveSystem` | A commandable dismounted squad reaches every ordered 8 m waypoint; at least two surviving members required. Previously visited points do not reset. Skipping a waypoint does not count. |
| `HOLD(zone,seconds)` | `OperationsControlObjectiveSystem` | At least one dismounted eligible infantry inside 12 m, no living hostile combatant inside, for continuous duration. Contest/empty zone resets the timer; vehicles/passengers cannot capture. |
| `CLEAR(role)` | `OperationsTargetObjectiveSystem` | All finite hostile entities bound to the role are destroyed or explicitly surrendered by a supported system. This plan uses destruction only; don't invent surrender. Newly activated members of that authored role are included. |
| `INTERACT(object,seconds)` | `OperationsInteractionObjectiveSystem` | Eligible dismounted infantry within 6 m, no hostile within 12 m, continuous channel; interruption resets the channel. For evidence, store carried object ID and transfer it only through explicit load/unload/extract rules. |
| `REPAIR(site)` | `OperationsRepairObjectiveSystem` | Designated repair unit within 6 m, site not destroyed, no hostile within 12 m; spend 40 tactical Materials once, work 45 s; pause under threat. Reach 75% site health and set restored fact. Site destruction fails that node. A second repair cannot charge again for the same node. On a live retry only, a verified exact SiteId already Restored in the launch snapshot and matching a living restored world site completes on activation without work/cost; a generic milestone flag is insufficient. |
| `ESCORT(group,n,route)` | `OperationsEscortObjectiveSystem` | At least n of the authored cargo vehicles reach the ordered route's exit alive with cargo. Visible Go/Hold and route-choice controls issue normal movement; no teleport or proximity-only completion. Cargo counted once at unloading zone after 10 s. |
| `RESCUE(group,n,exit)` | `OperationsRescueObjectiveSystem` | Secure public rescue point, interact 10 s to release survivors, then escort or legally board and deliver at least n individuals to exit. Individuals remain neutral/non-commandable; a dedicated escorted-follow order binds to a player escort. Total individual identities survive boarding and checkpoint. |
| `AIRLIFT(group,n,lz,exit)` | `OperationsExtractionObjectiveSystem` | Secure LZ, board compatible passengers, fly legal route, land/unload at exit; count n living delivered identities. LZ claim or departure alone never completes extraction. Uses shared aircraft/transport systems, with generic extraction facts separated from M04 scripting. |
| `STOP(group,n)` | `OperationsInterdictionObjectiveSystem` | Destroy at least n of the finite marked hostile cargo vehicles before their route exit; escaped IDs are irreversible failures. |
| `BREACH(gate)` | `OperationsBreachObjectiveSystem` | Authored hostile gate destroyed through shared combat, traversal blocker updated, and at least one eligible player squad traverses into the interior zone. Generic gate/path facts extracted from proven M05 behavior. |
| `EXTRACT(role,exit)` | `OperationsExtractionObjectiveSystem` | At least two original eligible player infantry reach exit, and all mandatory carried evidence/passenger conditions are true. If using a transport, require actual unloaded arrival; otherwise use a secured ground exit. |
| `PROTECT(role,floor)` | `OperationsProtectionObjectiveSystem` | Global condition active from launch until terminal result. Floor is count or explicit health threshold. Destruction/death facts evaluated before success on a shared tick. Protected site health must stay >0 unless specified otherwise. |

`PROTECT` nodes are active from launch even when a graph lists them beside a later phase. Role death before its objective activates still matters. Optional nodes never gate Victory; the brief's partial predicate controls Partial. Success/partial/failure facts are frozen by `OperationsMissionOutcomeSystem`, not these rule systems. Mission progress UI and ARIA read the same node state.

`OperationsCivilianEscortOrderSystem` is a necessary **new shared mechanic** for RESCUE ground evacuation: release and bind civilian groups, follow safe waypoints at civilian speed, stop on threat/escort loss, allow transfer to another eligible squad, handle boarding/death, publish progress. Do not fake rescue by deleting civilians at the collection point. Neutral people stay outside Army combat selection.

`OperationsEvidenceCarrySystem` is another **new shared mechanic**: evidence starts at a bound object, is picked up by a living eligible infantry unit, follows that stable carrier identity through a vehicle, drops at a reachable last ground position on carrier death, and can be recovered by another unit. If the carrying transport is destroyed, use the supported passenger casualty outcome and drop surviving recoverable evidence at the wreck's ground anchor; do not duplicate it. A brief can mark a particular object destroyable, which is explicit required-failure behavior.

Repair and timed interactions require visible action buttons, progress, interruption reasons, selection/focus targets, command eligibility and ARIA affordances. A timer incrementing without a real issued action is not implementation. Civilian/evidence/repair mechanics are blockers until tested, not flavor text.

## Twelve families

| Family | Shared rules / usual sequence | New family-specific work |
|---|---|---|
| RECON | Scan → optional evidence → extract | Confidence/known search zone projection and scan eligibility |
| PATROL | Ordered visit → clear/hold → safe return | Waypoint progress and blocked-route handling |
| RAID | Confirm → breach or secure → evidence → extract | Confidence gate, carried evidence, protected structures |
| RESCUE | Secure → release → move/board → deliver | Neutral civilian escort and survivor accounting |
| ESCORT | Choose route → Go/Hold → clear → deliver cargo | Cargo identities, route branch lock, loss/arrival facts |
| REPAIR | Secure sites → spend Materials/work → defend | Repair interaction/cost reservation and site state |
| DEFENSE | Prepare → warned finite waves → hold/protect | One wave director, objective-triggered phases; no endless spawning |
| INTERDICT | Observe route → block/engage → deny exits | Authored hostile transport routing and escaped-target terminal checks |
| SEIZE | Reach sites → capture/hold → fend off counterattack | Parallel capture nodes and occupation/progress UI |
| AIRLIFT | Secure landing zone → board → fly → unload | Shared fuel/landing/passenger checks and extraction result |
| BREACH | Approach → gate/path opens → target/evidence → extract | Shared breach interactions, path-blocker update, protected object checks |
| FINALE | Multi-phase composition of certified rules | No bespoke finale engine; use graph, roles, and local milestone settlement |

Implement rule classes by the first mission requiring them. Do not create `O001System` or a mission-ID switch. Content can differ in roles, node graph, routes, timers, initial forces, protected targets, consequences and finite trigger groups. Any new rule introduced by a future brief needs a schema entry, shared system, HUD/ARIA behavior and tests before it becomes a mission dependency.

## Force packages and tactical resources

Semantic roles below must resolve in a roster manifest to **actual certified config/prefab keys**, producer metadata where used, transport capacities, selection portraits, counter behavior, EN/FA names and save adapters. These role aliases are not invented existing prefab IDs. The package builder rejects unresolved roles. Reuse certified Campaign/Skirmish roster work; do not infer capability from mesh or filename.

| Package | Scenario-provided starting task force (individual entities) | Materials/Fuel/Oil; use |
|---|---|---|
| `FP_LIGHT` | 12 rifle infantry, 2 recon infantry, 2 support infantry = 16 | 80/0/0; foot recon, patrol, raids |
| `FP_SERVICE` | 12 rifle, 2 recon, 4 support, 2 repair specialists, 2 APC = 22 | 240/240/0; repair, road escort, ground rescue |
| `FP_GROUND` | 20 rifle, 2 recon, 4 support, 4 anti-armor, 2 repair specialists, 2 APC, 2 tanks = 36 | 240/360/0; defense, seize, interdiction, breach |
| `FP_AIR` | 12 rifle, 2 recon, 4 support, 2 anti-air infantry, 2 transport helicopters = 22 | 120/480/0; air evacuation, air-access missions |
| `FP_COMBINED` | 20 rifle, 2 recon, 4 support, 4 anti-armor, 2 anti-air, 2 repair specialists, 2 APC, 2 tanks, 2 transport helicopters = 40 | 320/600/0; district finales and combined transport |

Each mission adds its explicit civilians, enemy roster, cargo vehicles, repair-site entities and evidence objects. They are not silently included in the force totals. A cargo escort mission adds exactly three friendly cargo trucks by default; a rescue adds 12 civilians; an airlift adds 12 passengers. Tank/air missions require tested usable Fuel semantics; above values are tuning candidates. At minimum provide 150% of the measured fuel needed for one required round trip plus a safe return. Validate transport seats against each authored extraction plan; allow multiple sorties only when the mission deadline and ground protection make that viable.

No base production or Oil chain is required by these 60 starting briefs. Repair is the only mandatory Material spend. New construction/production variants require an explicit future revision. Disabled build/economy controls should say why when relevant, not show nonfunctional buttons. A mission needing three repair sites has at least 120 Materials in its package. Player account Credit balance is never read in a tactical match.

## Enemy packages and finite schedules

| Package | Full finite mission budget, including waves | Legal behavior |
|---|---|---|
| `EP_CELL` | 18 rifle + 2 support = 20 | Guard public search areas, patrol one alternate lane, react only to observed threats |
| `EP_RAIDERS` | 24 rifle + 4 support + 2 light vehicles = 30 | Two approach groups and one reserve; target escorts/services only when observed or publicly known |
| `EP_MECHANIZED` | 24 rifle + 4 support + 4 anti-armor + 2 APC + 2 tanks = 36 | Hold chokepoint, flank with infantry, commit armor after warning |
| `EP_AIRFIELD` | 24 rifle + 4 support + 4 anti-air infantry + 2 light vehicles = 34 | Defend ground access and publicly identified anti-air sites; no mandatory enemy jets |
| `EP_FINALE` | 32 rifle + 4 support + 4 anti-armor + 2 APC + 2 tanks = 44 | Three finite groups split between protected objective pressure and approach denial |

Default split: 50% initial, 25% reinforcement A, remainder reinforcement B; round individual roles down in the first two groups, put remainder in B. Group A warns on completion of the first required node and arrives 30 seconds later at its declared boundary anchor. Group B warns on activation of the final required phase and arrives 45 seconds later. No wave means the reserved entities remain unused, not spawned after result. Each brief can change trigger/route but not increase budget without explicit fields. Optional-node activation never spawns a punitive extra wave.

Required guard roles in an early CLEAR node bind **initial** entities unless a brief explicitly gives an independent timed activation. Remaining A/B reserves use separate support roles. A CLEAR node includes every declared member of its role, including pending members; a pending unit is not already dead. Compiler validation builds a dependency graph including wave triggers and rejects any required-clear role whose missing members spawn only after that clear completes. Initial-placement overrides (such as O057's four AA infantry) move entities from reserves into the initial set, preserving the total budget. An unused support reserve is allowed; an unactivated required-clear member is not.

`STOP` missions add exactly three hostile cargo trucks that cannot fire, use a route released at elapsed 120 seconds, and cannot reach the exit before elapsed 240 seconds. They are separate from the EP combat budget. Mission-specific overrides appear in briefs. Enemy waves spawn outside immediate visible player occupation and enter through authored routes; if blocked, queue at a valid boundary staging zone within the same finite budget. They do not appear inside secured rescue/repair zones.

Initial behavioral presets: Recruit reaction >=8 s/one active attack group; Regular >=5 s/two groups; Veteran >=3 s/two groups with observed flank; Commander >=2 s/three groups with observed counter-role assignment. Minimum reinforcement warning is 20 s at every difficulty. No reaction reads unrevealed player entities. Use the owning shared AI strategy modules; `OperationsWaveScheduleSystem` authors scenario reserves but does not replace movement/combat AI.

## Mission graph defaults and results

Time targets are pacing estimates. Hard deadline in each brief is an actual terminal rule and starts when interactive control begins, after briefing/loading. It pauses with the simulation. Default ground extraction uses a held safe exit, no vehicle. A `CLEAR` refers only to the specified group, not all map enemies. An `EXTRACT` phase remains achievable without killing unrelated units.

All missions also fail if no eligible commandable player unit survives or a mandatory required object becomes impossible. Minimums such as 8/12 civilians or 2/3 trucks remain mandatory at the exact terminal tick. Deadlines evaluate the brief's Partial predicate if Victory is not satisfied. Pressing Conclude is available only after the Partial predicate becomes true; Withdraw is always a separate warned action. Optional mastery uses one badge per brief, has no account reward or prerequisite effect, and cannot compensate for failed mandatory objectives.

A `PROTECT(site,alive)` requirement applies at all times. A brief can allow partial success after losing a protected site only if it explicitly says so; defaults classify mandatory protection loss as Defeat. Shared strategic harm penalties apply in addition. Site/route facts are persisted from the final authoritative world state, not from a cinematic or UI message.

World fact capture is always active: a civilian delivered, evidence carried to a safe exit, site destroyed, or squad reaching an exit is recorded even if a later graph node has not activated. Gated nodes consume eligible facts according to their configured start policy; default SCAN/INTERACT/REPAIR require an action after activation (except REPAIR's verified restored-site retry rule), while delivered-identity/protection/exit facts can satisfy Partial at any time. Ground safe exits remain reachable for withdrawal/partial extraction before the final EXTRACT node; this does not skip required nodes for Victory. Conclude is a separate public action evaluating the currently true Partial predicate, not a direct result override.

## Six map authoring packets

| District / planned map ID suffix | Required layout and route choices | Reuse candidate to audit; not a ready asset claim |
|---|---|---|
| D01 `old_quarter` | Two connected pedestrian loops; protected clinic, courtyard, archive, rescue court, main/safe exit | M01 urban modules; City Crossroads navigation/selection lessons |
| D02 `civic_center` | Central plaza with two approaches; clinic/service annex/relay; covered foot and exposed vehicle routes; elevated landing pad with certified ground ramp and flight clearance for O018 | Campaign forward-post/service props and city modules |
| D03 `industrial_belt` | Ring road and freight spine; three separated service points; truck turning areas; depot breach | Industrial Basin modules, after current Skirmish work is accepted |
| D04 `river_crossing` | Two already traversable land crossings; quays, road checkpoint, evacuation apron; no simulated bridge collapse | Existing road/bridge surface system; new district authoring |
| D05 `highland_approach` | Switchback and longer safe route; overlook/relay/repair station; two valid ground extraction points | Mountain Pass modules when available; no unsupported cliff navigation |
| D06 `airport_perimeter` | Perimeter loop, two landing zones, hangar gate, service road, terminal evacuation area | Airfield Plains and M04 transport work after certification |

Every map has typed anchors for `spawn.player`, `spawn.enemy_a`, `spawn.enemy_b`, `exit.ground`, `exit.air` if used, approach lanes, all named mission sites, optional secondary approach and safe camera focus. Mission-role aliases in each brief bind to specific anchors in that scenario asset. IDs use `anchor.operations.d01.clinic` etc.; the exact mission role-to-anchor manifest is authored and validated by the map owner. `route.main`, `route.safe`, `route.flank`, `route.enemy_cargo` contain ordered typed anchor IDs, lane widths and vehicle clearance. No runtime searches by GameObject name.

Per-map deliverables: overview diagram with all routes/anchors, navigation clearance report for largest used vehicle, infantry and convoy reachability tests, minimap, mobile camera limits, EN/FA briefing imagery, placement/height/shadow inspection, civilian hazard zones and protected-object boundaries, all role mappings. Physical bridge destruction, arbitrary rooftop access, environmental fire damage and naval travel are excluded; if visuals suggest an unavailable path, correct the art/briefing.

## Agent's recipe for any one mission

1. Read the entry, shared family rules, district map contract, and prerequisite packages. Confirm needed rule systems and role capabilities are Accepted, not just present in source.
2. Create the three per-mission assets through `OperationsMissionAssetBuilder` with stable IDs and version; author roles, routes, finite groups, graph node IDs, timers, protected objects, partial predicate, consequences and localization keys.
3. Resolve the semantic force/enemy packages to certified keys and record actual roster/resource/transport totals. Pin seed 1101 + numeric mission index for the first canonical fixture (O001 uses 1102), plus perturbation seeds defined in acceptance.
4. Bind shared HUD/markers/interaction controls and public ARIA affordances. Add briefing, warnings, outcome reasons, local report copy and EN/FA localization. Required dialogue can use text until audio is separately produced; no mission silently depends on missing paid voice generation.
5. Add a data-driven mission fixture keyed by mission ID; use the entry's positive and negative cases plus common terminal/save tests. Avoid one-off gameplay code or a scripted ARIA solution.
6. Validate normal manual play, ARIA's visible-control win, consequence settlement, return/redeploy, and the relevant device gate. Record exact version/hash/seed/difficulty/language/input provenance.
7. Update catalog status and evidence paths only after passing. A missing feature is a dependency to implement, not permission to weaken the objective, fabricate a result or mark the mission complete.

ARIA must support observe, scan, route choice, group move/attack, hold/defend, escort Go/Hold, civilian release/follow transfer, repair, evidence recovery, boarding/disembark, breach, extraction, camera focus, and result return before the family is accepted. The mandatory win gate applies to all 60 entries and every exposed difficulty; template-level wins alone are insufficient.
