# D06 — Airport Perimeter: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.airport_perimeter`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## Planned environment implementation — Demo 2

Assigned kit: **Perimeter, Logistics and Utilities**. Use warehouse/container yards, radio equipment, barriers and fences around existing runway/hangar infrastructure. Protect perimeter loop, both LZs, ground evacuation and flight/vehicle clearances. A demo plane wreck is optional scenery and supplies no operational aircraft behavior.

Follow the [integration guide](../../../Demo2_Asset_Integration_Guide.md) and [verified source manifest](../../../VisualConfigs/Demo2_Environment_Asset_Manifest.json). P6/map ownership records adapted GUIDs, materials, typed role bindings and map/content hashes; D2-V1–V6 join every affected mission’s existing acceptance. Reuse qualified project-owned assets without copying gameplay state or treating a model as a certified mechanic. All mission IDs, finite force budgets, objective graphs and status claims below remain unchanged.

## O051 — Perimeter Radar Check

**Intent and decision:** Inspect the perimeter radar nodes before opening air access. Use the exposed perimeter road or covered hangar service lanes.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o051` / `scenario.operations.o051` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1152`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(radar_north,radar_south,terminal_relay) -> INTERACT(radar_log,15) -> EXTRACT(squad,exit.ground). Three recon sites; no automatic global radar reveal.

**Partial / failure:** Partial when Two sites scanned and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All recon units survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered log sets evidence.d06.radar; Victory sets success.o051. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O051.asset`, `OperationsObjectives_O051.asset`, and `ScenarioSetup_O051.asset` under `Assets/Game/Configs/Operations/Missions/O051/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o051.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Radar inspection changes only declared intel facts and cannot reveal hidden targets outside the public visibility contract. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O052 — Terminal Ground Evacuation

**Intent and decision:** Move stranded terminal staff to a ground safe point while air access is unavailable. APC shuttles are fast; the sheltered service walk preserves vehicle reserves.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o052` / `scenario.operations.o052` |
| Family / force / enemy | `RESCUE` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1153`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(terminal_entry,30) -> RESCUE(terminal_staff,8,exit.ground) -> EXTRACT(squad,exit.ground). Twelve staff; PROTECT(terminal_service,alive).

**Partial / failure:** Partial when Four staff delivered and terminal_service alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve staff delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d06.terminal_safe and success.o052. Apply the `RESCUE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O052.asset`, `OperationsObjectives_O052.asset`, and `ScenarioSetup_O052.asset` under `Assets/Game/Configs/Operations/Missions/O052/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o052.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A mission with air disabled cannot expose a helicopter-only suggested rescue route in the HUD or ARIA plan. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O053 — Air Access Service

**Intent and decision:** Restore two airfield service nodes before air operations resume. Work sequentially behind one escort or split repair teams between runway edge and hangar apron.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o053` / `scenario.operations.o053` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1154`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(service_guards) -> (REPAIR(runway_service) AND REPAIR(hangar_service)) -> HOLD(service_road,60). Both sites alive; 80 Materials. These are repairable service props, not runway mesh construction.

**Partial / failure:** Partial when One service node restored and alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both repair specialists survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d06.service and success.o053; service state changes future approved air-access overlays. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O053.asset`, `OperationsObjectives_O053.asset`, and `ScenarioSetup_O053.asset` under `Assets/Game/Configs/Operations/Missions/O053/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o053.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Restoring service nodes does not bypass landing-reservation or runway-clearance checks in the shared transport system. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O054 — Perimeter Supply Convoy

**Intent and decision:** Bring three equipment trucks through the airport perimeter. The outer road is long and open; the service lane has cover but requires careful traffic handling.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o054` / `scenario.operations.o054` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1155`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(perimeter_junction) -> ESCORT(equipment_trucks,2,route.main OR route.safe) -> HOLD(hangar_apron,45). Three trucks; PROTECT(hangar_service,alive).

**Partial / failure:** Partial when One truck delivered and hangar_service alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All trucks delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d06.supply and sets success.o054. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O054.asset`, `OperationsObjectives_O054.asset`, and `ScenarioSetup_O054.asset` under `Assets/Game/Configs/Operations/Missions/O054/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o054.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Convoy traffic must avoid occupied landing-reservation space and cannot satisfy unloading by clipping through aircraft. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O055 — Control Office Records

**Intent and decision:** Recover access records from the airfield control office without damaging civilian navigation equipment. Clear the front approach or use the longer service corridor.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o055` / `scenario.operations.o055` |
| Family / force / enemy | `RAID` / `FP_LIGHT` / `EP_CELL` |
| Availability | Any attempt of local slot 1 or 2; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1156`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(control_office) -> CLEAR(office_guards) -> INTERACT(access_records,20) -> EXTRACT(squad,exit.ground). PROTECT(navigation_service,alive).

**Partial / failure:** Partial when Office guards cleared and navigation service alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Navigation service remains undamaged. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered records set evidence.d06.access; Victory sets success.o055. Apply the `RAID` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O055.asset`, `OperationsObjectives_O055.asset`, and `ScenarioSetup_O055.asset` under `Assets/Game/Configs/Operations/Missions/O055/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o055.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Object interaction and building attack actions have distinct targets; a tap on records cannot issue attack against protected equipment. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O056 — Hangar Strongpoint

**Intent and decision:** Remove the strongpoint blocking the service apron. Breach the hangar gate with armor while infantry clears a flanking lane and protects service equipment.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o056` / `scenario.operations.o056` |
| Family / force / enemy | `BREACH` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1157`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(hangar_target) -> BREACH(hangar_gate) -> CLEAR(strongpoint_guards) -> INTERACT(airfield_orders,20) -> EXTRACT(squad,exit.ground). PROTECT(hangar_service,alive).

**Partial / failure:** Partial when Gate traversed and guards cleared, hangar service alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No protected service site destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered orders set evidence.d06.command; Victory sets milestone.d06.threat and success.o056. Apply the `BREACH` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O056.asset`, `OperationsObjectives_O056.asset`, and `ScenarioSetup_O056.asset` under `Assets/Game/Configs/Operations/Missions/O056/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o056.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Hangar destruction presentation cannot remove the protected service entity or erase required navigation data during breach. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O057 — Landing Zone Pair

**Intent and decision:** Secure two landing-zone approaches against grounded anti-air teams. Split to occupy both or concentrate on one threat cluster before switching sides.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o057` / `scenario.operations.o057` |
| Family / force / enemy | `SEIZE` / `FP_GROUND` / `EP_AIRFIELD` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1158`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(aa_teams) -> (HOLD(lz_north,75) AND HOLD(lz_south,75)) -> CLEAR(counterattack). All four aa_teams infantry spawn initially as an explicit EP_AIRFIELD subset, not an additional budget. Remaining EP entities bind counterattack; its initial guards and later reserves all count.

**Partial / failure:** Partial when All aa_teams cleared and one landing zone held. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No anti-armor infantry lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d06.air_access and sets success.o057; actual landing rules still apply. Apply the `SEIZE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O057.asset`, `OperationsObjectives_O057.asset`, and `ScenarioSetup_O057.asset` under `Assets/Game/Configs/Operations/Missions/O057/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o057.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Counting anti-air clearance includes all four bound AA entities; catalog validation rejects assigning any required first-phase AA entity to a wave triggered by its own clearance. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O058 — Air Bridge Evacuation

