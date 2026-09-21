# D01 — Old Quarter: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.old_quarter`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## O001 — Street Signals

**Intent and decision:** Locate the active relay by checking three known search courtyards. Split scouts for speed or keep the squad together for protection; courtyard visibility differs from street visibility.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o001` / `scenario.operations.o001` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 720 s after player control starts |
| Canonical fixture | Seed `1102`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | Vertical slice (also B12); P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(signal_a,signal_b,signal_c) -> INTERACT(relay_evidence,15) -> EXTRACT(squad,exit.ground). Three scan sites; one carried evidence object. Wave A enters route.main after the first completed scan; B after evidence pickup.

**Partial / failure:** Partial when At least two scans complete and two original infantry extracted; evidence may be missing. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Finish all three scans without losing a recon unit. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Extracted relay_evidence sets evidence.d01.relay; public relay hint persists. Victory sets success.o001. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O001.asset`, `OperationsObjectives_O001.asset`, and `ScenarioSetup_O001.asset` under `Assets/Game/Configs/Operations/Missions/O001/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o001.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Scanning the same courtyard three times counts once; a dropped evidence object is recoverable and cannot be extracted twice. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O002 — Clinic Supply Route

**Intent and decision:** Bring three medical trucks to the clinic. The direct street is exposed; the service loop is longer but offers infantry cover. Scout and choose before releasing the convoy.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o002` / `scenario.operations.o002` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1103`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | Vertical slice (also B12); P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(junction) -> ESCORT(aid_trucks,2,route.main OR route.safe) -> HOLD(clinic,30). Exactly three cargo trucks; public Go/Hold control and route choice. PROTECT(clinic,alive).

**Partial / failure:** Partial when One truck unloaded and clinic survives; final clinic hold may be incomplete. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Deliver all three trucks. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Each unloaded truck contributes delivery facts; Victory marks route.d01.clinic Open and success.o002. No medical-healing subsystem is implied. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O002.asset`, `OperationsObjectives_O002.asset`, and `ScenarioSetup_O002.asset` under `Assets/Game/Configs/Operations/Missions/O002/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o002.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A truck reaching the clinic empty does not count; destroyed cargo cannot be recovered by replaying unload. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O003 — Courtyard Water Point

**Intent and decision:** Restore two neighborhood service pumps while preserving the clinic. Repair sequentially with full cover or split the two specialists to reduce exposure time.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o003` / `scenario.operations.o003` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1104`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | Vertical slice (also B12); P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(pump_guards) -> (REPAIR(pump_west) AND REPAIR(pump_east)) -> HOLD(service_court,60). PROTECT(clinic,alive); both pumps must remain alive. Repair uses 80 Materials total.

**Partial / failure:** Partial when One pump restored and still alive, clinic alive; other pump may remain damaged. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both repair specialists survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Persist each pump final state; Victory sets milestone.d01.service and success.o003, a finale prerequisite. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O003.asset`, `OperationsObjectives_O003.asset`, and `ScenarioSetup_O003.asset` under `Assets/Game/Configs/Operations/Missions/O003/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o003.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Interrupt repair under attack, resume without a second charge, then save/restore between the two pumps. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O004 — The Quiet Archive

**Intent and decision:** Recover a local logistics record from a guarded archive without destroying adjacent homes. Enter from the front court after clearing guards or use the longer rear foot lane.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o004` / `scenario.operations.o004` |
| Family / force / enemy | `RAID` / `FP_LIGHT` / `EP_CELL` |
| Availability | Any attempt of local slot 1 or 2; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 780 s after player control starts |
| Canonical fixture | Seed `1105`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(archive) -> CLEAR(archive_guards) -> INTERACT(archive_evidence,20) -> EXTRACT(squad,exit.ground). PROTECT(archive,alive); evidence pickup requires the building to survive.

**Partial / failure:** Partial when Archive confirmed and guards cleared, archive alive, two original infantry extracted; evidence not delivered. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No protected home destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered evidence sets evidence.d01.archive. Protected-home damage applies harm penalties; Victory sets success.o004. Apply the `RAID` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O004.asset`, `OperationsObjectives_O004.asset`, and `ScenarioSetup_O004.asset` under `Assets/Game/Configs/Operations/Missions/O004/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o004.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Destroying the archive before pickup makes the required objective impossible and cannot become Victory from guard kills. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O005 — Courtyard Evacuation

**Intent and decision:** Escort twelve residents to a safe perimeter. Move one protected group on foot or use APC seats in separate shuttles while infantry holds the collection point.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o005` / `scenario.operations.o005` |
| Family / force / enemy | `RESCUE` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1106`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(rescue_court,30) -> RESCUE(residents,8,exit.ground) -> EXTRACT(squad,exit.ground). Twelve civilian identities at rescue_court; route.safe is walkable and vehicle-compatible.

