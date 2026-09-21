# Industrial Basin — Base Assault: implementation packet

Proposed content, 2026-09-21. **All expanded entries below remain Planned.** Existing small prototype evidence, if mentioned, is not acceptance of the expanded version. Use [technical architecture](../TECHNICAL_ARCHITECTURE.md), [objective rules](../OBJECTIVE_IMPLEMENTATION.md), [roster/economy](../ROSTER_AND_ECONOMY_IMPLEMENTATION.md), [map contract](../MAP_IMPLEMENTATION.md) and [work packages](../AGENT_WORK_PACKAGES.md).

## Shared packet implementation

- Map: `opmap.skirmish.industrial_basin`; layout `layout.skirmish.ib.ba`. Player/enemy bases and all three routes: freight avenue, service ring, warehouse lane; both supply expansions and service pads.
- Objective owner: `SkirmishBaseAssaultObjectiveSystem` with shared fact projection and `SkirmishOutcomeSystem`. Roles: `base.player; base.enemy`.
- Win: Destroy the original enemy main Barracks while the original player main Barracks survives. Same-tick destruction of both draws; both alive at deadline draws.
- Loss: Original player main base destroyed while enemy survives, or accepted surrender. Other Barracks cannot replace the designated identity.
- All sizes preserve the objective rule; Standard/War/Large War deadlines and force values are printed per entry below. Difficulty never changes resources/stats. Default first visit: Regular/Standard.
- Startup/roster/production/army/visibility/AI/UI/save use the shared types in TECHNICAL_ARCHITECTURE; no per-entry controller or ARIA solution script.
- Build shared map assets once. Per entry, build `SkirmishScenario_SNNN.asset` and `ScenarioSetup_SNNN.asset` in `Assets/Game/Configs/SkirmishExpansion/Scenarios/SNNN/`; bind the shared objective/army/start/size assets and resolved initial placement arrays. Use `SkirmishDefinitionBuilder` (proposed), not hand-written Unity YAML.
- Map-specific acceptance: Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.
- Mandatory objective fixtures for every entry: Original vs replacement Barracks; both deaths on the same tick; deadline draw; loss of the whole field army with a surviving producer; hidden enemy health must remain last-observed.
- Shared class dependencies: `SkirmishSessionInitializationSystem`, `SkirmishScenarioSpawnSystem`, `SkirmishRosterProjectionSystem`, `SkirmishCapacityReservationSystem`, `SkirmishCapacityLifecycleSystem`, `SkirmishArmyGroupSystem`, `SkirmishResearchSystem`, `SkirmishEnemyStrategySystem`, `SkirmishCheckpointSystem`, `SkirmishResultSettlementSystem`, `SkirmishSessionCleanupSystem`, UI projections and extended `AriaSkirmishPlanSystem`.
- Enemy starts with the same profile/start resources and total combat roster. BA/FC are symmetric; BT/CE use two extra disclosed defender towers and A/B/reserve group placement from MAP_IMPLEMENTATION. All reinforcements are paid production, never scripted free waves.
- Infantry numbers below are individual soldiers, not squad cards. CAR/APC/TANK/AA/TH are platforms; logistics, delivery carriers, objective trucks and buildings are counted separately. Full vehicle tanks are additional listed endowment from certified role data; not invented Fuel stock.

## S073

**Industrial Basin · Base Assault · Ground Maneuver · Field Base** — handoff work ordinal **3**. Prototype compatibility mapping exists; certify this expanded revision separately.

