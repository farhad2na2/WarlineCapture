# Skirmish expansion: combined arms and large battles

Updated: 2026-09-21. Status: expanded scenarios remain **Planned**, not runtime-accepted. The [implementation handoff for battles 4–120](IMPLEMENTATION_HANDOFF.md) adds a fresh source audit, exact class/asset contracts, fourteen programming packages, and individual briefs for all 120 stable catalog IDs. Three prototype mappings now exist in source; their evidence does not certify expanded scenarios.
Historical source review: `c69865400` on `codex/m03-radar-warning`; September 21 inspection includes current uncommitted Industrial Basin/library work and is explicitly source-only.

This plan answers the request to use the wider roster and fight battles involving hundreds of units. It follows the small [Base Assault prototype](../Skirmish_Prototype/COMPLETION.md) and changes the [roadmap](../README.md) priority: prove a richer, scalable Skirmish before investing in progression or more game modes. Historical prototype acceptance is not acceptance of this expansion.

Detailed catalog: [120 battles and path to 200](BATTLE_CATALOG.md), [five map briefs](MAPS.md), [difficulty/starting packages/economy](MATCH_SETUP.md), [enemy AI and ARIA](AI_AND_ARIA.md), [all 120 candidate IDs](SCENARIO_CATALOG.csv). These supplements refine this plan; explicit September 21 implementation corrections supersede conflicting older proposals. Numerical costs/timings remain balance-test inputs. [Operations](../Operations/PLAN.md) has a separate plan and scope.

For programming, start with [TECHNICAL_ARCHITECTURE](TECHNICAL_ARCHITECTURE.md), [AGENT_WORK_PACKAGES](AGENT_WORK_PACKAGES.md), [all battle packets](Scenarios/README.md) and the [117 remaining work items](WORK_QUEUE_004_120.csv). Existing prototypes are S001/S025/S073; work ordinals 4–120 are not runtime indices or a renumbering of S-IDs. Every released entry requires ARIA to play and win through normal visible controls.

Companions: [source audit](BASELINE.md), [delivery packages](DELIVERY.md), [acceptance and performance gates](ACCEPTANCE.md), [unit unlocks and upgrades](UNLOCKS_AND_UPGRADES.md).

The unlock/upgrade direction was accepted for documentation on 2026-09-18. Later that day the owner requested the [Watch ARIA play plan](../Aria_Demonstration/PLAN.md) before expansion implementation. This expansion remains unstarted. Its new group, upgrade, air and objective mechanics must extend ARIA's shared skills and coverage as each package becomes playable.

## Catalog target clarified with the owner

Environment sourcing amendment (2026-09-21): follow the [Demo 2 asset reuse plan](../../Demo2_Asset_Reuse_Plan.md). Pilot a warehouse/logistics/utility module in an isolated Industrial Basin candidate, then consider perimeter/service accents for the other four maps. This fits the existing five-map scope and does not change S001–S120, roster commitments or the gameplay delivery order. Models are not new unit roles; geometry and presentation changes must earn the existing map and scenario acceptance.

The requested scale is **100–200 selectable battles on a few maps**, not twelve battles and not merely hundreds of units. Target **120 accepted scenarios on five maps**, with a planned extension to 200. The initial matrix is five maps × four objectives × three army configurations × two starting packages. Seeds, four difficulty levels and army sizes are replay options, not extra scenarios. Each combination must earn acceptance through distinct tactical decisions and complete systems; the matrix alone does not make content playable.

The five maps are Desert Base, City Crossroads, Mountain Pass, Industrial Basin and Airfield Plains. Objectives are Base Assault, Frontline Control, Breakthrough and Convoy Escort. Army profiles are Ground Maneuver, Air Mobile and Combined Arms. Starts are Field Base and Established Base. Forty of the 120 candidates use the complete Combined Arms roster. Four difficulty levels are Recruit, Regular, Veteran and Commander, with shared resources/stats and different legal strategic behavior. See the companion specifications for exact starts, outcome rules, caps, routes and acceptance.

Two existing introductory Base Assaults evolve into the first map equivalents; preserve their old snapshots for regression, not extra catalog count. The floating terrain shelf reported near the City Crossroads base remains an explicit open map defect. Current wins must not be presented as complete visual QA.

