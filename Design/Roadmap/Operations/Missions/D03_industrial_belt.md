# D03 — Industrial Belt: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.industrial_belt`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## Planned environment implementation — Demo 2

Assigned kit: **Logistics and Utilities; selective Perimeter**. Prioritize warehouses, containers, pallets, generators and transformers. Preserve freight spine, ring road, three separated service sites, truck turning bays and breach access. Bind repair visuals to actual site state; these missions gain no new production or Oil chain requirement.

Follow the [integration guide](../../../Demo2_Asset_Integration_Guide.md) and [verified source manifest](../../../VisualConfigs/Demo2_Environment_Asset_Manifest.json). P6/map ownership records adapted GUIDs, materials, typed role bindings and map/content hashes; D2-V1–V6 join every affected mission’s existing acceptance. Reuse qualified project-owned assets without copying gameplay state or treating a model as a certified mechanic. All mission IDs, finite force budgets, objective graphs and status claims below remain unchanged.

## O021 — Freight Ledger

**Intent and decision:** Trace diverted freight by inspecting three loading bays. The outer road allows safe movement; the central loading lane is faster but observed by guards.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o021` / `scenario.operations.o021` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 780 s after player control starts |
| Canonical fixture | Seed `1122`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(bay_north,bay_east,bay_south) -> INTERACT(freight_ledger,20) -> EXTRACT(squad,exit.ground). Each loading bay binds a distinct recon site.

**Partial / failure:** Partial when Two bays scanned and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No recon unit lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Extracted ledger sets evidence.d03.freight; Victory sets success.o021. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O021.asset`, `OperationsObjectives_O021.asset`, and `ScenarioSetup_O021.asset` under `Assets/Game/Configs/Operations/Missions/O021/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o021.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A scan of an adjacent bay cannot satisfy the selected bay through a large overlapping selection collider. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O022 — Workshop Convoy

**Intent and decision:** Deliver replacement tools to the repair workshop. The ring road has room for trucks; the freight spine offers a shorter route with two narrow turns.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o022` / `scenario.operations.o022` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1123`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(freight_junction) -> ESCORT(workshop_trucks,2,route.main OR route.safe) -> HOLD(workshop,45). Three cargo trucks; PROTECT(workshop_site,alive).

**Partial / failure:** Partial when One truck unloaded and workshop_site alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d03.workshop and sets success.o022. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O022.asset`, `OperationsObjectives_O022.asset`, and `ScenarioSetup_O022.asset` under `Assets/Game/Configs/Operations/Missions/O022/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o022.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Largest truck clearance is validated at both turns; a blocked truck exposes Hold/reroute rather than never-ending progress. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O023 — Three Service Stations

**Intent and decision:** Restore three separated fabrication-service stations. Parallel repair shortens the operation but spreads the escort; sequential repair makes rear security easier.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o023` / `scenario.operations.o023` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1124`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(station_guards) -> (REPAIR(station_a) AND REPAIR(station_b) AND REPAIR(station_c)) -> HOLD(workshop_lane,60). Three stations must remain alive; 120 Materials total.

**Partial / failure:** Partial when At least one station restored and alive; not all three complete. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All repair specialists survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Each station has a saved site state; Victory sets milestone.d03.service and success.o023. Stations do not create a new production economy. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O023.asset`, `OperationsObjectives_O023.asset`, and `ScenarioSetup_O023.asset` under `Assets/Game/Configs/Operations/Missions/O023/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o023.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** With 119 Materials in a negative fixture the third repair is visibly unaffordable, no negative balance or free completion. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O024 — Loading Yard Workers

**Intent and decision:** Extract workers from two disconnected loading pockets. Secure both pockets together or clear one and move its residents before approaching the other.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o024` / `scenario.operations.o024` |
| Family / force / enemy | `RESCUE` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1125`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (HOLD(pocket_a,30) AND HOLD(pocket_b,30)) -> RESCUE(workers,8,exit.ground) -> EXTRACT(squad,exit.ground). Twelve workers, six in each pocket; both rescue interactions required.

**Partial / failure:** Partial when Four workers delivered and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve workers delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d03.workers_safe and success.o024; survivor IDs identify their source pocket. Apply the `RESCUE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O024.asset`, `OperationsObjectives_O024.asset`, and `ScenarioSetup_O024.asset` under `Assets/Game/Configs/Operations/Missions/O024/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o024.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Interacting with pocket_a cannot release pocket_b workers or complete their protected route before it is secured. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O025 — Fuel Route Interdiction

**Intent and decision:** Stop hostile fuel transports leaving the district while keeping the civilian service depot intact. Choose one strong blocking point or two lighter road teams.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o025` / `scenario.operations.o025` |
| Family / force / enemy | `INTERDICT` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1126`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(cargo_approach) -> STOP(fuel_trucks,2) -> HOLD(depot_road,45). Three hostile cargo trucks; PROTECT(service_depot,alive).