**Bind:** catalog `S073`; definition `skirmish.s073`; scenario `scenario.skirmish.s073`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `G`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ba`. Recommended later size `Standard` is gated; first visit remains Standard.

**Opening and decisions:** Use initial infantry/car to scout freight avenue; recruit anti-armor before a tank commitment. Two intended approaches: Develop tanks for freight avenue with infantry cover, or use APC infantry through service ring while threatening supply. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s073.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject offensive-air queues in G while preserving transport/recon. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104802`, `130436`, `155994` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196686`, Veteran `262220`, Commander `327746`. Additional exposed sizes need a full Regular EN and FA win at War seed `393314` and Large War seed `458952`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S073/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S074

**Industrial Basin · Base Assault · Ground Maneuver · Established Base** — handoff work ordinal **74**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S074`; definition `skirmish.s074`; scenario `scenario.skirmish.s074`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `G`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;advanced_ground;objective_ba`. Recommended later size `Standard` is gated; first visit remains Standard.

**Opening and decisions:** Use the starting tank/APC to contest freight avenue, retaining one infantry squad at home. Two intended approaches: Press quickly with the established ground force, or invest in siege and attack via warehouse lane after scouting. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored/heavy APC; tank; radar; ground siege; transport helicopter; recon drone; logistics. Exclude attack helicopters, fighter/strike jets and transport plane. Counter contract: rocketeer squads before tanks; rifle/gunner support for anti-armor; optional unarmed transport/recon does not add offensive air. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 TANK | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 TANK | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 TANK | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s074.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject offensive-air queues in G while preserving transport/recon. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104803`, `130437`, `155995` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196687`, Veteran `262221`, Commander `327747`. Additional exposed sizes need a full Regular EN and FA win at War seed `393315` and Large War seed `458953`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S074/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S075

**Industrial Basin · Base Assault · Air Mobile · Field Base** — handoff work ordinal **75**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S075`; definition `skirmish.s075`; scenario `scenario.skirmish.s075`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `A`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Hold a cheap ground screen and build R2/Helipad only after the affordable counter window. Two intended approaches: Escort an APC ground push on service ring, or transition to attack/transport air while defending supply. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s075.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104804`, `130438`, `155996` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196688`, Veteran `262222`, Commander `327748`. Additional exposed sizes need a full Regular EN and FA win at War seed `393316` and Large War seed `458954`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S075/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S076

**Industrial Basin · Base Assault · Air Mobile · Established Base** — handoff work ordinal **76**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S076`; definition `skirmish.s076`; scenario `scenario.skirmish.s076`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `A`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Scout with infantry and the initial transport helicopter; AA protects the staging area. Two intended approaches: Lift infantry behind an observed flank near service ring, or establish offensive air with an affordable ground reserve. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** five infantry roles; car; fast/armored APC; radar; R1 AA; transport/attack helicopters; recon drone; fighter/strike jets; transport plane; logistics. Exclude tank, heavy APC and ground siege launcher. Counter contract: R1 ground AA before offensive helicopters; rocketeers handle enemy light armor; affordable air return/refuel is required. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 1 armored APC, 1 AA, 1 TH | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 1 CAR, 2 armored APC, 2 AA, 2 TH | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 CAR, 2 armored APC, 3 AA, 2 TH | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s076.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Reject tank/heavy APC/siege queues in A while keeping R1 AA available. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104805`, `130439`, `155997` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196689`, Veteran `262223`, Commander `327749`. Additional exposed sizes need a full Regular EN and FA win at War seed `393317` and Large War seed `458955`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S076/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S077

**Industrial Basin · Base Assault · Combined Arms · Field Base** — handoff work ordinal **77**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S077`; definition `skirmish.s077`; scenario `scenario.skirmish.s077`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `C`; start `F`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Choose between earlier ground armor and the R2 air facility; do not buy every unlock before defending. Two intended approaches: Concentrate a tank/infantry push along freight avenue, or pressure warehouse lane while air/recon supports the other approach. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Field begins R1: buy R2 facilities/readiness through normal costs.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 8 rifle, 4 rocketeer, 1 CAR, 1 armored APC | 450/120/350 | 2; 0/0 | 7/7 | 1/1/1 | 1080 s |
| War | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 12 rifle, 4 gunner, 4 rocketeer, 1 CAR, 2 armored APC | 675/180/525 | 3; 0/0 | 7/7 | 2/1/1 | 1500 s |
| LargeWar | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 16 rifle, 4 gunner, 4 rocketeer, 2 CAR, 2 armored APC | 900/240/700 | 3; 0/0 | 7/7 | 2/1/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s077.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify R1 missing-facility explanations, paid R2 transition and level-zero upgrades on every fresh start. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104806`, `130440`, `155998` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196690`, Veteran `262224`, Commander `327750`. Additional exposed sizes need a full Regular EN and FA win at War seed `393318` and Large War seed `458956`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S077/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.

## S078

**Industrial Basin · Base Assault · Combined Arms · Established Base** — handoff work ordinal **78**. One of the 117 remaining new catalog combinations.