## 1. Recommended experience

Implement environment adoption through the [practical guide](../../Demo2_Asset_Integration_Guide.md), [verified source manifest](../../VisualConfigs/Demo2_Environment_Asset_Manifest.json) and SK-11 tasks. The guide gives exact prefab sources/proposed destinations, Editor steps, ownership rules, scoped build limitations and D2-V1–V6 acceptance. Keep environment adaptation separate from roster capability and per-scenario publication.

Build a mobile combined-arms RTS: recruit specialist squads, deploy armor and aircraft, maintain supply, take useful ground, and coordinate attacks on several fronts. The player should be able to command a large army through a few stable groups, see why an order succeeded or failed, and recover from a lost fight.

The first expansion is one player versus one AI. The intended main scale is **about 200–240 simultaneously active combat units across both sides**, with a **300–350-unit large-battle preset** after performance validation. These are proposed targets, not demonstrated capacity. Later 2v2 support divides an already tested total battle budget among four factions before attempting a bigger total.

Keep Base Assault as the first expanded ruleset, then add **Frontline Control**, **Breakthrough** and **Convoy Escort** with reusable objective policies. A Sandbox follows for trying every supported unit and custom army composition. Persistent district consequences remain an Operations responsibility.

## 2. What “hundreds of units” means

One soldier, tank, helicopter, or other independently simulated combat platform counts as one combat unit. A squad card is a command convenience, not an extra unit. Embarked soldiers count once. Dead bodies, projectiles, buildings, and background civilians do not inflate the advertised battle count. Track delivery aircraft and supply vehicles separately, including their rendering and simulation costs.

Initial target configurations for 1v1:

| Preset | Infantry per side | Ground combat/transport vehicles per side | Tactical aircraft per side | Combat units across both sides | Match target | Supply ceiling per side |
|---|---:|---:|---:|---:|---|---:|
| Standard | 48 | 8 | 2 | 116 | 12–18 minutes | 128 |
| War | 96 | 12 | 4 | 224 | 18–25 minutes | 200 |
| Large War | 144 | 20 | 6 | 340 | 20–30 minutes | 320 |

These are composition ceilings, not automatic starting armies. Begin Standard with roughly 12 infantry and two light vehicles per side, then tune growth through real recruitment. War needs a larger established base and a stronger opening force; multiplying the old 24-unit limit alone is insufficient. Test full-cap fights separately from normal matches.

Proposed Supply costs: infantry 1, light vehicle/APC 4, heavy armor or missile launcher 6, aircraft 8. Supply means army capacity, not another spendable resource. Use explicit catalog data; revise exceptional costs during balance tests. Both Supply and per-category ceilings must allow an order. Show one clear Supply total in the header; explain category limits on the affected unit card. Queue reservations, aircraft in delivery, and passengers cannot bypass capacity. Reducing capacity never deletes a surviving army; it suspends new recruitment until capacity returns. Start with a fixed preset ceiling, avoiding an additional supply-building mechanic in the first slice.

Initial support ceilings are 6/8/10 supply vehicles per side for Standard/War/Large War and at most two active delivery aircraft per side, all subject to throughput tests. At most 20/30/40 player-built structures per side, with separately bounded barrier segments, prevents construction spam from evading the battle budget. Final limits must support the measured economy.

Choose the largest **certified** preset appropriate for the device before deployment. During a match, reduce cosmetic load if necessary; never remove units or alter either side's combat rules to recover frame rate.

## 3. Roster: meaningful roles before every model

The repository contains 51 UnitGrid configs and 23 BuildingDefinition configs, including civilians and visual variants. That is an asset inventory, not 51 finished combat roles. Every eligible military entry needs a recorded producer, cost, target capability, counter, movement/transport behavior, usable portrait, localization, and performance status.

