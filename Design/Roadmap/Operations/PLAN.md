# Operations: mode and mission production plan

Recorded 2026-09-21; implementation status clarified 2026-09-22. **Design and implementation handoff.** P0–P4 now provide Operations-owned contracts, strategic/tactical/loop models, O001–O003 graphs and Play Mode capture tooling. Shipping UI, shared gameplay and durable profile integration are still incomplete; a model/capture win is not player readiness. The next delivery is [P4R: O001 player-ready implementation](O001_PLAYER_READY_IMPLEMENTATION.md), which closes the original integration requirements before content expansion. See the [review evidence](../../AgentReports/Operations/O001_READINESS_REVIEW_20260922.md).

This package targets the existing **Operations game mode** on SCN-11/SCN-12. Working district/mission names are supplemental fiction, subject to narrative and EN/FA copy review. See [P0_BASELINE_RECHECK.md](P0_BASELINE_RECHECK.md), [P0_SHARED_SEAMS.md](P0_SHARED_SEAMS.md), and [P0_SHADOW_PROJECT.md](P0_SHADOW_PROJECT.md). Windows Operations Unity work uses `D:\Projects\WarlineCapture-Operations`; all platforms follow current root AGENTS.md and project-ownership rules.

## Recommended scope

Environment sourcing amendment (2026-09-21): the [Demo 2 asset reuse plan](../../Demo2_Asset_Reuse_Plan.md) selects warehouse/logistics/utility art for D03 Industrial Belt and bridge/quay pieces for D04 River Crossing, with restrained service/perimeter accents in D02/D05/D06. D01 keeps the established desert architecture. Reuse is planned, not accepted gameplay; preserve O001–O060, both independent land crossings, existing feature limits and all mission gates. No naval or structural bridge-collapse mechanic follows from available art.

Plan **60 individually authored tactical missions: six district arcs of ten missions**. Build them with **12 reusable mission families**, six district map layouts, and a persistent city simulation. Add six abstract command actions, which are **not counted as missions**. Difficulty settings, seeds, repeat appearances, failed-attempt retries, practice replays, and alternate outcome branches do not increase 60.

Skirmish's [120 planned scenarios](../Skirmish_Expansion/BATTLE_CATALOG.md) combine maps/objectives/armies/starts. Operations earns its variety through intelligence, civilian protection, infrastructure, changing district conditions, and consequences that shape subsequent deployments. Doubling an authored mission by changing weather or enemy health would not justify a second catalog entry. Sixty provides six substantial district stories without requiring 120 separate narrative/consequence/repair-path contracts before the mode is coherent.

Treat 60 as a **full-mode planning target**, not the first playable delivery or a promise of release readiness. First prove a three-mission loop, then 12, 30, and finally 60. Additional districts can follow measured player demand; no expansion is counted in this package. A rejected mission must be redesigned or replaced under an explicit content revision, not padded with a trivial variant.

## Read this handoff in order

For environment work, also read the [Demo 2 integration guide](../../Demo2_Asset_Integration_Guide.md) and [source/output manifest](../../VisualConfigs/Demo2_Environment_Asset_Manifest.json). P2/P6 and the district briefs now include the adaptation assignments; use the guide's D2-A01–A04 work items and D2-V1–V6 gates alongside the mission handoff.

| Document | Responsibility |
|---|---|
| [BASELINE.md](BASELINE.md) | Observed code, actual reusable seams, gaps, and conflicts to avoid |
| [STRATEGIC_RULES.md](STRATEGIC_RULES.md) | City state, days, offers, costs, consequences, recovery, and completion |
| [ARCHITECTURE.md](ARCHITECTURE.md) | Classes/components/systems, assemblies, payloads, persistence, lifecycle |
| [MISSION_IMPLEMENTATION.md](MISSION_IMPLEMENTATION.md) | Objective vocabulary, families, force packages, map/AI/ARIA contracts |
| [MISSION_CATALOG.csv](MISSION_CATALOG.csv) | Exactly 60 stable planning entries and machine-readable references |
| [District briefs](Missions/README.md) | Every mission's player intent, ordered implementation, branches, tests |
| [DELIVERY.md](DELIVERY.md) | Bounded coding packages, dependencies, ownership, and agent instructions |
| [O001_PLAYER_READY_IMPLEMENTATION.md](O001_PLAYER_READY_IMPLEMENTATION.md) | Immediate P4R integration sequence, manual-playable and release gates, ownership and evidence |
| [ACCEPTANCE.md](ACCEPTANCE.md) | Catalog, simulation, interaction, save, localization, and device gates |

The CSV is the identity/index inventory; a mission's district brief owns its objective parameters. Shared family behavior comes from MISSION_IMPLEMENTATION; city rules from STRATEGIC_RULES. All numeric tuning in this package is an **initial test specification**, not measured balance. If a brief needs a different rule, amend the owning shared contract and its tests before implementing the exception.

