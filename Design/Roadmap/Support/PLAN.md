# Support rollout: Campaign, 120 Skirmish battles and 60 Operations

Updated: 2026-09-28. Owner approved the four-ability design and requested this cross-mode roadmap. Status: **planned implementation; no runtime or mode acceptance claimed**. Counts remain 25 authored Campaign missions, S001–S120 and O001–O060. Support adds choices inside these entries; it does not create new missions.

## Shared system and usage limits

Build the [technical handoff](../../AgentReports/SupportSystem/IMPLEMENTATION.md) and [T00–T12 tasks](../../AgentReports/SupportSystem/TASKS.md) first. Use the approved full-screen Build-style popup, existing game models, exclusive targeting and explicit confirmation. Scan/Radar remains separate. Every ARIA call needs exact one-action consent, including Watch mode; a no-response proposal cannot silently execute or stall the objective planner. An enemy AI uses legal runtime requests without a human consent dialog, with exactly the same costs, visibility, cooldowns and target rules.

| Ability | Maximum per attempt, per faction | Fuel per call | Timing / effect |
|---|---:|---:|---|
| Smoke Screen | 2 | 1 | 35s between uses; 12m radius, 15s, direct ranged damage ×0.65 |
| Precision Strike | 1 | 4 | 3s approach; 250 raw damage to one confirmed hostile military target |
| Paratroopers | 1 | 6 | One canonical rifle squad; 5s approach plus existing descent |
| Supply Drop | 1 | 3 | 40 tactical Materials; 5s approach plus descent; explicit ground collection |

No automatic recharge, paid refill, per-wave reset or difficulty/army-size charge multiplier. All four together allow five calls costing 15 Fuel. Fresh attempts receive the authored allocation and normal starting resources; a supported resume restores consumption and receipts. A district change, pause, popup reopen, capture or save/load never refreshes a running attempt. These are initial balance inputs, not playtest certification.

Use one ECS executor and per-mode policy adapters. The CSVs below are planning inputs keyed to existing stable IDs; import them into versioned authored policy assets after review/validation, never read Design files in the player. Missing policy denies Support. Per-entry publication requires certified shared ability + certified mode adapter + entry allow-list + current readiness/target/resources; a CSV row is not an enable switch.

## Campaign: teach, reinforce, combine

[CAMPAIGN_SUPPORT.csv](CAMPAIGN_SUPPORT.csv) maps all 25 current mission identities to the already approved [defaults](../../AgentReports/SupportSystem/support_defaults.json).

- CH01–CH03 and CH04-M01/M02 gameplay: hidden. CH04-M02 settlement unlocks Smoke only after it is usable in the next mission.
- CH04-M03: optional Smoke lesson; settlement unlocks Strike.
- CH04-M04: optional Smoke/Strike; retain owned-transport lessons; unlock Paratroopers instead of Harbor Scan.
- CH04-M05 and CH05-M01: combine those three, with bounded ARIA consent. No Naval Fire reward or additional Strike charge.
- CH05-M02: use those three and unlock Supply Drop; Rally stays outside this consumable system.
- CH05-M03: optional Supply lesson; CH05-M04 reinforces it; CH05-M05 combines known tools with no new unlock/mechanic.

All missions remain completable without Support. Future Campaign chapters beyond the current 25 must author explicit allowances and resource budgets, reuse these four proven mechanics, and introduce any future mechanic before a finale. No automatic retroactive exposure in early missions, no invented chapter IDs and no reward for an unimplemented ability. This schedule supersedes older Support reward/exposure prose while T01 synchronizes canonical balance and chapter assets.

## Skirmish: all 120 entries receive explicit policies

[SKIRMISH_SUPPORT.csv](SKIRMISH_SUPPORT.csv) joins one-to-one with [SCENARIO_CATALOG.csv](../Skirmish_Expansion/SCENARIO_CATALOG.csv). Preserve catalog IDs, map/objective/army/start combinations and all 120 counts. Proposed standard profiles:

| Army profile | Authored Support options | Match gate |
|---|---|---|
| G — Ground Maneuver (40 entries) | Smoke, Supply | Smoke R1; utility Supply R2 after a valid logistics/air-route setup |
| A — Air Mobile (40 entries) | All four | Smoke R1; Paratroopers/Supply R2; Strike R3 |
| C — Combined Arms (40 entries) | All four | Same as A |

Ground utility delivery does not grant a commandable aircraft or offensive air role. Off-map flights do not require buying a player-owned jet/runway; the readiness gate preserves the existing counter window, and route safety still applies. They cannot bypass known AA. Grants come from the scenario, never Campaign progress, account purchases or enemy difficulty cheats. Field and Established starts retain their existing readiness/economy; no free extra Fuel or Materials to make all calls affordable. Available charges become usable when the gate is met and never replenish when readiness changes.

Both factions receive identical ability ceilings and gates, including asymmetric BT/CE roles; role-specific objective forces remain unchanged. Enemy decision quality may vary with difficulty, its information/resources may not. Use at most two simultaneous automatic Support aircraft per side, sharing the existing carrier budget rather than creating a second budget. Reinforcements consume alive+queued+reserved Supply and infantry caps; the presentation carrier uses bounded support accounting.