**Bind:** catalog `S078`; definition `skirmish.s078`; scenario `scenario.skirmish.s078`; map `opmap.skirmish.industrial_basin`; objective `BA`; army `C`; start `E`. Prerequisite tickets: `SK-00;SK-01;SK-02;SK-03;SK-04;SK-05;SK-06;SK-10;SK-11;SK-12;SK-13`. Required capability tags: `ground;intel;transport;offensive_air;advanced_ground;advanced_air;objective_ba`. Recommended later size `War` is gated; first visit remains Standard.

**Opening and decisions:** Separate the starting tank/AA screen from transportable infantry; keep Fuel delivery protected. Two intended approaches: Make a coordinated ground-plus-lift assault, or raid exposed supply from service ring while armor holds freight avenue. These describe tactical options, not a mandatory click sequence or AI script.

**Roster:** all certified ground, infantry, recon, transport, offensive-air, anti-air and siege roles; logistics. Exclude unsupported abilities and noncombatant models advertised as combat roles. Counter contract: early rocketeers and R1 AA; infantry screens tanks/launchers, air counters cannot replace objective infantry. Established begins R2 with Helipad/intel and the table’s forces; category upgrades remain level zero and Airport remains unbuilt.

| Size | Player starting combat force | Enemy starting combat force | M / Oil / usable Fuel each | Logistics support each; objective trucks P/E | Starting structures P/E | I/V/logistics queues each | Deadline |
|---|---|---|---|---|---|---|---|
| Standard | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 12 rifle, 4 gunner, 4 rocketeer, 1 armored APC, 1 TANK, 1 AA, 1 TH | 900/240/700 | 3; 0/0 | 10/10 | 2/1/1 | 1080 s |
| War | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 16 rifle, 4 gunner, 8 rocketeer, 2 armored APC, 2 TANK, 1 AA, 2 TH | 1350/360/1050 | 4; 0/0 | 11/11 | 2/2/1 | 1500 s |
| LargeWar | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 24 rifle, 8 gunner, 8 rocketeer, 2 armored APC, 3 TANK, 2 AA, 2 TH | 1800/480/1400 | 4; 0/0 | 11/11 | 2/2/1 | 1800 s |

**Implementation sequence:** (1) Compile exactly the identity/profile/layout above and match all three size rows to [INITIAL_SETUP_MATRIX](../INITIAL_SETUP_MATRIX.csv), including caps/storage/BT substitution/CE support. (2) Bind the packet’s objective roles and all starting/group/producer/route anchors; simulate both viable approaches with real movement. (3) Install `SkirmishBaseAssaultObjectiveSystem` and its typed state; preserve the exact terminal/clock semantics. (4) Wire producer costs/queues, contextual controls and objective/army HUD from the resolved snapshot; add keys `skirmish.s078.{title,brief,objective,warning.*,result.*}` in EN/FA. (5) Expose the same public role/route/goal affordances to enemy and ARIA policies through their separate legal observations/actuators. (6) Verify checkpoint and result/replay, then publish only this entry’s certified size/difficulty matrix.

**Entry-specific checks:** Verify free starting facilities/forces are grants, not a hidden second debit; R2 persists while stat upgrades start at zero. Verify simultaneous armor/AA/air queues share category and Supply reservations correctly. Prove direct and flank base assaults; a supply raid by itself never awards Victory. Preserve in-progress prototype ownership; rail crossings/pipes need explicit traversability; no invented chain-explosion hazard.

**ARIA must play and win:** Regular seeds `104807`, `130441`, `155999` in both EN and FA at Standard: three full normal-speed runs per locale, at least two real wins in each, all traces/losses retained. Additional exposed difficulty wins at Standard/EN use Recruit `196691`, Veteran `262225`, Commander `327751`. Additional exposed sizes need a full Regular EN and FA win at War seed `393319` and Large War seed `458957`, plus their device/recovery gates. These samples supplement per-configuration legality checks; all four difficulties are required for final catalog completion. ARIA uses visible controls, no hidden knowledge/state mutation/extra resources, no human tactical intervention or restart in a counted win. A Draw is not a win.

**Completion artifact:** Config and code hashes; compiled setup/actual entity census; normal human win; ARIA matrix; loss/deadline/surrender and unique edge fixtures; saved-result/replay/checkpoint evidence; camera/map/EN/FA/device review. Store evidence under `Design/AgentReports/SkirmishExpansion/S078/` and update only this publication row after every required gate. Authoring a valid asset does not mark it Accepted.