## The player experience

Inspect the city, select an urgent district problem, see costs and likely consequences, deploy into a 3D operation, return with concrete results, choose another action, and advance the day when ready. Short briefings explain who needs help, what success requires, and what is at risk. Results show both tactical facts and district changes with causal explanations.

Operations is a finite, replayable stabilization run with an optional continuation after success. Time advances only when the player ends the day. No offline decay or real-time punishment. A full run targets **20–35 in-game days**, roughly 30–45 tactical deployments depending on choices and retries; the entire 60-mission library is encountered over multiple runs or through practice unlocks. The prerequisite graph requires at least five successful missions per district, so 30 is the minimum successful-deployment count. This is a pacing hypothesis to test, not a guarantee. Most missions target 8–15 minutes; finales 15–20. Sessions can stop at the dashboard or a certified tactical checkpoint.

The six arcs are concurrent command problems, not six Campaign chapters. All districts are visible from run start; operations become available through their own prerequisite and state rules. Completing one district does not lock the others. Successful local finales create durable stabilization milestones; unresolved crises elsewhere remain visible.

| District | IDs | Tactical identity | Persistent question |
|---|---|---|---|
| D01 Old Quarter | O001–O010 | Narrow lanes, courtyards, short civilian routes | Can the player establish trust while identifying real threats? |
| D02 Civic Center | O011–O020 | Public buildings, broad plazas, exposed service corridors | Can essential services stay open under pressure? |
| D03 Industrial Belt | O021–O030 | Depots, freight lanes, separated repair sites | Can logistics support security without destroying infrastructure? |
| D04 River Crossing | O031–O040 | Two land crossings, quays, constrained vehicle routes | Can access and evacuation survive contested crossings? |
| D05 Highland Approach | O041–O050 | Switchbacks, observation ridges, isolated service nodes | Can limited forces keep remote routes connected? |
| D06 Airport Perimeter | O051–O060 | Runways, hangars, perimeter roads, landing zones | Can air access recover while ground threats remain? |

These are six planned authored layouts. Reuse terrain/art/roads and certified gameplay modules from Campaign/Skirmish where suitable. Identical physical maps retain their existing map ID; a genuinely different district layout earns a new one. Do not count six skins of one unchanged route graph as six completed battlefields.

## Mission mix

The 12 families are Recon, Patrol, Raid, Rescue, Escort, Repair, Defense, Interdict, Seize, Airlift, Breach, and Finale. They compose a small set of objective rules; they are not twelve independent combat engines. Each mission has a distinct target/route/protection/timing decision and at least two viable approaches. Each district finale combines previously taught systems and resolves its local service/security problem.

The first three missions prove observation, delivery, and restoration with district consequences. Later missions combine limited transport, timed threats, repair under pressure, evidence protection, holding routes, and air access. There are no mandatory naval, stealth, interrogation, civilian-conversation, structural bridge-collapse, or fully simulated electrical-grid mechanics in the 60-mission scope. Flavor involving these subjects uses the explicit supported interactions described in the briefs.

## Progression and fairness

Use a free scenario-provided task force for each mission; baseline completion must not depend on account purchases or grinding Campaign. Readiness changes reserve access and support options within authored limits. Permanent account upgrades cannot silently change the certified scenario baseline. Failure loses an opportunity and worsens a district; it does not delete the account roster or mandatory story access.

District milestones unlock mission offers, optional local reports, practice missions, and modest account rewards. Do not grant Campaign stars or hide Campaign revelations here. Results distinguish completed objectives from collateral and rescued people. A victory with avoidable harm may be strategically worse than a careful partial result.

ARIA acts as a staff officer: explains likely district consequences, uncertainty, and available legal commands. Tactical recommendations use the same visible objective/route/role state as the player. Autonomous strategic spending, ending days, or choosing permanent consequences requires a separate user-facing product interaction; it is not silently bundled with tactical Watch mode.

## Scope and dependencies

The original planning task produced documentation only; the implementation status above reflects subsequent P0–P4 work. Continue with P4R's shipping integration gates. Existing M01–M05, the small Skirmish baseline, and concurrent Skirmish expansion work must remain intact. Operations does not depend on all 120 Skirmish scenarios, jets, or 340-unit battles. It does depend on certified shared selection, movement, combat, transport, visibility, map readiness, and saving for whichever missions use them.

Architecture authority: [SOLID/ECS contract](../../Architecture/gameplay_solid_ecs_contract.md), [map identity](../../Architecture/operation_map_and_scenario_identity_contract.md), [economy](../../Economy_Reward_Design.md), [product GDD](../../AAA_Mobile_Game_Design_Document_v0_2.md), and [content grammar](../../Gameplay_North_Star_And_Content_Grammar.md). This plan refines Operations; it does not override those shared contracts. Feature presence in source is not feature certification.