BA/FC must not turn a Strike into direct victory or a Supply crate into passive income. BT designated breakthrough units remain the only scoring set: delivered rifles cannot become designated survivors. CE Support never spawns/replaces an objective convoy truck or rescues an already lost objective by editing state. These constraints apply to every map. Recertify existing S001/S025/S073 mappings when their policies change; old wins are not new Support evidence.

## Operations: all 60 entries receive bounded mission grants

[OPERATIONS_SUPPORT.csv](OPERATIONS_SUPPORT.csv) joins one-to-one with [MISSION_CATALOG.csv](../Operations/MISSION_CATALOG.csv) by mission_id/code. Every mission has a proposed optional profile. Final mission briefs may narrow it with a recorded reason; no silent expansion beyond the four abilities. No Campaign unlock requirement and no account wallet payment.

| Mission family | Planned maximum options |
|---|---|
| RECON, PATROL, RESCUE | Smoke |
| ESCORT, REPAIR | Smoke, Supply |
| RAID, INTERDICT, BREACH | Smoke, Strike |
| DEFENSE, AIRLIFT | Smoke, Paratroopers, Supply |
| SEIZE | All four |
| FINALE | Union of eligible tools already introduced in that district; CSV is an upper ceiling, not a first-use grant |

Operations arcs are concurrent: do not use O-number order as an unlock chain. Show a short optional first-use explanation when an available non-finale mission introduces a tool. A finale permits only the district's previously introduced tools, tracked in run state; declining or skipping use does not block introduction, missions or success. Record that state through the normal Operations save boundary and certified checkpoint/retry semantics. Practice derives an explicit profile from the authored policy and changes no live-run state.

Mission authoring must bind a real tactical Fuel source and Materials storage to the task force where Support is offered. Do not invent a conversion from district Supply readiness, Supplies/Command/account resources into Fuel. If the current mission cannot represent that tactical economy, its adapter dependency stays open and Support stays disabled. Existing district readiness overlays may narrow grants only through explicit versioned mission policy with briefing reasons; no new hidden numerical readiness tax is introduced here.

Strike requires positive military target confirmation and protected/civilian exclusions. No area bombing, objective-health override, free reveal or civilian suppression. Smoke affects both factions and does not change identification or protection rules. Paratroopers are ordinary rifles; they cannot replace named rescuers, engineers, evidence teams or evacuees. Supply grants only tactical Materials: never Fuel/Oil, a delivered medicine/water objective, city readiness, account rewards or district recovery credit. Results flow through existing consequence settlement once; Support itself adds no strategic reward/penalty. Spare charges and crate stock do not export to the city wallet.

## Ordered extension tasks after the shared Campaign slice

| Task | Dependency | Concrete deliverable and exit |
|---|---|---|
| X01 — shared mode boundary | T02–T04 contracts; airborne tasks before their exposure | Generalize attempt identity/faction and versioned policy input without duplicating executor; no Campaign singleton dependency. Bind mode visibility/protection, physical Fuel, Materials, population, air anchors and cleanup. Test missing adapters deny access, faction isolation and no cross-mode leakage. |
| X02 — Skirmish pilot | X01; relevant T07–T10 acceptance | Bind one G and one A/C candidate, readiness gates, both-side AI and carrier caps. Prove manual and ARIA use/decline, enemy legal use, with/without-Support full wins and result return; BT/CE objective integrity tests before those families. |
| X03 — Skirmish coverage | X02 | Import/review 120 policies in the existing map batches. Add Support evidence per entry/difficulty/size matrix required by existing acceptance; publish only certified entries. No 120 acceptance claim from two pilots. |
| X04 — Operations pilot | X01; O001 real player path; relevant ability acceptance | Use O001 Smoke and O002/O003 Supply where actual resource adapters permit; then one military-target mission and one reinforcement mission. Preserve Scan, district consequences, retry/checkpoint and practice isolation. |
| X05 — Operations coverage | X04 | Extend existing 3 → 12 → 30 → 60 cadence. Audit all family profiles, air routes, protected targets and finale prior-introduction rule. Full mission/result/dashboard and city-run evidence remain required. |

Campaign, Skirmish and Operations keep their existing content workstreams. Operations does not wait for all 120 Skirmishes. Features may be certified incrementally, but mark any unfinished approved ability or content coverage honestly. Do not make Support a prerequisite for releasing an otherwise accepted no-Support baseline; recertify when it is added.

## Evidence and publication

For every exposed entry record policy/catalog version, enabled abilities and grants, map/route/target bindings, tactical resource budget, native EN/FA layouts, automated request/resource/cleanup checks, normal-input Support and no-Support paths, ARIA approval/decline/Stop, result/return, and mode save/retry evidence. Existing difficulty/seed/size and complete-win requirements remain in force. Keep design approval, automated results, native visual review, full normal-input play, real-player and device acceptance separate. No runtime tests were run for this roadmap-only update.
