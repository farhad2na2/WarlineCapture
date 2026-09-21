# D02 — Civic Center: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.civic_center`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## O011 — Signals Across the Plaza

**Intent and decision:** Identify which public-service antenna is being used by the hostile network. Cross the exposed plaza quickly or use the longer covered colonnade.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o011` / `scenario.operations.o011` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 720 s after player control starts |
| Canonical fixture | Seed `1112`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(plaza_antenna,annex_antenna,clinic_antenna) -> INTERACT(service_log,15) -> EXTRACT(squad,exit.ground). Three distinct search positions and one carried log.

**Partial / failure:** Partial when Two antennas scanned and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All recon infantry survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered service_log sets evidence.d02.service; Victory sets success.o011. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O011.asset`, `OperationsObjectives_O011.asset`, and `ScenarioSetup_O011.asset` under `Assets/Game/Configs/Operations/Missions/O011/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o011.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A scan through an occluding civic building fails eligibility without revealing its hidden guard roster. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O012 — Shelter Access

**Intent and decision:** Open a walking route between a public shelter and the clinic. Clear the direct plaza lane or approach the same checkpoints from covered side streets.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o012` / `scenario.operations.o012` |
| Family / force / enemy | `PATROL` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 780 s after player control starts |
| Canonical fixture | Seed `1113`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** VISIT(shelter,plaza_corner,clinic_entry) -> CLEAR(route_guards) -> HOLD(clinic_entry,45). PROTECT(shelter,alive).

**Partial / failure:** Partial when Shelter and plaza_corner visited, route_guards cleared, shelter alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No protected public building damaged. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d02.shelter and sets success.o012. Apply the `PATROL` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O012.asset`, `OperationsObjectives_O012.asset`, and `ScenarioSetup_O012.asset` under `Assets/Game/Configs/Operations/Missions/O012/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRouteObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o012.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Removing a navigation blocker must replan the patrol route; a stale path cannot leave the objective permanently unreachable. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O013 — Backup Power

**Intent and decision:** Restore the civic annex and clinic backup generators. Split engineers with infantry escorts or keep the task force together and accept the longer work sequence.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o013` / `scenario.operations.o013` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1114`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(service_guards) -> (REPAIR(annex_generator) AND REPAIR(clinic_generator)) -> HOLD(service_lane,60). PROTECT(clinic,alive); 80 Materials.

**Partial / failure:** Partial when One generator restored and clinic alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Complete both repairs before elapsed 600 s. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Generator state persists; Victory sets milestone.d02.service and success.o013. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O013.asset`, `OperationsObjectives_O013.asset`, and `ScenarioSetup_O013.asset` under `Assets/Game/Configs/Operations/Missions/O013/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o013.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Power is represented by explicit restored site facts; no imaginary electrical-network connectivity can award completion. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O014 — Archives Transfer

**Intent and decision:** Move three record trucks from the service annex to protected storage. The short plaza crossing has poor cover; the loop gives enemies more time to reposition.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o014` / `scenario.operations.o014` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1115`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(annex_loading,30) -> ESCORT(record_trucks,2,route.main OR route.safe) -> HOLD(storage_entry,30). PROTECT(storage,alive).

**Partial / failure:** Partial when One truck unloaded and storage alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Deliver all three record trucks. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets evidence.d02.public_records and success.o014; route.d02.storage becomes Open. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O014.asset`, `OperationsObjectives_O014.asset`, and `ScenarioSetup_O014.asset` under `Assets/Game/Configs/Operations/Missions/O014/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o014.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Changing convoy route after the branch-lock waypoint is rejected with a visible reason, without teleporting trucks. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O015 — Shelter Evacuation

**Intent and decision:** Move residents out of a threatened shelter while its entrance stays open. Use two APC lifts or a covered walking corridor; each leaves different protection needs.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o015` / `scenario.operations.o015` |
| Family / force / enemy | `RESCUE` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1116`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(shelter_guards) -> RESCUE(shelter_residents,8,exit.ground) -> EXTRACT(squad,exit.ground). Twelve civilians; PROTECT(shelter,alive).