**Partial / failure:** Partial when One truck stopped and service_depot alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Stop all three trucks without losing a tank. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory marks route.d03.hostile_freight Blocked and success.o025; no captured fuel is added to persistent wallets. Apply the `INTERDICT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O025.asset`, `OperationsObjectives_O025.asset`, and `ScenarioSetup_O025.asset` under `Assets/Game/Configs/Operations/Missions/O025/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsInterdictionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o025.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Truck destruction has normal game damage only; no unimplemented chain explosion may be required to meet the stop count. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O026 — Depot Gate

**Intent and decision:** Enter the fortified freight depot and recover dispatch orders. Armor can open a main approach or infantry can protect the breaching group from a side lane.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o026` / `scenario.operations.o026` |
| Family / force / enemy | `BREACH` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1127`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(depot) -> BREACH(depot_gate) -> CLEAR(dispatch_guards) -> INTERACT(dispatch_orders,20) -> EXTRACT(squad,exit.ground). PROTECT(workshop_site,alive).

**Partial / failure:** Partial when Gate traversed and dispatch_guards cleared, workshop_site alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both APCs survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered orders set evidence.d03.dispatch; Victory sets milestone.d03.threat and success.o026. Apply the `BREACH` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O026.asset`, `OperationsObjectives_O026.asset`, and `ScenarioSetup_O026.asset` under `Assets/Game/Configs/Operations/Missions/O026/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o026.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** The cleared gate must permit the largest required player vehicle; no victory from a path still blocked by the visual gate. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O027 — Freight Junction

**Intent and decision:** Take the road and rail-side loading approaches to separate hostile movement. Both objectives are land zones; rail vehicles and rail simulation are not required.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o027` / `scenario.operations.o027` |
| Family / force / enemy | `SEIZE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1128`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (HOLD(road_junction,75) AND HOLD(loading_apron,75)) -> CLEAR(counterattack). Wave B targets whichever zone was captured first through a reachable public approach.

**Partial / failure:** Partial when One zone completed and at least half of counterattack combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No repair specialist lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d03.freight_junction and sets success.o027. Apply the `SEIZE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O027.asset`, `OperationsObjectives_O027.asset`, and `ScenarioSetup_O027.asset` under `Assets/Game/Configs/Operations/Missions/O027/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o027.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Counterattack selects from observed or public zone state, never from hidden player unit positions. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O028 — Supply Office Records

**Intent and decision:** Recover two independent records from a service office and weighbridge hut. Carry them with separate squads for speed or keep one group together to protect both carriers.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o028` / `scenario.operations.o028` |
| Family / force / enemy | `RAID` / `FP_LIGHT` / `EP_CELL` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1129`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(supply_office) -> CLEAR(record_guards) -> (INTERACT(office_record,15) AND INTERACT(hut_record,15)) -> EXTRACT(squad,exit.ground). Both evidence objects required; PROTECT(supply_office,alive).

**Partial / failure:** Partial when One record delivered and two original infantry extracted, supply_office alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No evidence carrier dies. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Persist only actually delivered evidence IDs; Victory sets success.o028. Apply the `RAID` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O028.asset`, `OperationsObjectives_O028.asset`, and `ScenarioSetup_O028.asset` under `Assets/Game/Configs/Operations/Missions/O028/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o028.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Two pickups by one carrier are permitted if capacity allows both authored evidence slots; duplicate object identity is never counted as the other record. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O029 — Workshop Night Watch

**Intent and decision:** Protect the workshop and a completed service station through a two-front attack. Night lighting changes presentation only; combat visibility uses the certified shared model.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o029` / `scenario.operations.o029` |
| Family / force / enemy | `DEFENSE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1130`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(workshop_lane,240) AND CLEAR(assault_groups). PROTECT(workshop_site,alive); PROTECT(station_a,alive). A arrives 90 s via freight spine; B 180 s via ring road, with 30/45 s warnings.

**Partial / failure:** Partial when Both protected sites survive and half of assault combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Workshop ends at 75% health or higher. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d03.readiness and success.o029; station damage persists. Apply the `DEFENSE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O029.asset`, `OperationsObjectives_O029.asset`, and `ScenarioSetup_O029.asset` under `Assets/Game/Configs/Operations/Missions/O029/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o029.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Night appearance cannot leak enemies via minimap or silently lower detection distance outside the chosen visibility config. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O030 — Industrial Restart

**Intent and decision:** Reopen the freight spine, restore a damaged backup station, and deliver restart equipment. Divide infantry security from the armored escort or clear the road first.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o030` / `scenario.operations.o030` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O023/O026/O029 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1131`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** BREACH(freight_barrier) -> (REPAIR(backup_station) AND ESCORT(restart_trucks,2,route.main)) -> HOLD(freight_junction,90) -> EXTRACT(squad,exit.ground). Three trucks; PROTECT(workshop_site,alive).

**Partial / failure:** Partial when Barrier traversed and either backup_station restored or two trucks delivered; workshop alive and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All trucks delivered with no protected station destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d03.stabilized and success.o030. Requires successful O023/O026/O029. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O030.asset`, `OperationsObjectives_O030.asset`, and `ScenarioSetup_O030.asset` under `Assets/Game/Configs/Operations/Missions/O030/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o030.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Service restoration and truck delivery share no counter; one event cannot satisfy both mandatory branches. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
