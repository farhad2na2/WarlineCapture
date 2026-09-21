# Mountain Pass — Base Assault: implementation packet

Proposed content, 2026-09-21. **All expanded entries below remain Planned.** Existing small prototype evidence, if mentioned, is not acceptance of the expanded version. Use [technical architecture](../TECHNICAL_ARCHITECTURE.md), [objective rules](../OBJECTIVE_IMPLEMENTATION.md), [roster/economy](../ROSTER_AND_ECONOMY_IMPLEMENTATION.md), [map contract](../MAP_IMPLEMENTATION.md) and [work packages](../AGENT_WORK_PACKAGES.md).

## Shared packet implementation

- Map: `opmap.skirmish.mountain_pass`; layout `layout.skirmish.mp.ba`. Player/enemy bases and all three routes: central pass, west vehicle bypass, east infantry trail; both supply expansions and service pads.
- Objective owner: `SkirmishBaseAssaultObjectiveSystem` with shared fact projection and `SkirmishOutcomeSystem`. Roles: `base.player; base.enemy`.
- Win: Destroy the original enemy main Barracks while the original player main Barracks survives. Same-tick destruction of both draws; both alive at deadline draws.
- Loss: Original player main base destroyed while enemy survives, or accepted surrender. Other Barracks cannot replace the designated identity.
- All sizes preserve the objective rule; Standard/War/Large War deadlines and force values are printed per entry below. Difficulty never changes resources/stats. Default first visit: Regular/Standard.
- Startup/roster/production/army/visibility/AI/UI/save use the shared types in TECHNICAL_ARCHITECTURE; no per-entry controller or ARIA solution script.
- Build shared map assets once. Per entry, build `SkirmishScenario_SNNN.asset` and `ScenarioSetup_SNNN.asset` in `Assets/Game/Configs/SkirmishExpansion/Scenarios/SNNN/`; bind the shared objective/army/start/size assets and resolved initial placement arrays. Use `SkirmishDefinitionBuilder` (proposed), not hand-written Unity YAML.
- Map-specific acceptance: All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.
- Mandatory objective fixtures for every entry: Original vs replacement Barracks; both deaths on the same tick; deadline draw; loss of the whole field army with a surviving producer; hidden enemy health must remain last-observed.
- Shared class dependencies: `SkirmishSessionInitializationSystem`, `SkirmishScenarioSpawnSystem`, `SkirmishRosterProjectionSystem`, `SkirmishCapacityReservationSystem`, `SkirmishCapacityLifecycleSystem`, `SkirmishArmyGroupSystem`, `SkirmishResearchSystem`, `SkirmishEnemyStrategySystem`, `SkirmishCheckpointSystem`, `SkirmishResultSettlementSystem`, `SkirmishSessionCleanupSystem`, UI projections and extended `AriaSkirmishPlanSystem`.
- Enemy starts with the same profile/start resources and total combat roster. BA/FC are symmetric; BT/CE use two extra disclosed defender towers and A/B/reserve group placement from MAP_IMPLEMENTATION. All reinforcements are paid production, never scripted free waves.
- Infantry numbers below are individual soldiers, not squad cards. CAR/APC/TANK/AA/TH are platforms; logistics, delivery carriers, objective trucks and buildings are counted separately. Full vehicle tanks are additional listed endowment from certified role data; not invented Fuel stock.

## S049

**Mountain Pass · Base Assault · Ground Maneuver · Field Base** — handoff work ordinal **50**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S049`; definition `skirmish.s049`; scenario `scenario.skirmish.s049`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `G`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ba`. Recommended later size `Standard` is gated; first visit remains Standard.

**Opening and decisions:** Use initial infantry/car to scout central pass; recruit anti-armor before a tank commitment. Two intended approaches: Develop tanks for central pass with infantry cover, or use APC infantry through west vehicle bypass while threatening supply. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s049.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject offensive-air queues in G while preserving transport/recon. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104778`, `130412`, `155970` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196662`, Veteran `262196`, Commander `327722`. Additional exposed sizes need a full Regular EN and FA win at War seed `393290` and Large War seed `458928`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S049/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S050

**Mountain Pass · Base Assault · Ground Maneuver · Established Base** — handoff work ordinal **51**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S050`; definition `skirmish.s050`; scenario `scenario.skirmish.s050`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `G`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ba`. Recommended later size `Standard` is gated; first visit remains Standard.

**Opening and decisions:** Use the starting tank/APC to contest central pass, retaining one infantry squad at home. Two intended approaches: Press quickly with the established ground force, or invest in siege and attack via east infantry trail after scouting. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s050.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject offensive-air queues in G while preserving transport/recon. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104779`, `130413`, `155971` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196663`, Veteran `262197`, Commander `327723`. Additional exposed sizes need a full Regular EN and FA win at War seed `393291` and Large War seed `458929`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S050/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S051

**Mountain Pass · Base Assault · Air Mobile · Field Base** — handoff work ordinal **52**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S051`; definition `skirmish.s051`; scenario `scenario.skirmish.s051`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `A`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Hold a cheap ground screen and build R2/Helipad only after the affordable counter window. Two intended approaches: Escort an APC ground push on west vehicle bypass, or transition to attack/transport air while defending supply. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s051.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104780`, `130414`, `155972` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196664`, Veteran `262198`, Commander `327724`. Additional exposed sizes need a full Regular EN and FA win at War seed `393292` and Large War seed `458930`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S051/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S052

**Mountain Pass · Base Assault · Air Mobile · Established Base** — handoff work ordinal **53**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S052`; definition `skirmish.s052`; scenario `scenario.skirmish.s052`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `A`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Scout with infantry and the initial transport helicopter; AA protects the staging area. Two intended approaches: Lift infantry behind an observed flank near west vehicle bypass, or establish offensive air with an affordable ground reserve. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s052.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104781`, `130415`, `155973` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196665`, Veteran `262199`, Commander `327725`. Additional exposed sizes need a full Regular EN and FA win at War seed `393293` and Large War seed `458931`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S052/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S053

**Mountain Pass · Base Assault · Combined Arms · Field Base** — handoff work ordinal **54**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S053`; definition `skirmish.s053`; scenario `scenario.skirmish.s053`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `C`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Choose between earlier ground armor and the R2 air facility; do not buy every unlock before defending. Two intended approaches: Concentrate a tank/infantry push along central pass, or pressure east infantry trail while air/recon supports the other approach. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s053.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104782`, `130416`, `155974` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196666`, Veteran `262200`, Commander `327726`. Additional exposed sizes need a full Regular EN and FA win at War seed `393294` and Large War seed `458932`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S053/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S054

**Mountain Pass · Base Assault · Combined Arms · Established Base** — handoff work ordinal **55**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S054`; definition `skirmish.s054`; scenario `scenario.skirmish.s054`; map `opmap.skirmish.mountain_pass`; objective `BA`; army `C`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Separate the starting tank/AA screen from transportable infantry; keep Fuel delivery protected. Two intended approaches: Make a coordinated ground-plus-lift assault, or raid exposed supply from west vehicle bypass while armor holds central pass. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s054.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove direct and flank base assaults; a supply raid by itself never awards Victory. All mandatory points must be ground-infantry reachable; keep heavy vehicles/trucks off the east infantry trail and certify wreck passing bays.

**ARIA must play and win:** Regular seeds `104783`, `130417`, `155975` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196667`, Veteran `262201`, Commander `327727`. Additional exposed sizes need a full Regular EN and FA win at War seed `393295` and Large War seed `458933`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S054/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.