| Release group | Existing candidates | Intended job and necessary counterplay |
|---|---|---|
| Ground core | Riflemen, Heavy Gunner, Marksmen, Assault Breacher, Ghillie Rocketeer | General infantry; anti-infantry fire support; long-range pressure; close assault; anti-armor. Their range, damage type, targeting, and survivability must actually differ. |
| Ground vehicles | Light Armored Car, Fast/Armored/Heavy APC, Battle Tank | Flank and suppress; transport; protected assault. Rocketeers and maneuver must offer useful answers to armor. |
| Air and air defense | Light/Attack Helicopter, Air Missile Launcher | Mobile fire support with exposed approaches; a reliable ground counter available no later than attack aircraft. |
| Recon and transport | Radar Tank, Satellite Dish, Recon Drone, Transport Helicopter, Canopy Truck | Detect/classify threats; carry squads around route constraints; reinforce or extract. Recon waits for a working information model. |
| Advanced air and siege | Strike/Fighter Jet, Ground Missile Launcher, Transport Plane | Strike, air interception, defended-base pressure, heavy delivery. Validate acquisition, attack passes, refueling, landing/runway requirements, and visible counterplay. |
| Extended roster | Contractors, remaining military variants, Bomb Suit Specialist, opposing irregular roster | Add only with an honest role or selectable appearance variant. A bomb suit description does not establish a working disposal ability. |

All validated standard roster entries are available in Skirmish without campaign grinding or purchased stat advantages. Match buildings, resources, and three readiness stages determine what can be deployed; unavailable cards explain their requirements. Facility upgrades unlock capabilities, while a small set of Materials-funded category upgrades improves existing and future eligible units. These reset for a new match. See the accepted [unlock and upgrade rules](UNLOCKS_AND_UPGRADES.md). Alternative appearances live inside the role card's detail picker so the recruitment screen stays short. Preserve existing catalog names; do not silently give civilian assets military roles.

Every existing unit/building receives a disposition: standard roster, role variant, alternate-faction roster, scenario/Sandbox-only, or blocked by a named missing behavior. “All units” means a route to using the suitable roster, not dozens of interchangeable buttons or unsupported abilities presented as working.

Ground production uses existing request/delivery systems. The producer decision is now specified in [ROSTER_AND_ECONOMY_IMPLEMENTATION](ROSTER_AND_ECONOMY_IMPLEMENTATION.md): Barracks for infantry, a new modular Ground Staging prefab for vehicles and a separate logistics queue, Helipad for helicopters, Airport for jets/heavy transport, and an expanded intel-station drone pad. Never publish a vehicle card until its producer and delivery route are implemented and explained.

## 4. A battle with decisions throughout

Desired rhythm, initially tested in Base Assault:

1. **Establish:** choose infantry support or early vehicle pressure; select a rally point; identify the direct and flanking routes.
2. **Contest:** move a mixed force, protect supply, and react to the enemy composition. Transport offers a different approach.
3. **Expand:** invest in production, fuel/air access, or a forward position. Every choice has an opportunity cost.
4. **Commit:** combine infantry, armor, and supporting fire, or raid supply while a smaller group holds the front.
5. **Resolve:** finish a visible objective, recover from a failed push, or surrender. Clear progress prevents unexplained waiting.

Complete Breakthrough/Convoy rules, scenario-role asymmetry and all deadline values are defined in [BATTLE_CATALOG.md](BATTLE_CATALOG.md) and [MATCH_SETUP.md](MATCH_SETUP.md).

No scripted requirement to press Hold then Scan, compulsory Continue chain, or camera lock during deliveries. A contextual hint may introduce a new control once; player decisions drive the match.

### Objectives

**Base Assault:** preserve the designated-base identity and current destruction rules. Larger presets receive a visible duration configuration. If time expires with both bases alive, the existing draw rule remains explicit. Improve routes and counters when defenders dominate; do not conceal an outcome rule or manufacture pace through arbitrary health inflation.

**Frontline Control**, after ground and air foundations are accepted: three capture zones, two sides, a visible reinforcement-ticket total for each. Holding a majority drains the opponent's tickets; either destroying its designated main base or reducing its tickets to zero wins. Initial tuning proposal: 500 tickets, one ticket/second while the opponent holds at least two zones, 25-minute maximum, higher remaining tickets wins at the deadline, equal tickets draw. Destruction takes priority over the deadline; simultaneous terminal outcomes draw.

