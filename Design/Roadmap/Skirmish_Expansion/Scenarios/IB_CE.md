# Industrial Basin — Convoy Escort: implementation packet

Proposed content, 2026-09-21. **All expanded entries below remain Planned.** Existing small prototype evidence, if mentioned, is not acceptance of the expanded version. Use [technical architecture](../TECHNICAL_ARCHITECTURE.md), [objective rules](../OBJECTIVE_IMPLEMENTATION.md), [roster/economy](../ROSTER_AND_ECONOMY_IMPLEMENTATION.md), [map contract](../MAP_IMPLEMENTATION.md) and [work packages](../AGENT_WORK_PACKAGES.md).

## Shared packet implementation

- Map: `opmap.skirmish.industrial_basin`; layout `layout.skirmish.ib.ce`. Three truck bays at northwest storage terminal; destination southeast storage terminal; routes a/b: freight avenue / service ring; two defender tower slots, holding pockets, repair service pads.
- Objective owner: `SkirmishConvoyObjectiveSystem` with shared fact projection and `SkirmishOutcomeSystem`. Roles: `base.player; base.enemy; convoy.1; convoy.2; convoy.3; convoy.origin; convoy.destination`.
- Win: Deliver at least 2 of 3 original objective trucks with 5 uninterrupted living seconds in the destination. Trucks wait for orders, are Fuel-exempt, and use normal movement/damage plus the specified repair action.
- Loss: Two original trucks destroyed, deadline before two deliveries, or surrender. Main-base loss alone is not terminal. No replacement truck, sale, boarding or logistics transfer can satisfy the objective.
- All sizes preserve the objective rule; Standard/War/Large War deadlines and force values are printed per entry below. Difficulty never changes resources/stats. Default first visit: Regular/Standard.
- Startup/roster/production/army/visibility/AI/UI/save use the shared types in TECHNICAL_ARCHITECTURE; no per-entry controller or ARIA solution script.
- Build shared map assets once. Per entry, build `SkirmishScenario_SNNN.asset` and `ScenarioSetup_SNNN.asset` in `Assets/Game/Configs/SkirmishExpansion/Scenarios/SNNN/`; bind the shared objective/army/start/size assets and resolved initial placement arrays. Use `SkirmishDefinitionBuilder` (proposed), not hand-written Unity YAML.
- Map-specific acceptance: Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.
- Mandatory objective fixtures for every entry: Three original truck IDs vs ordinary logistics; 4.9 s dwell restore; same-tick lethal damage before arrival; two-loss failure; selectable overlap; route change after a wreck; Fuel exemption isolated; repair cost/cancel/death accounting.
- Shared class dependencies: `SkirmishSessionInitializationSystem`, `SkirmishScenarioSpawnSystem`, `SkirmishRosterProjectionSystem`, `SkirmishCapacityReservationSystem`, `SkirmishCapacityLifecycleSystem`, `SkirmishArmyGroupSystem`, `SkirmishResearchSystem`, `SkirmishEnemyStrategySystem`, `SkirmishCheckpointSystem`, `SkirmishResultSettlementSystem`, `SkirmishSessionCleanupSystem`, UI projections and extended `AriaSkirmishPlanSystem`.
- Enemy starts with the same profile/start resources and total combat roster. BA/FC are symmetric; BT/CE use two extra disclosed defender towers and A/B/reserve group placement from MAP_IMPLEMENTATION. All reinforcements are paid production, never scripted free waves.
- Infantry numbers below are individual soldiers, not squad cards. CAR/APC/TANK/AA/TH are platforms; logistics, delivery carriers, objective trucks and buildings are counted separately. Full vehicle tanks are additional listed endowment from certified role data; not invented Fuel stock.

## S091

