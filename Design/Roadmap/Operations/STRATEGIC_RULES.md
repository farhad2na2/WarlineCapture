# Persistent city and action rules

Proposal, 2026-09-21. Values below are initial balancing inputs. All policy belongs in versioned configs projected into ECS; no UI button computes these rules.

## Run and district state

One active Operations run per profile, plus archived run summaries and account-wide practice unlocks. A new run uses a chosen difficulty, nonzero seed, content version, and six starting district records. Preserve the old run when starting a new one through an explicit New Run confirmation. Difficulty is selected for the run; changing it later marks subsequent evidence with the changed profile rather than preserving an unqualified completion claim.

| Field | Range/default meaning | Authority |
|---|---|---|
| Security `S` | 0–100; capacity to keep routes/services safe | Patrol, protection results, threat pressure |
| Trust `T` | 0–100; willingness to support the command effort | Aid, rescue, collateral, community action |
| Infrastructure `I` | 0–100; usable district services | Repair/protection and damaged critical sites |
| Enemy influence `E` | 0–100; hostile operating freedom | Threat missions and unresolved incidents |
| Intel confidence `C` | 0–100; recent actionable information | Recon, analysis, decay; not wallet Intel |
| Heat `H` | 0–100; near-term hostile reaction pressure | Exposed operations, hostile alerts, de-escalation |
| Supply readiness `L` | 0–100; district access/logistical reliability | Deliveries, route service, blockades; not a spendable currency |
| Civilian density | Low/Medium/High authored district/mission exposure | Scenario civilian population configuration; never lower it as a reward for civilian deaths |
| Site state | Intact/Damaged/Restored for stable site IDs | Tactical facts and explicit day events; visual/mission-start overlay |
| Route state | Open/Contested/Blocked for stable route IDs | Mission facts and explicit crises; scenario route availability |
| Flags | Typed milestone and evidence IDs | Once per accepted outcome; never infer from display text |

Starting tuples `(S,T,I,E,C,H,L)`:

| D01 | D02 | D03 | D04 | D05 | D06 |
|---|---|---|---|---|---|
| 40,45,45,50,20,20,45 | 35,40,40,55,20,25,40 | 35,40,35,60,15,25,35 | 30,45,40,55,15,20,35 | 30,40,35,60,15,20,30 | 30,35,40,65,15,30,35 |

Clamp only after accumulating a transaction's signed deltas. The report records requested and actually applied deltas so a capped meter never displays a fictitious gain. Metrics are saved integers; tactical seconds/timers use simulation ticks or milliseconds, never wall-clock dates.

Author the undirected district-adjacency pairs in `OperationsCampaignConfig`: D01–D02, D01–D04, D02–D03, D02–D06, D03–D04, D03–D06, D04–D05, D05–D06. They affect only the explicitly defined day pressure. They do not permit units or cargo to travel between simultaneously loaded tactical scenes.

City site state is an abstract service condition, not a persistent full-physics battlefield. A destroyed protected service site settles as **Damaged**, with the destruction event retained. On a later eligible protection mission, its authored emergency service replacement starts at 60% health with matching damaged presentation and briefing; this grants no Infrastructure or restoration milestone. Intact/Restored sites start at 100%. A repair scenario starts its named Damaged site at 25% and repairs it through the actual interaction. All sites named by REPAIR in the 60 briefs start Damaged in a new run; other protected sites start Intact. Live Repair offers require an applicable Damaged site **or an unfinished Success milestone for that mission**; a restored site cannot be silently damaged again to create a mission. On a retry, an exactly matching, still-restored site satisfies its repair node without charging again, but clearing threats/holding/extracting remains required. An unrelated service flag never satisfies a repair node. This prevents a Partial with completed repairs from locking the missing Victory prerequisite.

Incidents may damage a site through a visible committed day event; a replay in Practice uses its independent authored starting state. Each finale's backup site is a distinct site from earlier district repairs. Once a local finale wins, do not offer it again in that live run. These rules prevent a destroyed prerequisite site from permanently making its next defense/repair impossible while preserving strategic damage consequences.

## Days and action points

Start on day 1 with **3 action points (AP)**. An accepted tactical deployment consumes 1 AP. Abstract actions below consume 1 AP. Unspent AP expires when the player commits End Day. Viewing, briefing, changing a proposed loadout, practice, cancel before Deploy, and returning from a failed content load cost nothing. One active tactical deployment at a time.

No real-time regen, paid AP, automatic overnight advancement, or offline district damage. Block End Day during a pending deployment/result/save. If zero AP, offer End Day and practice; do not trap the player in a disabled menu. Recovery from failure remains available tomorrow.

All baseline task forces, necessary counters, and critical mission tools are provided by the scenario. AP represents commitment; it is not cash or tactical population capacity. Trial version of this mode requires **zero Credit costs** for mandatory actions. Optional purchased supplies are outside the initial release gate and cannot unlock victory-only solutions.

## Six abstract actions (not tactical mission count)