**Partial / failure:** Partial when Four residents delivered and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Deliver all twelve residents. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Persist delivered/dead counts; Victory sets report.d01.evacuation and success.o005. Civilian density is not reduced as a benefit. Apply the `RESCUE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O005.asset`, `OperationsObjectives_O005.asset`, and `ScenarioSetup_O005.asset` under `Assets/Game/Configs/Operations/Missions/O005/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o005.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Boarded civilians do not count as saved; carrier destruction applies the actual passenger result exactly once. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O006 — Market Cell

**Intent and decision:** Disable the cell headquarters inside a walled store yard and recover its roster. Armor can open the main gate while infantry uses a cover lane; nearby market stalls are protected.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o006` / `scenario.operations.o006` |
| Family / force / enemy | `BREACH` / `FP_GROUND` / `EP_RAIDERS` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1107`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(yard) -> BREACH(yard_gate) -> CLEAR(cell_hq_guards) -> INTERACT(cell_roster,20) -> EXTRACT(squad,exit.ground). PROTECT(market_service,alive).

**Partial / failure:** Partial when Gate traversed and cell guards cleared, market_service survives, two original infantry extracted; roster missing. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No protected market structure destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered roster sets evidence.d01.cell; Victory sets milestone.d01.threat and success.o006, a finale prerequisite. Apply the `BREACH` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O006.asset`, `OperationsObjectives_O006.asset`, and `ScenarioSetup_O006.asset` under `Assets/Game/Configs/Operations/Missions/O006/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o006.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Gate health reaching zero must release path blocking; killing defenders from outside without traversing cannot satisfy BREACH. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O007 — Two-Lane Patrol

**Intent and decision:** Establish access along two connected neighborhood lanes. Patrol the clinic side first to secure a retreat or the relay side first to delay enemy movement.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o007` / `scenario.operations.o007` |
| Family / force / enemy | `PATROL` / `FP_LIGHT` / `EP_CELL` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 720 s after player control starts |
| Canonical fixture | Seed `1108`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** VISIT(patrol_start,clinic_corner,relay_corner,patrol_end) -> CLEAR(route_patrol) -> HOLD(patrol_end,45). route.main and route.flank reach the same ordered points by different approaches.

**Partial / failure:** Partial when First three waypoints visited and route_patrol cleared; last hold incomplete. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No squad loss. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d01.patrol and sets success.o007. This does not substitute for the abstract Patrol action receipt. Apply the `PATROL` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O007.asset`, `OperationsObjectives_O007.asset`, and `ScenarioSetup_O007.asset` under `Assets/Game/Configs/Operations/Missions/O007/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRouteObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o007.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Driving an APC or moving one surviving infantry through all waypoints cannot satisfy the dismounted two-person visit rule. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O008 — Courier Cutoff

**Intent and decision:** Stop hostile dispatch trucks without blocking the public clinic route. Prepare a short ambush at the junction or divide infantry and armor between two exits.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o008` / `scenario.operations.o008` |
| Family / force / enemy | `INTERDICT` / `FP_GROUND` / `EP_RAIDERS` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 780 s after player control starts |
| Canonical fixture | Seed `1109`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(cargo_approach) -> STOP(courier_trucks,2) -> INTERACT(dispatch_case,15) -> EXTRACT(squad,exit.ground). Three hostile cargo trucks on route.enemy_cargo; dispatch_case drops from the first stopped truck.

**Partial / failure:** Partial when At least one courier truck stopped and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Stop all three trucks. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered case sets evidence.d01.dispatch; Victory sets success.o008. No case is created if every truck escapes. Apply the `INTERDICT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O008.asset`, `OperationsObjectives_O008.asset`, and `ScenarioSetup_O008.asset` under `Assets/Game/Configs/Operations/Missions/O008/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsInterdictionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o008.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A truck crossing its exit on the same tick as lethal damage follows death-before-exit ordering and is recorded only once. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O009 — Clinic Under Pressure

**Intent and decision:** Keep the restored public clinic available through attacks from two lanes. Hold a forward intersection or preserve a mobile reserve near the clinic.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o009` / `scenario.operations.o009` |
| Family / force / enemy | `DEFENSE` / `FP_GROUND` / `EP_RAIDERS` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1110`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(clinic,180) AND CLEAR(assault_groups). PROTECT(clinic,alive). A warns at elapsed 30 s and arrives at 60 s via route.main; B warns at 90 s and arrives at 135 s via route.flank. All EP combat units belong to assault_groups.

**Partial / failure:** Partial when Clinic alive at deadline and at least half the assault combat entities destroyed; continuous hold or full clearance incomplete. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Clinic ends at 75% health or higher. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Final clinic state persists; Victory sets milestone.d01.readiness and success.o009, a finale prerequisite. Apply the `DEFENSE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O009.asset`, `OperationsObjectives_O009.asset`, and `ScenarioSetup_O009.asset` under `Assets/Game/Configs/Operations/Missions/O009/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o009.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Final hostile death and clinic destruction in one tick yields Defeat, never a Victory popup. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O010 — Old Quarter Reopened

**Intent and decision:** Reconnect the clinic, service court and market in one operation. Choose an early split to restore the last service node or concentrate on the hostile roadblock before spreading out.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o010` / `scenario.operations.o010` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O003/O006/O009 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1111`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** BREACH(roadblock_gate) -> (REPAIR(service_backup) AND ESCORT(reopening_trucks,2,route.safe)) -> HOLD(market_square,90) -> EXTRACT(squad,exit.ground). Three friendly cargo trucks; PROTECT(clinic,alive).

**Partial / failure:** Partial when Gate traversed and either service_backup restored or two trucks delivered; clinic alive and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks delivered and no civilian deaths. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d01.stabilized and success.o010; service_backup and route.d01.market reflect final facts. Requires successful O003/O006/O009. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O010.asset`, `OperationsObjectives_O010.asset`, and `ScenarioSetup_O010.asset` under `Assets/Game/Configs/Operations/Missions/O010/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o010.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** An unrelated old service flag cannot auto-complete the backup-site repair; a retry with that exact already-restored site follows the verified restored-site rule without a second charge. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
