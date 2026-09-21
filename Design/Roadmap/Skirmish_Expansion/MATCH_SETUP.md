# Skirmish setup, difficulty and starting packages

Planning/balance specification, updated 2026-09-21. These values are concrete initial tuning inputs, not shipped values or measured balance. Preserve the three legacy prototype mappings until expanded configurations pass their gates. This document and [BATTLE_CATALOG.md](BATTLE_CATALOG.md) refine the original [plan](PLAN.md); no Operations progression is specified here.

Implementation companions: [360 exact scenario/size vectors](INITIAL_SETUP_MATRIX.csv), [role/producer/economy contracts](ROSTER_AND_ECONOMY_IMPLEMENTATION.md), [objective algorithms](OBJECTIVE_IMPLEMENTATION.md) and [setup compiler architecture](TECHNICAL_ARCHITECTURE.md). Every infantry squad below has four individuals. Additional infantry/vehicle queues are capacity on the specified producer, not extra designated main bases; Ground Staging also has one separate logistics queue. September 21 economy corrections below replace the contradictory old recipe.

## Setup defaults and configuration ownership

Initial player-facing choices: scenario card, **Regular** difficulty, **Standard** size, and Start Battle. Card identity includes its map/objective/army/start. Advanced controls expose only certified alternatives. All published scenarios are selectable without campaign grinding. Recommend an introductory subset, but do not lock the library behind 119 prior wins.

Store an immutable snapshot: scenario ID/content version, physical map and navigation hashes, objective/version, faction/side, army profile and roster/balance version, start package, difficulty/version, army size/caps, economy profile, intel rules, seed streams, clock, checkpoint version and ARIA capability version. Simulation, briefing, AI, ARIA and results must agree on that snapshot. A changed setup becomes Custom, except supported seed/difficulty/size changes. Profile records distinguish scenario + difficulty + size + content version.

Default visibility in the expanded catalog is shared fog/intel: explored terrain remains known, live targets need legal observation, last-seen contacts visibly age. Development ground slices may use clearly labeled full vision until intel works; they cannot claim reconnaissance acceptance or full catalog completion. Pause is available; no competitive multiplayer assumptions. Checkpoint/resume is required before War becomes the recommended mobile choice. An interrupted match restores its exact state, not fresh resources.

## Four difficulty levels

Use four visible levels: **Recruit, Regular, Veteran, Commander**. Regular is the reference balance. Existing internal difficulty enums may need migration; do not silently map an unsupported old setting to a different displayed difficulty. Every faction obeys the same roster, costs, health, damage, range, visibility, caps, build/research times and starting package. No economic/stat handicap in these four standard levels.

| Setting | Recruit | Regular | Veteran | Commander |
|---|---|---|---|---|
| Purpose | Learn with recovery time | Intended balanced match | Challenge experienced players | Strongest fair opponent |
| Deliberate response after new strategic information | 5–8 sec | 3–5 sec | 1.5–3 sec | 0.8–2 sec |
| Proactive scouting target interval | 45–60 sec | 25–40 sec | 15–25 sec | 10–20 sec |
| Coordinated pressure | Usually one front | Main front + occasional flank | Two fronts and mobile reserve | Two/three purposeful fronts when affordable |
| Counter-production | Broad response to repeated observed threat | Adjust to observed dominant threats | Anticipate from observed facilities/composition | Strong threat/economy prediction from legal evidence |
| Retreat/recovery | Simple regroup, longer recommitment | Preserve valuable units | Timely retreat/repair and replacement | Coordinated withdrawal, reserves and supply protection |
| Attack planning | Conservative, avoids early all-in | Mix pressure and expansion | Disrupt production/supply | Coordinate objective pressure and supply raids |

These are initial behavior targets, not slower firing or artificial input delays for individual units. Nearby combat autonomy stays responsive at every difficulty. Strategic scoring is amortized independently of these deliberate reaction windows. Commander cannot use unseen enemy positions or future player orders.

Minimum deliberate offensive departure from home for BA/FC Field starts: Recruit 90 sec, Regular 60 sec, Veteran 45 sec, Commander 30 sec; Established starts 60/40/25/15 sec. These are grace ceilings for scripted opening restraint, not forced attacks: a prudent AI can wait longer. Defending its own territory is always allowed. Objective garrisons in BT/CE are present and disclosed from the start; the same grace times apply only to additional proactive mobile raids, not to defending a corridor or responding to an approaching convoy. Do not give a convoy an invisible protected phase.

Difficulty must not change the scenario count. Publish observed difficulty differences and human comprehension, not a promise that Recruit never loses or Commander always wins. Accessibility coaching and ARIA explanation frequency are independent of enemy difficulty.

