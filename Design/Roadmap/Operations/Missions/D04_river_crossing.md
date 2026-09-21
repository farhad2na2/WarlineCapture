# D04 — River Crossing: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.river_crossing`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## O031 — Crossing Survey

**Intent and decision:** Inspect the two crossings and the quay road before committing vehicles. The nearer crossing is exposed; the farther one offers cover and a longer return.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o031` / `scenario.operations.o031` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 780 s after player control starts |
| Canonical fixture | Seed `1132`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(crossing_west,crossing_east,quay_road) -> INTERACT(crossing_log,15) -> EXTRACT(squad,exit.ground). Three ground-accessible recon sites.

**Partial / failure:** Partial when Two sites scanned and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No recon unit lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered log sets evidence.d04.crossings; Victory sets success.o031. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O031.asset`, `OperationsObjectives_O031.asset`, and `ScenarioSetup_O031.asset` under `Assets/Game/Configs/Operations/Missions/O031/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o031.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Water is not walkable fallback terrain; path failure must report the unavailable route without teleporting scouts. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O032 — Quayside Evacuation

**Intent and decision:** Bring residents from the quay to a land evacuation point. Use APCs on the exposed road or a slower protected walking lane; no boat system is required.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o032` / `scenario.operations.o032` |
| Family / force / enemy | `RESCUE` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1133`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(quay_shelter,30) -> RESCUE(quay_residents,8,exit.ground) -> EXTRACT(squad,exit.ground). Twelve residents; route.safe stays entirely on land.

**Partial / failure:** Partial when Four residents delivered and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve residents delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d04.quay_safe and success.o032. Apply the `RESCUE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O032.asset`, `OperationsObjectives_O032.asset`, and `ScenarioSetup_O032.asset` under `Assets/Game/Configs/Operations/Missions/O032/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o032.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A civilian falling outside navigable land is a validation failure, not an automatic extracted or dead fact. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O033 — Crossing Service Post

**Intent and decision:** Restore service equipment at both ends of the existing crossing. The bridge remains structurally traversable; the challenge is protecting work on opposite banks.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o033` / `scenario.operations.o033` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1134`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(post_guards) -> (REPAIR(west_service_post) AND REPAIR(east_service_post)) -> HOLD(crossing_midpoint,60). Both posts alive; 80 Materials.

**Partial / failure:** Partial when One post restored and alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both repair specialists survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d04.service and success.o033; restored posts open route.d04.service. No mesh/bridge reconstruction mechanic is implied. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O033.asset`, `OperationsObjectives_O033.asset`, and `ScenarioSetup_O033.asset` under `Assets/Game/Configs/Operations/Missions/O033/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o033.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Repairing a post cannot replace bridge geometry or hide an invalid vehicle navigation surface. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O034 — Cross-River Aid

**Intent and decision:** Take three aid trucks over one of two certified crossings. Choose the narrow shorter bridge or the broader detour and assign infantry to its far approach.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o034` / `scenario.operations.o034` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1135`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(far_approach) -> ESCORT(aid_trucks,2,route.main OR route.safe) -> HOLD(aid_depot,45). Three trucks; PROTECT(aid_depot_site,alive).

**Partial / failure:** Partial when One truck delivered and aid_depot_site alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d04.aid and sets success.o034. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O034.asset`, `OperationsObjectives_O034.asset`, and `ScenarioSetup_O034.asset` under `Assets/Game/Configs/Operations/Missions/O034/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o034.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A stopped truck on the bridge must not permanently reserve the entire road after destruction or checkpoint restoration. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O035 — Quay Road Patrol

**Intent and decision:** Clear foot access along the riverfront without pursuing enemies into an unsafe vehicle bottleneck. Use covered quay approaches or keep to the elevated service road.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o035` / `scenario.operations.o035` |
| Family / force / enemy | `PATROL` / `FP_LIGHT` / `EP_CELL` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1136`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** VISIT(west_post,quay_steps,warehouse_corner,east_post) -> CLEAR(quay_patrol) -> HOLD(east_post,45). Steps are a certified infantry path; route.safe avoids them.

**Partial / failure:** Partial when Three waypoints visited and quay_patrol cleared. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No squad loss. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d04.quay_walk and sets success.o035. Apply the `PATROL` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O035.asset`, `OperationsObjectives_O035.asset`, and `ScenarioSetup_O035.asset` under `Assets/Game/Configs/Operations/Missions/O035/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRouteObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o035.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Infantry-only steps reject vehicle orders with an alternate route; ARIA must not repeatedly issue the same impossible move. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O036 — Warehouse Gate

