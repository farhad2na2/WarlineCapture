# Delivery packages

Status on 2026-09-18: all implementation packages below are **planned**, not implemented or acceptance-tested. The planning/source-review work is complete. The owner accepted the [unlock and upgrade direction](UNLOCKS_AND_UPGRADES.md) for documentation and will provide the next instruction. Follow [PLAN.md](PLAN.md) and close each package against [ACCEPTANCE.md](ACCEPTANCE.md).

Maintain one issue ledger: `ID | reproduction/seed | expected | actual | severity | owner | narrow fix | impacted modes | evidence | status`. A screenshot, successful method call, or green unit test cannot alone close a player interaction bug.

## Sequence and review gates

| Package | Prerequisite | Concrete deliverable | Required review decision |
|---|---|---|---|
| E0 — baseline and scale probe | None | Reproduce present placement/selection/recruitment paths; record all roster dispositions; deterministic scenario inputs for 50/100/200/350/500-entity probes; exact-build device baseline | Agree the first supported device tier, worst cost, and minimal ground roster. Fix blockers before expanding. |
| E1 — match definitions and roster | E0 | Versioned preset/roster definitions, typed roles/counters, generic population reservation, producer mapping, five differentiated infantry roles and car/APC/tank | Complete production → delivery → selection → combat → death for each entry, with correct costs/refunds. |
| E2 — army control and movement | E1 | Stable squads/groups, large touch cards and Army panel, multi-select, rally points, compatible mixed orders, formation/traffic behavior | Move and fight a roughly 100-unit two-sided ground battle through real mobile controls; no stranded squads or precision-tap requirement. |
| E3 — Standard ground battle | E2 | Expanded authored layout, sufficient supply throughput, counter-producing AI, ground Base Assault at Standard size | Full EN/FA matches with several viable compositions; understandable production/shortages; target-device Standard gate. |
| E4 — visibility, helicopters and anti-air | E3 | Shared intel/target visibility, recon/Scan integration, helicopters, anti-air, batch transport and fuel/return behavior | Ground counter available before hostile air; no hidden-target leaks; full mixed match and 200-entity engineering gate. |
| E5 — War and Frontline Control | E4 | War economy/cap/map validation, three-zone objective and tickets, multi-front AI, result/replay and long-session checkpoint package | Real 200-plus-unit scenario and sustainable device play; players can explain why they win and where to act; recovery accepted before making War the default. |
| E6 — advanced roster and Large War | E5 | Jets, siege, transport planes, remaining suitable variants/roles and larger layout | Every catalog entry has a final disposition; up to 340-unit preset passes supported-device and 30-minute tests. |
| E7 — optional teams and Sandbox | E5; E6 for Large War size | Generalized team relations/objectives, 2v2 AI option within certified total cap, army Sandbox | No friendly targeting, faction resource leakage, asymmetric knowledge, or misleading unsupported setup choices. |

E0 begins performance work immediately. Within each package, measure the touched system and make focused improvements. Do not defer all scale work until E6, and do not do a wholesale engine rewrite before a richer small battle is playable. All feature switches default to the accepted existing preset until their new configuration is deliberately selected.

## E0 — establish facts before enlargement

- **E0.1:** Pin code/config hashes and isolate QA saves. Recheck mountains, roads, full rotated footprints, buildings selectable with feedback, previews matching final spawn, recruitment deliveries and camera interruption, exchange balances, terminal voices, both languages.
- **E0.2:** Create the roster ledger described in [BASELINE.md](BASELINE.md). Flag duplicated names/portraits, noncombatants, missing producers, missing counters and unimplemented advertised abilities.
- **E0.3:** Build a reproducible stress scene/preset using real unit definitions, AI, projectiles and map geometry. Establish separate warmup, idle, mass move, dense combat, air/transport and destruction phases. Test both spread-out armies and concentrated visibility. Report actual counts, not requested spawn counts.
- **E0.4:** Profile Editor diagnostically and Android builds on identified devices. Use the existing CI wrappers and performance contracts. Rank CPU, GPU, memory, path-queue and allocation issues by measured contribution.
- **E0.5:** Select the first five infantry roles and three vehicle roles. Produce the cost/production/fuel spreadsheet or data table, reachable layout sketches, and the producer mapping before implementing E1. No availability claim based on names alone.

## E1–E3 — first playable expansion

**Configuration and accounting:** selected session owns immutable preset/map/roster/balance/objective IDs and versions. Defaults migrate old saved setup to the supported small preset. Player and AI call the same eligibility and reservation policy. Reserve cost and Supply once on enqueue; transfer to in-transit/deployed counts; cancel, producer death, delivery death and failed landing have explicit refund/requeue behavior. No double count for embarked passengers, no leaks after Replay.