## Size and duration

These are maximum alive + reserved combat capacities, not automatically spawned forces. Embarked soldiers count once. Infantry counted individually; four soldiers form one squad. Ground transport counts in the ground category. Recon drones and player-operated transport aircraft count in the air category. Automatic delivery carriers, supply trucks and objective trucks have separate bounded support accounting and performance cost.

| Size | Infantry/side | Ground combat + transport/side | Tactical aircraft/side | Maximum combat platforms both sides | Supply/side | Supply vehicles/side | Player-built structures/side | Normal pacing target |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| Standard | 48 | 8 | 2 | 116 | 128 | 6 | 20 | 12–18 min |
| War | 96 | 12 | 4 | 224 | 200 | 8 | 30 | 18–25 min |
| Large War | 144 | 20 | 6 | 340 | 320 | 10 | 40 | 20–30 min |

Supply costs: infantry 1; armored car/APC 4; tank/missile launcher 6; Radar Tank 4; tactical aircraft 8. Both Supply and category limits must allow a queue request. Aircraft cap is deliberately separate from support carriers. Two automatic delivery aircraft at most per side. CE permits three additional objective trucks, explicitly shown in support telemetry. Authored starting/objective structures are reported separately; walls/barriers have a separate 40/60/80-segment ceiling and cannot seal required routes. Performance totals include everything.

Hard match deadlines by objective and size:

| Objective | Standard | War | Large War |
|---|---:|---:|---:|
| BA | 18 min | 25 min | 30 min |
| FC | 20 min | 25 min | 30 min |
| BT | 18 min | 25 min | 30 min |
| CE | 18 min | 25 min | 30 min |

Pacing targets describe typical duration; deadlines are explicit rule values. Do not force a match to last 15 minutes or silently speed combat to reach these targets. Established starts retain the same deadline and often resolve sooner. Unsupported size is disabled before deployment with a device explanation; no mid-match deletion or combat-rule reduction to restore performance. A lower certified graphics tier is allowed without changing simulation.

## Starting balances and facilities

All quantities below are **per faction at Standard size**, before objective-specific adjustments. They are starting grants, not costs secretly charged against the displayed wallet. Materials/Oil/Fuel are usable stock at deployment; capacities are real storage. The wallet and storage never show different versions of the balance.

| Start | Readiness | Materials | Oil | Usable Fuel | Storage M/O/F | Initial logistics | Ground queues |
|---|---:|---:|---:|---:|---|---|---:|
| F — Field Base | 1 | 450 | 120 | 350 | 800 / 500 / 800 | 1 oil tanker + 1 ground supply truck | 1 infantry + 1 vehicle |
| E — Established Base | 2 | 900 | 240 | 700 | 1800 / 1000 / 1600 | 1 oil tanker + 2 ground supply trucks | 2 infantry + 1 vehicle |

War multiplies starting stock and storage by 1.5 (round to whole units); Large War by 2. No implicit unit multiplier. Facility inventory and forces use the explicit tables below. Logistics starts at three support vehicles for Field War/Large and four for Established War/Large (one tanker, remaining ground trucks), within the support cap. War/Large Field gets a second infantry queue. Both factions receive identical packages in BA/FC.

Field facilities: one designated main Barracks (HQ + infantry producer), one ground vehicle staging/producer point, one oil pump, one refinery/conversion facility, one Fuel Bladder, one ammunition/materials depot, and one watchtower. Established: same backbone, second infantry production capacity, two watchtowers total, Satellite Dish/intel station, and one Helipad. Field G/A/C has an authored empty helipad foundation site; it is not a functional free producer. Airport/runway reservation exists on every eligible map but starts unbuilt, even in Established. No temporary props represented as working facilities.

**Resolved producer implementation:** reuse Barracks/Helipad/Airport, create the modular Ground Staging prefab, add the intel-station drone pad, and author separate mode fabrication/refinery profiles exactly as specified in [ROSTER_AND_ECONOMY_IMPLEMENTATION](ROSTER_AND_ECONOMY_IMPLEMENTATION.md). SK-02/03 own the implementation; no further choice of vehicle-factory asset is needed from the user. These remain proposed functional roles until built and tested. No card may deploy a unit without its real producer/delivery route.

War/Large Established gets a second ground queue and second refinery module. Other readiness/facility differences are bought normally. Starting upgrades are capability/readiness only; all category stat upgrades start at level zero. Starting facilities are fully built and supplied; their positions and exits pass the authored-layout audit.

## Exact starting forces