**Intent and decision:** Clear the hostile warehouse controlling the crossing and recover route records. Breach the road-facing gate while infantry protects the quay flank.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o036` / `scenario.operations.o036` |
| Family / force / enemy | `BREACH` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1137`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(warehouse) -> BREACH(warehouse_gate) -> CLEAR(warehouse_guards) -> INTERACT(route_records,20) -> EXTRACT(squad,exit.ground). PROTECT(aid_depot_site,alive).

**Partial / failure:** Partial when Gate traversed and warehouse_guards cleared, depot alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No protected quay structure destroyed. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered records set evidence.d04.routes; Victory sets milestone.d04.threat and success.o036. Apply the `BREACH` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O036.asset`, `OperationsObjectives_O036.asset`, and `ScenarioSetup_O036.asset` under `Assets/Game/Configs/Operations/Missions/O036/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o036.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** The warehouse footprint cannot swallow the ground extraction anchor or make the return path unreachable after gate destruction. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O037 — Twin Crossing Control

**Intent and decision:** Secure both crossing exits against a reserve moving along the river road. Split armor between exits or establish one zone and rotate a reserve to the other.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o037` / `scenario.operations.o037` |
| Family / force / enemy | `SEIZE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1138`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (HOLD(west_exit,75) AND HOLD(east_exit,75)) -> CLEAR(counterattack). PROTECT(west_service_post,alive); PROTECT(east_service_post,alive).

**Partial / failure:** Partial when One exit secured and both posts alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both tanks survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d04.crossings and sets success.o037. Apply the `SEIZE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O037.asset`, `OperationsObjectives_O037.asset`, and `ScenarioSetup_O037.asset` under `Assets/Game/Configs/Operations/Missions/O037/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o037.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A zone on the opposite bank cannot include units through a large radius that crosses inaccessible water; author its exact footprint. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O038 — River Road Cutoff

**Intent and decision:** Stop a hostile convoy before it leaves the river district. Block the broad road or use infantry to cover one exit while armor holds the other.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o038` / `scenario.operations.o038` |
| Family / force / enemy | `INTERDICT` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1139`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(enemy_staging) -> STOP(cargo_trucks,2) -> HOLD(road_junction,45). Three hostile cargo trucks; exit route chosen and saved at launch, observable through scouting.

**Partial / failure:** Partial when One cargo truck stopped. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks stopped. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory blocks route.d04.hostile_supply and sets success.o038. Apply the `INTERDICT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O038.asset`, `OperationsObjectives_O038.asset`, and `ScenarioSetup_O038.asset` under `Assets/Game/Configs/Operations/Missions/O038/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsInterdictionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o038.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Reloading before convoy release preserves its resolved route; no seed reroll through pause or map reload. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O039 — Crossing Relief Convoy

**Intent and decision:** Deliver emergency equipment while both crossing approaches are contested. Escort tightly or clear the far side with an advance force before releasing trucks.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o039` / `scenario.operations.o039` |
| Family / force / enemy | `ESCORT` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1140`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(near_approach,45) -> ESCORT(relief_trucks,2,route.main OR route.safe) -> HOLD(far_approach,60). Three friendly cargo trucks; PROTECT(aid_depot_site,alive).

**Partial / failure:** Partial when One truck delivered and depot alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d04.readiness and success.o039; delivered cargo supports the finale unlock. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O039.asset`, `OperationsObjectives_O039.asset`, and `ScenarioSetup_O039.asset` under `Assets/Game/Configs/Operations/Missions/O039/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o039.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Returning a delivered truck to the exit cannot increment cargo or readiness a second time. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O040 — River Lifeline

**Intent and decision:** Reopen safe passage, restore one service station, and evacuate a trapped group. Choose whether infantry secures the safe crossing before the engineer and transport advance.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o040` / `scenario.operations.o040` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O033/O036/O039 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1141`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(west_exit,60) -> (REPAIR(evac_service) AND RESCUE(trapped_residents,8,exit.ground)) -> HOLD(east_exit,90) -> EXTRACT(squad,exit.ground). Twelve residents; PROTECT(aid_depot_site,alive).

**Partial / failure:** Partial when Four residents delivered or evac_service restored; depot alive and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve residents delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d04.stabilized and success.o040. Requires successful O033/O036/O039. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O040.asset`, `OperationsObjectives_O040.asset`, and `ScenarioSetup_O040.asset` under `Assets/Game/Configs/Operations/Missions/O040/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsRescueObjectiveSystem`, `OperationsCivilianEscortOrderSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o040.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A civilian saved through the ground route and later boarded for presentation remains one delivered identity, never two. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
