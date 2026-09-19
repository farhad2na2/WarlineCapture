# Unit unlocks and upgrades

Recorded: 2026-09-18. Status: accepted design direction, documented at the owner's request. **Implementation has not started; wait for the owner's next instruction.** Exact costs, durations, upgrade values and producer mappings remain design/balance work.

This document defines the unlock and upgrade direction for the [Skirmish expansion](PLAN.md). It separates match progression from future permanent Campaign/Operations progression.

## Skirmish unit access

All supported military unit types can be inspected in the Skirmish catalog without campaign grinding or purchased stat advantages. Deployment requires the appropriate readiness stage, production facility, resources and available army capacity. Availability in the catalog does not mean the player begins each match able to deploy every unit.

| Readiness stage | Forces | Unlock path |
|---|---|---|
| 1 — Initial force | Rifle squads, gunners, marksmen, basic anti-armor infantry, light vehicles and APCs | Starting Barracks and ground production |
| 2 — Combined arms | Tanks, helicopters, air defense and reconnaissance | Upgrade base readiness and build the relevant facilities, including a Helipad for helicopters |
| 3 — Advanced warfare | Jets, long-range missile launchers and heavy transport | Advanced readiness, Airport where required, and sufficient supply infrastructure |

These are gameplay stages within a match, separate from the implementation packages E1–E7. A development build exposes only the units whose behavior has passed validation. Ground-producer mapping still needs the existing-asset audit; this document does not claim a vehicle factory already exists.

Counters must be obtainable in time. Basic anti-armor infantry is available before tanks become dominant. Air defense must be accessible before hostile attack helicopters can apply pressure; do not make the player build offensive air infrastructure to get a ground-based counter. Recon unlocks only after the shared visibility/intel behavior works.

The recruitment screen shows supported role cards from the start. Unavailable entries explain the next requirement in plain language, such as “Build a Helipad,” with a shortcut to the relevant building or upgrade. Show cost, production time, capacity and prerequisite progress. If several requirements are missing, prioritize the actionable next step and expose the rest in the detail view. No unexplained padlocks or clickable actions that do nothing. Similar appearances remain variants inside a role card.

## Match upgrades

Two upgrade types are included in the initial design:

1. **Facility/readiness upgrades:** unlock advanced unit production or additional production queue capacity. Facilities retain their own explicit prerequisites.
2. **Shared category upgrades:** a small, capped set such as infantry weapons, vehicle protection and aircraft efficiency. Buy with Materials and a visible research timer. Completed upgrades apply to both existing and future eligible units in that faction.

Players do not upgrade individual soldiers one at a time. Repairing vehicles and replenishing squads are separate actions and do not count as research. Upgrades should strengthen a chosen composition while preserving useful counters; repeated upgrades must not make one unit type universally dominant.

Use a compact upgrade surface attached to the relevant production/building interface. Show the current effect, next effect, cost, duration and progress. Completion must produce visible feedback and a correct stat change. Apply each effect once through the unit's base definition and faction upgrade state; replacement units, boarding and reopening UI must not stack the bonus accidentally.

Player and AI use the same prerequisites, costs, timers, capacity rules and effects. Tune the initial upgrade count and numerical effects during the ground slice. A large branching research tree is deferred.

## Reset and recovery

Readiness and category upgrades belong to the current battle. A fresh match or Replay starts from its selected starting conditions. Profile-level unit levels do not carry combat advantages into Skirmish.

When match checkpoints are implemented, resuming the same battle must retain completed upgrades, research progress and associated cost accounting. This is different from starting a fresh battle. Cancellation, research-building destruction and refund rules must be explicitly resolved and tested before release; do not silently lose research or duplicate refunds.

## Setup options

- **Normal start:** begin with the initial force and develop the base during play.
- **Established Base:** optional shorter-session setup beginning at the combined-arms stage. Starting facilities, forces, economy and readiness must agree, and both factions receive the documented starting conditions.
- **Full Arsenal:** Sandbox option for trying all supported units, with clear resource and army-size settings. Unsupported units and uncertified battle sizes remain unavailable.

## Permanent progression in Campaign and Operations

Future Campaign/Operations progression can award unit blueprints through missions or objectives. Blueprints unlock those units in the Armory, with clear requirements and an understandable earned reward. This belongs to the later progression milestone; it does not gate the balanced Skirmish roster.

Individual veterancy and permanent unit levels are deferred until larger battles and the simpler upgrade model are balanced. Their effects, acquisition costs and progression rules have not been approved or implemented by this document.

## First implementation scope

When implementation is requested: deliver the three-stage readiness model, clear facility requirements, and a small set of shared upgrades. Introduce ground requirements/upgrades in the first playable expansion, then attach air and advanced-roster unlocks as their implementation packages pass. Preserve M1–M5 behavior through mode-owned configuration.

Required verification includes exact-once costs/effects, queued and existing units, missing/destroyed facilities, cancellation/refunds, player/AI parity, fresh-match reset, same-match checkpoint recovery, counter availability, EN/FA labels, and mobile readability. The [acceptance matrix](ACCEPTANCE.md) tracks these cases.