Only dismounted infantry captures. Proposed capture duration is eight uncontested seconds per ownership transition; taking an enemy point first neutralizes it, then captures it. Enemy infantry contests and freezes progress. Abandoned partial progress decays after a visible ten-second grace period. Owned empty points remain owned. Show the ring, owner, contested state, and timer. Capturing does not also grant income in the first version: territorial score and supply expansion should remain understandable separately.

**Sandbox**, later: adjustable armies, supported map/roster, resource presets, AI aggression, optional unlimited time. No progression rewards. Clearly separate stress-test settings from certified normal presets.

## 5. Maps and construction

Author the five battlefields in [MAPS.md](MAPS.md), beginning with the two existing regions and preserving campaign anchors and small regression presets. A larger match needs usable land and routes, not just wider camera bounds.

- Two expandable bases, clear staging areas, separate production exits, suitable landing pads, and runway space for enabled aircraft.
- At least three connected approaches: direct, flank, and a second contestable route. Measure width and travel time for the largest allowed vehicle.
- Forward positions useful for holding ground, supply routes that can be raided, and retreat routes that do not require traffic to cross through the same narrow doorway.
- Mountains, water, road surfaces, sidewalks, walls, and valid foundations have authored placement/traversal meaning. Check the complete rotated footprint and entry/exit clearance; revalidate at confirmation.
- Gates match the wall opening or permitted carriageway, retain the preview's transform, and cannot silently relocate. Capture nodes and producer exits cannot be sealed by friendly construction.
- Define whether a destroyed footprint is immediately clear, occupied by a traversable wreck, or blocked until a visible cleanup action; never leave an unexplained invisible blocker.

Target first ground contact around 45–90 seconds of purposeful travel in Standard, with a flank meaningfully longer but still useful. Measure this using actual units, not world distance alone. Prove multiple approaches on the two existing map regions before authoring the remaining three. Each map ultimately supplies 24 accepted scenario combinations.

## 6. Mobile army command

Use stable squads of four initially. Keep each member as an individual simulation unit. Several squads and vehicles can belong to a player-named battle group such as Left, Center, Reserve, or Air. Test four-to-six soldiers per squad later; avoid silently resizing groups during combat.

- Keep four-to-six large favorite group cards visible, depending on safe-area width. Pinning and replacement are explicit. Open a paged Army panel to reach the rest; do not shrink 24 cards into the current tray.
- One tap selects a squad/group. An explicit multi-select control adds groups; rectangle selection stays optional. Instructions and indicators must show the real gesture.
- Cards show member count, aggregate health, order, and urgent state. Distant groups appear as stable icons; selection reveals detail. No card reordering when units die or arrive.
- Move, Attack, and Hold stay familiar. Add Attack-advance as an explained option of Attack, and Retreat as a contextual group action once supported. Move remains a direct movement order; Hold preserves position.
- A new order replaces the old one reliably. Air and ground members receive compatible suborders; mixed selection must never force vehicles to an air destination or silently drop half the group.
- Selected-group route/destination feedback is visible immediately. Formation spreading, road columns, yielding, and regrouping happen automatically; the player does not assign 96 individual destinations.
- Recruitment uses role cards, quantity/queue controls, producer status, and a rally point. Accepting a request closes the drawer, shows one acknowledgment, and adds a queue badge. Cancel/refund and delivery failure remain readable.
- Batch helicopter deliveries where possible. Camera movement is optional, immediately interruptible, and does not repeat for every batch. A tap on the arrival notification finds the new group.
- ARIA is concise event assistance with rate-limited voice. Objective progress and important warnings remain visible when the panel is closed. Avoid voice stacking and language mixing; ending a match clears gameplay voices.

One shared selection truth must drive world highlight, card, selection panel, and command eligibility. Preserve the current seven-command layout, existing Scan control, anchored minimap, and full-width readable ARIA actions; new unit abilities belong in context rather than permanent extra HUD buttons.

## 7. Economy and opponent

Retain Materials, Oil, Fuel, and automatic supply delivery. Grow throughput through additional paid production/supply facilities and contested expansion locations. Cost, recruitment duration, transport capacity, fuel storage, and travel time must support the desired army size together.