**Industrial Basin · Convoy Escort · Ground Maneuver · Field Base** — handoff work ordinal **91**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S091`; definition `skirmish.s091`; scenario `scenario.skirmish.s091`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `G`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Leave trucks at origin until a scout and cheap ground escort check the first exposed segment. Two intended approaches: Use gunner/rocketeer infantry on freight avenue, or send APCs ahead along service ring and move trucks between holding pockets. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 3/0 | 7/9 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 3/0 | 7/9 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 3/0 | 7/9 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s091.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject offensive-air queues in G while preserving transport/recon. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104820`, `130454`, `156012` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196704`, Veteran `262238`, Commander `327764`. Additional exposed sizes need a full Regular EN and FA win at War seed `393332` and Large War seed `458970`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S091/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S092

**Industrial Basin · Convoy Escort · Ground Maneuver · Established Base** — handoff work ordinal **92**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S092`; definition `skirmish.s092`; scenario `scenario.skirmish.s092`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `G`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Starting tank screens the truck route while one squad protects the rear service pad. Two intended approaches: Advance as a tight armored escort, or clear the long route first and move the convoy only when both holding pockets are safe. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 900/240/700 | 3; 3/0 | 10/12 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 1350/360/1050 | 4; 3/0 | 11/13 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 1800/480/1400 | 4; 3/0 | 11/13 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s092.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject offensive-air queues in G while preserving transport/recon. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104821`, `130455`, `156013` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196705`, Veteran `262239`, Commander `327765`. Additional exposed sizes need a full Regular EN and FA win at War seed `393333` and Large War seed `458971`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S092/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S093

**Industrial Basin · Convoy Escort · Air Mobile · Field Base** — handoff work ordinal **93**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S093`; definition `skirmish.s093`; scenario `scenario.skirmish.s093`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `A`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Start with a ground escort and scout both truck routes; air research competes with convoy protection funds. Two intended approaches: Use an APC infantry escort on service ring, or delay departure for a limited air-support transition without exhausting the deadline. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 3/0 | 7/9 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 3/0 | 7/9 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 3/0 | 7/9 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s093.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104822`, `130456`, `156014` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196706`, Veteran `262240`, Commander `327766`. Additional exposed sizes need a full Regular EN and FA win at War seed `393334` and Large War seed `458972`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S093/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S094

**Industrial Basin · Convoy Escort · Air Mobile · Established Base** — handoff work ordinal **94**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S094`; definition `skirmish.s094`; scenario `scenario.skirmish.s094`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `A`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Move infantry ahead with the transport helicopter while AA and APCs remain near the trucks. Two intended approaches: Leapfrog ground protection between holding pockets, or use observed air support to clear an intercept point before advancing. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 900/240/700 | 3; 3/0 | 10/12 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 1350/360/1050 | 4; 3/0 | 11/13 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 1800/480/1400 | 4; 3/0 | 11/13 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s094.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104823`, `130457`, `156015` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196707`, Veteran `262241`, Commander `327767`. Additional exposed sizes need a full Regular EN and FA win at War seed `393335` and Large War seed `458973`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S094/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S095

**Industrial Basin · Convoy Escort · Combined Arms · Field Base** — handoff work ordinal **95**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S095`; definition `skirmish.s095`; scenario `scenario.skirmish.s095`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `C`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Choose tank escort investment or air scouting; the three objective trucks do not replace Oil/Fuel logistics. Two intended approaches: Clear freight avenue with mixed infantry/armor, or draw enemy reserve away and take service ring. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 3/0 | 7/9 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 3/0 | 7/9 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 3/0 | 7/9 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s095.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104824`, `130458`, `156016` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196708`, Veteran `262242`, Commander `327768`. Additional exposed sizes need a full Regular EN and FA win at War seed `393336` and Large War seed `458974`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S095/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S096

**Industrial Basin · Convoy Escort · Combined Arms · Established Base** — handoff work ordinal **96**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S096`; definition `skirmish.s096`; scenario `scenario.skirmish.s096`; map `opmap.skirmish.industrial_basin`; objective `CE`; army `C`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-09;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ce;vehicle_repair`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Split tank/AA escort, advance infantry and the protected convoy; do not leave truck commands implicit. Two intended approaches: Move a mixed force and convoy together, or use a mobile advance group to clear the alternative route and repair at a safe service pad. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 900/240/700 | 3; 3/0 | 10/12 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 1350/360/1050 | 4; 3/0 | 11/13 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 1800/480/1400 | 4; 3/0 | 11/13 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishConvoyObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s096.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove both valid convoy routes and the normal service-pad repair command. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104825`, `130459`, `156017` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196709`, Veteran `262243`, Commander `327769`. Additional exposed sizes need a full Regular EN and FA win at War seed `393337` and Large War seed `458975`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S096/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.