| Action ID | Availability and limit | Committed result |
|---|---|---|
| `action.operations.analyze` | Any district; once/district/day | `C +12`; reveal a public opportunity hint, never exact hidden target coordinates |
| `action.operations.community` | Any district; once/district/day | `T +6, H -4`; makes a zero-Trust district recoverable |
| `action.operations.service` | `E <=70`; once/district/day | `I +5`; repairs minor service damage, cannot award a tactical critical-site milestone |
| `action.operations.patrol` | Any district; once/district/day | `S +5, H +2`; does not complete a Patrol mission |
| `action.operations.allocate` | One use citywide/day | `L +8` in selected district; no deduction from a tactical wallet |
| `action.operations.deescalate` | Any district; once/district/day | `H -10, C -3`; trades target freshness for lower pressure |

Existing Drone Scan maps to Analyze; Aid maps to Community until a named Aid Convoy inventory action is implemented. Repair maps to Service for an abstract action or a clearly labeled tactical Repair offer. Raid opens a tactical offer/briefing. Never use the same button/cost label to ambiguously choose an abstract result or launch combat. A missing precondition returns a localized reason with zero AP loss.

## Offer selection and mission unlocks

Each catalog mission has a stable definition ID; each appearance gets a unique `OfferId`. The director is deterministic for `(runId, day, seed, districtId, directorVersion)` and saves its resolved offers. Do not reroll offers on menu navigation, restart, language changes, or a rejected click.

Per district, publish up to two eligible tactical offers; show three citywide priorities and allow all districts to be inspected. Rank urgent warnings, uncompleted arc missions, then replayable relief missions. Break ties by stable mission ID. Never create a second active offer for the same definition. Optional refresh occurs only on committed End Day.

Slot rules in each ten-mission arc:

1. Slots 1–2 are available from day 1; selection among them needs no successful previous mission.
2. Slots 3–5 require **an attempted** slot 1 or 2. A failure still opens this tier.
3. Slots 6–9 require **two successful** missions from slots 1–5. Failed required missions remain reofferable; Analyze/Community and starter missions are never permanently removed.
4. Slot 10 requires successful slots **3, 6, and 9** in its district. These are its service, threat, and readiness milestones; the individual brief explains their meaning. A later site crisis does not erase the historical unlock, but it can change the finale's starting overlay.
5. A Victory unlocks that definition in practice. First attempt also unlocks practice with a visible Uncompleted badge so a stuck player can train without strategic cost. Practice can select any certified difficulty, but changes no live city/account state.

Raid/Breach offers require `C >=40`; below that threshold show them as identified but not deployable and point to Analyze or Recon. Never substitute a random innocent target. Crisis defense/evacuation is not confidence-gated. All family actions have an available free task-force path, including under low Supply readiness.

At most **one new urgent incident citywide per day**, at most two active incidents citywide. An incident has a stable ID, district, kind, warning, and two full End Day opportunities to respond. It nominates an already-defined eligible mission, not a hidden 61st mission. Do not overwrite the district's only recovery offer. Expired incidents resolve once; resolved incidents are removed before new ones are selected. Critical finale prerequisites never expire permanently.

## Tactical consequences

Abbreviations follow the metric table. All unused deltas are zero. This is the baseline **Victory** vector; a mission can additionally set its specified site/route/arc flag.

| Family | Victory delta `(S,T,I,E,C,H,L)` |
|---|---|
| RECON | 0,1,0,-2,18,2,0 |
| PATROL | 10,3,0,-5,4,2,0 |
| RAID | 8,1,0,-14,6,7,0 |
| RESCUE | 3,14,0,-3,3,3,0 |
| ESCORT | 4,6,2,-3,0,2,14 |
| REPAIR | 3,5,18,-2,0,2,6 |
| DEFENSE | 12,5,4,-8,2,3,2 |
| INTERDICT | 7,1,0,-12,6,5,4 |
| SEIZE | 10,2,4,-10,3,5,8 |
| AIRLIFT | 4,10,0,-3,2,4,10 |
| BREACH | 8,1,0,-16,8,8,0 |
| FINALE | 12,8,10,-20,5,-10,10 |

`Partial` applies half of the family's vector, rounded toward zero, plus harm penalties; it does not award Success prerequisites or a finale milestone. `Defeat` applies `S -4, T -2, E +6, H +3`. `Withdrawn` applies `S -2, E +3` plus recorded harm; it is available through a visible confirmation. A completed objective cannot be uncompleted by withdrawing after a terminal result; the result is already frozen. `TechnicalFailure` refunds the committed AP exactly once and applies no district penalties/rewards; it keeps the offer and its immutable snapshot eligible for retry.

Once per attempt add `T -2` per civilian death (cap -20), `T -4/I -5` per destroyed protected service site (up to three counted sites), and `S -1` per 25% of the task force lost (cap -4). Count a passenger death once, including transport loss. A protected site is explicitly authored, never inferred from a building mesh. Report harm regardless of which faction caused it; source attribution is stored for explanation. Mission-specific mandatory protection failure is evaluated separately from these strategic penalties.