**Partial / failure:** Partial when Four residents delivered and shelter alive, with two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve residents delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d02.shelter_safe and success.o015; report actual harm independently. Apply the `RESCUE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O015.asset`, `OperationsObjectives_O015.asset`, and `ScenarioSetup_O015.asset` under `Assets/Game/Configs/Operations/Missions/O015/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o015.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Losing the escort stops neutral followers safely and exposes a transfer action; they must not silently join a hostile squad. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O016 — Service Annex Raid

**Intent and decision:** Remove the hostile command post from a service annex without destroying the adjacent records wing. Armor controls the plaza while infantry chooses front or rear entry.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o016` / `scenario.operations.o016` |
| Family / force / enemy | `RAID` / `FP_GROUND` / `EP_RAIDERS` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1117`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(annex_target) -> CLEAR(command_post_guards) -> INTERACT(command_orders,20) -> EXTRACT(squad,exit.ground). PROTECT(records_wing,alive).

**Partial / failure:** Partial when Command-post guards cleared and records_wing alive, two original infantry extracted; orders missing. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Records wing ends undamaged. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered orders set evidence.d02.command; Victory sets milestone.d02.threat and success.o016. Apply the `RAID` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O016.asset`, `OperationsObjectives_O016.asset`, and `ScenarioSetup_O016.asset` under `Assets/Game/Configs/Operations/Missions/O016/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o016.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Target confirmation uses live observation; a high city Intel value alone cannot satisfy the tactical SCAN node. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O017 — Public Square Hold

**Intent and decision:** Secure two plaza access points without leaving the service lane exposed. Split across both zones or clear one flank before moving the reserve.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o017` / `scenario.operations.o017` |
| Family / force / enemy | `SEIZE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1118`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (HOLD(square_west,60) AND HOLD(square_east,60)) -> HOLD(service_lane,90). PROTECT(annex_generator,alive).

**Partial / failure:** Partial when Either square zone secured and annex_generator alive; final lane hold incomplete. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both tanks survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d02.plaza and sets success.o017; generator final condition persists. Apply the `SEIZE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O017.asset`, `OperationsObjectives_O017.asset`, and `ScenarioSetup_O017.asset` under `Assets/Game/Configs/Operations/Missions/O017/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o017.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A contested zone resets continuous progress; parked armor alone cannot capture either square zone. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O018 — Rooftop Landing Zone

**Intent and decision:** Evacuate clinic staff from an authored elevated landing pad with a certified access ramp. Clear ground threats before landing or use one helicopter as a shuttle while infantry holds the ramp.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o018` / `scenario.operations.o018` |
| Family / force / enemy | `AIRLIFT` / `FP_AIR` / `EP_AIRFIELD` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1119`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(lz_threats) -> HOLD(clinic_lz,30) -> AIRLIFT(clinic_staff,8,clinic_lz,exit.air). Twelve passengers; PROTECT(clinic_lz_site,alive). No free rooftop climbing.

**Partial / failure:** Partial when Four staff delivered to exit.air and clinic_lz_site survives. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve staff delivered with both helicopters alive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d02.staff_evacuated and success.o018. The rooftop requires certified landing/ground access before authoring acceptance. Apply the `AIRLIFT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O018.asset`, `OperationsObjectives_O018.asset`, and `ScenarioSetup_O018.asset` under `Assets/Game/Configs/Operations/Missions/O018/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o018.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A pad at the wrong height cannot complete landing or unloading; count actual passengers at the extraction destination. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O019 — Hospital Service Watch

**Intent and decision:** Defend the hospital service road against infantry and an armored reserve. Choose forward anti-armor positions or protect the generator with a central reserve.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o019` / `scenario.operations.o019` |
| Family / force / enemy | `DEFENSE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1120`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(hospital_lane,210) AND CLEAR(assault_groups). PROTECT(hospital,alive); PROTECT(clinic_generator,alive). A arrives 75 s after warning at 45 s; B arrives 150 s after warning at 105 s.

**Partial / failure:** Partial when Both protected sites alive and half the assault combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Hospital health remains at least 75%. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d02.readiness and success.o019; damaged generator affects the saved service overlay. Apply the `DEFENSE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O019.asset`, `OperationsObjectives_O019.asset`, and `ScenarioSetup_O019.asset` under `Assets/Game/Configs/Operations/Missions/O019/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o019.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Restored generator state is loaded correctly and its destruction during this mission persists even if the hospital survives. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O020 — Civic Services Restored

**Intent and decision:** Restore an emergency service node, bring in equipment, and keep both plaza approaches open. Coordinate protection of workers and trucks instead of chasing every enemy.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o020` / `scenario.operations.o020` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O013/O016/O019 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1121`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (REPAIR(emergency_service) AND ESCORT(service_trucks,2,route.safe)) -> (HOLD(square_west,60) AND HOLD(square_east,60)) -> EXTRACT(squad,exit.ground). Three trucks; PROTECT(hospital,alive).

**Partial / failure:** Partial when Emergency service restored or two trucks delivered, hospital alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No public service site destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d02.stabilized and success.o020. Requires successful O013/O016/O019. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O020.asset`, `OperationsObjectives_O020.asset`, and `ScenarioSetup_O020.asset` under `Assets/Game/Configs/Operations/Missions/O020/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o020.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Two zone nodes can finish independently, but the final extraction node only activates after both complete. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