**Intent and decision:** Carry twelve passengers from the reopened service apron to safety. Use both helicopters simultaneously or keep one reserve while infantry holds the ground route.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o058` / `scenario.operations.o058` |
| Family / force / enemy | `AIRLIFT` / `FP_AIR` / `EP_AIRFIELD` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1159`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(lz_threats) -> HOLD(apron_lz,30) -> AIRLIFT(evacuees,8,apron_lz,exit.air). Twelve passengers; PROTECT(terminal_service,alive).

**Partial / failure:** Partial when Four passengers delivered and terminal service alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve delivered with both helicopters alive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d06.air_bridge and success.o058; no persistent Fuel reward. Apply the `AIRLIFT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O058.asset`, `OperationsObjectives_O058.asset`, and `ScenarioSetup_O058.asset` under `Assets/Game/Configs/Operations/Missions/O058/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o058.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Save during boarding, restore and complete; seats, passenger identities and departure reservations remain consistent. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O059 — Runway Service Defense

**Intent and decision:** Defend runway support and the terminal service node from an armored raid. Forward anti-armor teams can slow the road attack while a reserve covers the terminal lane.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o059` / `scenario.operations.o059` |
| Family / force / enemy | `DEFENSE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1160`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(runway_service_zone,240) AND CLEAR(assault_groups). PROTECT(runway_service,alive); PROTECT(terminal_service,alive). A arrives at 90 s via perimeter road, B at 180 s via service lane with 30/45 s warnings.

**Partial / failure:** Partial when Both service sites alive and half the assault combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both service sites end at 75% health or higher. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d06.readiness and success.o059. Apply the `DEFENSE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O059.asset`, `OperationsObjectives_O059.asset`, and `ScenarioSetup_O059.asset` under `Assets/Game/Configs/Operations/Missions/O059/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o059.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Terminal result freezes remaining wave requests; late callbacks cannot spawn enemies into the result or next mission. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O060 — Airport Reopened

**Intent and decision:** Restore a backup service node, evacuate the last passenger group and hold the public access road. Allocate airlift protection and repair cover while maintaining a ground reserve.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o060` / `scenario.operations.o060` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O053/O056/O059 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1161`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(apron_guards) -> (REPAIR(backup_service) AND AIRLIFT(final_passengers,8,apron_lz,exit.air)) -> HOLD(public_access,90) -> EXTRACT(squad,exit.ground). Twelve passengers; PROTECT(terminal_service,alive); secure apron_lz with HOLD(apron_lz,30) before boarding.

**Partial / failure:** Partial when Four passengers delivered or backup_service restored; terminal alive and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve delivered with no protected service site destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d06.stabilized and success.o060. Requires successful O053/O056/O059. This completes the local arc, not an automatic city win; city thresholds still apply. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O060.asset`, `OperationsObjectives_O060.asset`, and `ScenarioSetup_O060.asset` under `Assets/Game/Configs/Operations/Missions/O060/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o060.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** The sixth district finale and first stable End Day must not award city completion early; require two consecutive qualifying End Days. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