Most partial outcomes occur when a deadline arrives or the player chooses to conclude after a minimum objective subset. Each brief supplies a `partial` predicate; if false, the outcome is Defeat. Mandatory objective impossibility, or loss of all commandable mission units, evaluates terminal status immediately. When combat and extraction complete on the same simulation tick, apply deaths/destruction first, then update facts, then evaluate failure, Victory, Partial in that order. Victory requires every mandatory node and the brief's survival floor. No score-based auto-win.

Brief-specific site/route facts reflect what physically happened even on Partial/Defeat. Only an explicit Success flag requires Victory. A recovered evidence object becomes a persistent evidence flag only after successful extraction. A restored site that is destroyed before exit remains Damaged. Later replay never duplicates one-time milestone flags.

## District modifiers at deployment

Capture all modifiers in a signed-off launch snapshot; the city cannot change while that attempt is running. `L <30` removes optional reserves, not the minimum winning task force. `C >=70` reveals one authored approach warning, not enemy entities. `H >=70` selects the authored high-alert reinforcement schedule (same finite enemy budget, earlier warned arrival). `I >=60` opens the district's certified service route if that scenario has one. No hidden health/damage multiplier and no random roll that makes a required objective impossible.

## End Day order (one atomic transaction)

1. Validate revision, AP state, no active attempt, and unconsumed day command token. Freeze the current-day report input.
2. Expire unresolved incidents whose due day has arrived: each applies `S -4, E +4, L -3` in its district once. Service Disruption additionally marks its one nominated service site Damaged; Road Blockade marks its one nominated route Contested; Hostile Pressure has no extra site/route delta. The warning previews these exact consequences. Offer only an incident whose configured response mission has a legal recovery path.
3. Compute district pressure **from the pre-tick snapshot after incident deltas**, simultaneously across all districts: `P = (E>=60 ? 2 : 0) + (H>=70 ? 1 : 0) + (I<30 ? 1 : 0)`. Apply `S -=P`, `L -= (E>=70 ? 2 : 0)`, `E += (S<30 ? 2 : 0)`, `C -=3`, `H -=5`. Adjacency never cascades in-place: if any adjacent district had `E>=80`, add at most `E +1` to this district. Clamp afterward.
4. A completed local finale supplies a permanent local recovery term `S +2, L +2, E -2` each day. It is a stabilization milestone, not immunity to future incidents.
5. Evaluate city stabilization progress; publish its reasoned status. Advance day, refill AP to 3, expire per-day action limits, and choose new incidents/offers using the persisted PRNG state.
6. Append the immutable day report and economy receipt, commit the profile envelope once, then show the report. Save failure leaves the old revision active, disables duplicate submission, and offers Retry Save. Back/Continue only navigates.

Incident choice: rank eligible districts by `2*E + H - S - I/2`, ties by seeded stable ordering. If no district scores above 60, generate no incident. Incident kind depends on a declared site/route tag; no valid compatible mission means no incident. Cooldown is two days per kind/district after resolution. Use a frozen config version so balance updates cannot alter a previously committed report.

## Stabilization, recovery, and rewards

Win the city run when all six local finales have a Victory milestone and **all districts** maintain `S>=60, T>=50, I>=50, E<=35, L>=40` for **two consecutive committed End Days**. No condition on current Intel freshness or Heat is required to finish. Show the exact remaining thresholds and first/second stable day. Failure resets the consecutive-day counter, not earned milestones. Offer archive-and-finish or continue the same city after success.

There is no automatic game-over day and no AP/reward purchase requirement. Any district at a floor still allows Analyze, Community, Patrol and starter task-force deployments. Excess hostile influence cannot lock all recovery missions. A player can voluntarily abandon/archive a run; existing profile rewards and practice records survive. Balance tests must demonstrate recovery from every metric floor using free actions and real winning missions.

Proposed reward baseline: first **account-wide** Victory per mission grants 100 Credits and 50 CommanderXP; every accepted live-run Victory grants 20 Credits, capped at 60 repeat Credits/day; the first end-of-day commit with at least one accepted live Victory grants 20 Credits. Partial/Defeat/Withdrawn/TechnicalFailure/practice grant none. First-clear grants ignore the repeat cap, but count once per mission across new runs. No Command, Campaign stars, Materials/Fuel/Oil, or paid power is awarded by this baseline. Reward tuning is provisional and belongs in `OperationsRewardConfig`.

## Difficulty and ARIA

Recruit, Regular, Veteran, Commander use the same success rules, metric deltas, force budget and resources. Differences are reaction delay, scouting, route selection, coordination and reserve timing; all stay legal under fog and the published warning floor. Start with Regular certification; do not expose an uncertified higher profile. ARIA's tactical Watch and the explicit **Operations Run Watch** scope must be able to win without privileged state, extra AP, free forces, hidden enemy reads, or altered conditions. Strategic Watch acts through the same public offers, costs, confirmations and End Day controls as a human.