**Role data:** author target domains and damage/armor classes only as required by the first counters. Do not infer abilities from prefab names. Keep campaign configuration intact through Skirmish-specific balance profiles. A rifleman, marksman, gunner and rocketeer must remain distinguishable after runtime tuning.

**Unlocks and upgrades:** E1 defines the three readiness stages, explicit facility requirements and faction upgrade state. E3 delivers the ground unlock path, compact facility/category upgrade UI, Materials costs, research timers and fresh-match reset. E4/E6 attach their air and advanced-unit requirements. Existing and newly produced units receive completed effects exactly once; AI follows the same rules. Resolve cancellation/destruction/refund behavior before accepting research. Keep counter access ahead of the threat. Established Base is added only when its complete starting configuration works; Full Arsenal follows with Sandbox. Permanent blueprint progression remains a later roadmap milestone.

**Army interface:** squad/group model is separate from screen slot. Retain IDs when members die, board, disembark, or reinforce. Group assignment, removal and pinning are reversible. Selected groups stay visible when paging the Army panel. Logistics units are excluded from a normal combat-group select unless explicitly chosen.

**Movement:** test current scheduler first; implement shared corridor planning or formation improvements only where measured. Preserve manual-order priority over autonomous retargeting, release old goal reservations, and invalidate paths after obstruction/destruction. Give blocked routes a visible reason and alternate-route behavior rather than indefinite waiting.

**Economy:** demonstrate enough Materials/fuel and producer capacity for the Standard roster during a normal match. Helicopter presentation may batch delivery, but units cannot appear without the paid quantity being delivered. Losses can be replenished; destroyed supply can be rebuilt on valid reachable ground. Vehicles never permanently occupy producer slots after leaving.

**AI:** build a composition planner around the existing production/orders. Keep a reserve, respond to observed armor, select a reachable route, and gather reinforcements into purposeful groups. Order cooldowns/hysteresis prevent target thrashing. Record actual spending and production instead of tuning free waves.

E3 is the first user review build. It should already feel materially richer than the 24-infantry prototype. Do not wait for jets or a 340-unit army to demonstrate value.

## E4–E6 — complete the large-war loop

**Information contract:** visibility/intel is shared by picking, attack acceptance, auto-acquisition, minimap, warnings and AI. Last-known position is separate from a live target reference. Destruction removes live markers immediately; any permitted stale intelligence is visibly different and expires. No omniscient AI economy/counter-building under fog.

**Air contract:** define ground/air targeting, attack pass and return states, fuel exhaustion, safe idle orbit, landing reservation, boarding interaction, and carrier loss. A transport request chooses only compatible passengers and reports excess capacity; selection must work when passengers overlap the aircraft. Ground armies get viable anti-air and warning before air pressure escalates.

**Frontline:** scenario metadata owns three zones, timings and tickets. UI and simulation read one state; result timing and simultaneous outcomes are authoritative. Tests include contested capture, boarded infantry, death on the final tick, surrender/deadline, empty points, destroyed base, and result freeze. AI distributes groups across zones and protects supply instead of sending everything at the main base.

**Long sessions:** implement and validate a versioned match checkpoint before promoting War or Large War as the mobile default. Store stable unit/building/order/queue/visibility/objective data and remaining timers, never raw ECS entity IDs. Restore atomically into a fresh session, rebuild references and reservations, preserve exactly-once cost/result accounting, reject incompatible checkpoints with a clear safe return. Test suspend, OS termination, interrupted writes, upgrade migration and delivery in flight. Until accepted, disclose setup-only recovery in the setup/help flow and keep shorter presets primary.

**Remaining roster:** audit every unexposed asset again after core systems mature. Make matching military appearances available through variants; place crew/civilians/scenario specialists in appropriate scenario or Sandbox roles. Each advanced unit must have useful counterplay, correct render/animation/fuel behavior, and a working producer. Do not add nominal ability buttons to close the ledger.

## Change discipline

- Change source and mode data in reviewable packages; use Unity builders/Editor APIs for assets.
- Preserve the accepted small preset and independent M1–M5 configs. Shared selection, pathing, transport, economy and rendering changes trigger affected campaign checks.
- Run focused checks, real UI/world outcomes, and representative full matches. Record whether a run used injected armies, skipped setup, or accelerated time.
- Keep gameplay QA and stress testing separate. Never change health, money or objective state during a claimed normal playthrough.
- New features ship only when supported setup controls, field guide, EN/FA text, feedback and result behavior are complete.
- Update evidence/status at each gate. Commit at stable boundaries when requested or part of the agreed implementation workflow; this planning task does not commit or launch implementation.