Abbreviations: R rifle squad, H heavy-gunner squad, K anti-armor/rocketeer squad (each squad four soldiers); CAR light armored car; APC compatible armored transport; TANK battle tank; AA air missile launcher; TH transport helicopter. Units begin outside transport, selected groups unassigned until the player chooses. Vehicles are fully fueled with no ammunition deficit; fuel loaded into them is listed separately from storage and counted in total starting-value comparison.

| Size/start | G — Ground Maneuver | A — Air Mobile | C — Combined Arms |
|---|---|---|---|
| Standard F | 2R + K; CAR + APC | 2R + K; CAR + APC | 2R + K; CAR + APC |
| Standard E | 3R + H + K; CAR + APC + TANK | 3R + H + K; CAR + APC + AA; TH | 3R + H + K; APC + TANK + AA; TH |
| War F | 3R + H + K; CAR + 2 APC | Same | Same |
| War E | 4R + H + 2K; CAR + 2 APC + 2 TANK | 4R + H + 2K; CAR + 2 APC + 2 AA; 2 TH | 4R + H + 2K; 2 APC + 2 TANK + AA; 2 TH |
| Large War F | 4R + H + K; 2 CAR + 2 APC | Same | Same |
| Large War E | 6R + 2H + 2K; 2 CAR + 2 APC + 3 TANK | 6R + 2H + 2K; 2 CAR + 2 APC + 3 AA; 2 TH | 6R + 2H + 2K; 2 APC + 3 TANK + 2 AA; 2 TH |

Field openings intentionally share basic forces; the scenario's roster restrictions change the affordable development path. All aircraft in this table are unarmed transport; offensive air requires production and permits a counter window. BT substitutes the first three infantry squads with 12 designated rifles, preserving total count and limits. It adds no extra combat soldiers. CE adds its three objective trucks on top of support accounting only.

BT/CE defensive enemy starts with the same total combat roster, funds and productive backbone as the player for that profile. BT splits existing forces between the two corridors and mobile reserve; CE between visible/legally concealed patrol areas and rear reserve. The defender receives two additional authored watchtowers protecting approaches; they must never overlap into unavoidable start-zone fire. The briefing explicitly lists this defender advantage. No arbitrary multiplied enemy army or infinite reinforcement spawner. Tune this objective-specific asymmetry by placement/time/goal, not by covert stats. Additional defensive features must be itemized before acceptance.

## Initial recruitment and construction budget

Provisional Materials costs / queue seconds below provide a starting economy to test. Infantry cost is **per four-person squad**, platform cost per vehicle/aircraft. Oil is feedstock, not an additional recruitment currency. Fuel supports operation; travel/return budgets are measured from actual unit consumption before release.

| Role | M / seconds | Readiness / producer | Counter or purpose |
|---|---|---|---|
| Rifle squad | 80 / 15 | R1 Barracks | General capture/escort |
| Heavy-gunner squad | 100 / 20 | R1 Barracks | Anti-infantry support |
| Marksman squad | 120 / 22 | R1 Barracks | Long-range infantry pressure |
| Breacher squad | 100 / 20 | R1 Barracks | Close protected-position assault; needs real capability |
| Rocketeer squad | 120 / 22 | R1 Barracks | Early anti-armor counter |
| Armored car | 160 / 25 | R1 ground producer | Scout/light pressure |
| Fast / Armored / Heavy APC | 160/200/280 M; 25/30/40 sec | R1 / R1 / R2 ground producer | Infantry transport with distinct protection/speed |
| Battle Tank | 360 / 50 | R2 ground producer | Heavy direct fire |
| Air missile launcher | 220 / 30 | R1 ground producer, only in air-offense profiles | Timely ground anti-air; no Helipad prerequisite |
| Radar Tank | 180 / 30 | R2 ground producer | Legal detection; requires implemented intel |
| Ground missile launcher | 420 / 55 | R3 ground producer | Vulnerable siege, not universal targeting |
| Transport helicopter | 240 / 35 | R2 Helipad | Tactical transport |
| Light / heavy attack helicopter | 300/420 M; 40/55 sec | R2 Helipad | Ground support, vulnerable to AA |
| Recon drone | 140 / 25 | R2 intel station | Reconnaissance; no imaginary combat weapon |
| Fighter / strike jet | 480/520 M; 60/65 sec | R3 Airport | Air interception / ground strike |
| Transport plane | 440 / 60 | R3 Airport | Validated heavy transport, distinct from automated deliveries |
| Replacement ground truck / tanker | 100/140 M; 20/25 sec | R1 logistics staging | Supply recovery; counts against support cap |