Before balance claims, calculate time to first vehicle, first counter, first aircraft, and a representative 96-infantry force. Include queues, deliveries and upgrade expenditure. An economy that fills 200 Supply only after an hour fails an 18–25-minute preset. Use three understandable readiness stages: initial force, combined arms, then advanced warfare. Each unlock names the required facility. Shared category upgrades have a visible research timer and capped effects; defer a large branching research tree until these choices work. Established Base is an optional stage-two start; Full Arsenal belongs in Sandbox.

Protect a recoverable core economy: the starting backbone operates immediately; additional income requires vulnerable expansion. Test supply loss, rebuilding, blocked deliveries, fuel shortage, and capacity reservations. No invisible free resource pulses or emergency enemy spawns.

The shared enemy/ARIA planning boundaries, four objective policies, difficulty behavior and generalization tests are specified in [AI_AND_ARIA.md](AI_AND_ARIA.md). AI needs a base reserve, field groups, reactive counter-production, scouting, two-route pressure, transport use, and retreat/replacement. Use existing AI planning/order systems with mode configuration. Evaluate strategic choices at a bounded cadence; preserve responsiveness for nearby combat and immediate player commands. AI follows the same costs, caps, target visibility, transport rules, and loss accounting. Difficulty changes composition, timing, and coordination; any economy handicap must be explicit in setup.

## 8. Recon and full air warfare

The current preset uses full-map visibility. Keep that truthful during the ground slice. Before claiming Radar Tank or Recon Drone provides scouting, implement and validate a shared visibility/intel service for world rendering, targeting, minimap, warnings, and AI.

Define visible, last-seen, and unknown contacts. Terrain can remain known; unseen enemy units cannot be precision-targeted through hidden entity references. Radar may provide an approximate vehicle/air contact with age and confidence; direct vision identifies it. Last-seen icons visibly age and expire. The existing Scan command owns relevant scan interaction; no duplicate radar button.

Air warfare requires target-category rules, air defense, fuel and return behavior, clear unavailable reasons, and safe landing/boarding. Jets need real attack-pass and runway/return behavior where required by their configs. Transport planes must be classified consistently as player tactical assets or automated deliveries, including cap and destruction/refund accounting. Ship these as capabilities, not just selectable models.

## 9. Scale without sacrificing control

Use the existing ECS, pathfinding, spatial-query, and rendering foundations. Profile the actual combat scenario before choosing an optimization. Replace per-frame broad scans and string classification in the Skirmish overlay where they dominate; avoid a general engine rewrite.

Bound path requests, share suitable group routes, reserve spread destinations, prioritize new player orders, and handle chokepoint traffic. Time-sliced strategic AI must not make the simulation run slower than the match clock. Keep damage and movement authoritative even when visual detail is reduced.

Use animation/render LOD, pooled projectiles/markers/VFX, capped audio voices, batched UI updates, and a bounded corpse/wreck presentation policy. Never use offscreen status to grant invulnerability, skip damage, or let supplies teleport. Profile mass arrivals, all units visible, all units fighting, destruction, and 30-minute thermal behavior.

The test ladder is 50 → 100 → 200 → 350 → 500 actual combat entities. **500 is an engineering stress case**, not a shipping promise. Physical-device certification decides which preset is exposed on each tier. Existing [performance budgets](../../Architecture/performance_regression_contract.md) remain authoritative; the [acceptance plan](ACCEPTANCE.md) adds proposed command responsiveness and large-battle tests.

## 10. Delivery decision

The next playable milestone is **Standard ground warfare**: five useful infantry roles, car/APC/tank access, reliable army groups, functioning production and supply, and roughly 100 active combatants across both sides. Air defense, helicopters, recon/visibility, and the War target follow; then advanced aircraft, Frontline Control, and Large War. Reliable match checkpoints are a release requirement before making the longer War/Large War presets the normal mobile default.

Run scale probes from the first package so performance discoveries inform design. Each milestone ends with a real match, bilingual UI evidence, capacity/performance measurements, and an explicit decision to continue or correct. M1–M5 regression protection remains mandatory for shared changes. New rewards, multiplayer, Operations progression, and decorative VFX are separate workstreams after this core experience earns acceptance.

No implementation time estimate is reliable until the first scale probe, roster audit, and ground slice establish the costs. Track package completion and measurable acceptance rather than promising a date or calling asset availability finished gameplay.