Do not treat these values as approved prefab edits. Confirm actual targeting/delivery semantics and role costs in the roster ledger before implementing; prevent a paid platform appearing with unusable fuel or no safe return. Each producer card shows fuel needs, next queue completion and delivery ETA separately. New unlocks require affordable counters already available for at least one measured counter-production + delivery interval before the corresponding offensive force reaches the opponent.

Proposed structures M / build sec: extra Barracks 240/40; ground staging 300/45; Helipad 300/45; Airport 600/75; watchtower 160/30; Satellite Dish 200/35; extra oil pump 240/40; refinery 220/40; storage/depot 160/30. Match-ready R2 research 240 M/45 sec; R3 480 M/60 sec, requiring R2 and functional production. Airports must fit validated runway pads. Starting facilities are free starting grants as above; replacement costs apply after destruction. Gates/walls retain a per-segment priced definition after footprint audit, not free infinite fortification.

## Economy, upgrades and recovery rules

Initial Standard throughput per functioning supply backbone: **100 M/min fabrication and 120 Oil/min extraction**, delivered through real logistics. September 21 correction: the earlier 100-Oil → 300-Materials/45-second proposal contradicted that rate and is superseded. Materials fabrication and Fuel refining are separate mode-specific facility profiles: Standard fabrication consumes 10 Oil → 50 M per 30 seconds; refining consumes 30 Oil → 60 usable Fuel per 30 seconds. Total input is 80 Oil/min when both run. Each facility processes one batch at a time; no invisible wallet pulse or passive fuel regeneration.

War throughput: 160 M/min + 180 Oil/min extraction, using 16 Oil → 80 M and 45 Oil → 90 Fuel per 30 seconds. Large War: 220 M/min + 240 Oil/min extraction, using 22 Oil → 110 M and 60 Oil → 120 Fuel per 30 seconds. See [ROSTER_AND_ECONOMY_IMPLEMENTATION](ROSTER_AND_ECONOMY_IMPLEMENTATION.md) for the full input budget, priority and storage rules. Implement explicit mode facility profiles shared by both factions; preserve global Campaign recipes. A second refinery adds processing capacity but no free Oil. Verify stock/capacity, truck throughput and queue utilization together. The target is first counter within roughly 45–90 sec, first bought vehicle 1–3 min, first offensive helicopter 4–7 min, advanced air 8–12 min in Field starts, and a viable War army by 12–18 min. Established starts compress this. Failed measured timings reopen costs/rates through a versioned tuning change.

Affordable replenishment must remain a choice against expansion/research. A supply raid reduces actual deliveries. A destroyed producer can be rebuilt if the player has resources and a legal site. No emergency gifts to rescue a doomed state. Queued funds and Supply reserve exactly once; cancellation before production refunds 100%, cancellation after production starts refunds 75% of the paid cost, and launched deliveries are non-cancellable. Destruction loses delivered/in-transit units and committed costs; a failed system-owned spawn before dispatch refunds 100% and releases reservation. Clearly distinguish blocked waiting from destroyed loss.

Category upgrades (initial one level per category): Infantry weapons +10% damage, Vehicles protection +10% maximum health, Aircraft efficiency −10% fuel consumption; each 200 M / 45 sec, available R2 at its relevant producer. Infantry upgrade affects only infantry attacks; no tower/air damage leakage. Vehicle max-health upgrade preserves current health percentage, does not fully heal. Effects apply once from base stats to existing and future units. Counter testing may revise values. No stacking repeat purchases. Research cancellation before start refunds 100%; after start 75%; research-building destruction loses the unfinished research and its cost. Completed faction upgrades persist through building loss until match end. Checkpoints preserve progress; fresh battle resets to the selected initial package.

Storage-full production waits with a visible reason; never consumes input and discards output. The player can cancel according to the same refund rules. Deleting or destroying storage cannot manufacture capacity or resources. Destroyed vehicle/structure markers disappear immediately; wreck presentation and footprint reuse are explicitly authored.

## Custom and Sandbox

The 120 catalog entries use the fixed profile tables. Custom Battle can choose map/objective/army/start/difficulty/seed and certified size, with an explicit preview of both forces. Overrides to resources, roster or asymmetry label the result Custom; configuration remains reproducible. Invalid combinations cannot start.

Sandbox follows the core catalog systems: Full Arsenal at R3, all validated producers/roles, optional unlimited resources/time, configurable armies and AI aggression. It earns no standard scenario completion or progression rewards. It is the place to experiment with all supported units immediately. Unsupported abilities, invalid pads and uncertified normal sizes do not become safe just because Sandbox is selected; engineering stress presets are separately labeled and excluded from readiness claims.
