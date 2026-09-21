# D05 — Highland Approach: ten mission implementation briefs

**All ten entries are Planned, including ARIA certification.** Numeric timings/resources are initial test specifications. They inherit the force/enemy packages, rule semantics, global loss/terminal rules, harm penalties, and checkpoint protocol in [MISSION_IMPLEMENTATION](../MISSION_IMPLEMENTATION.md). Every Victory applies its family metric vector from [STRATEGIC_RULES](../STRATEGIC_RULES.md) plus the listed facts. Partial uses half that vector and no Success milestone; Defeat/Withdrawn/TechnicalFailure use their shared distinct rules.

Planned map: `opmap.operations.highland_approach`. Reuse an existing published map ID instead only if the physical layout is identical; update the catalog and validate before accepting content. No map/mission asset is claimed to exist from these briefs.

The graph supplies the mandatory win predicate. `AND` means both required parallel branches; `OR` in a route argument is a player choice locked at its branch waypoint. Evidence named by INTERACT is carried and required at EXTRACT unless the brief explicitly says otherwise. Global PROTECT conditions apply from launch; their loss is Defeat. Partial predicates apply only when no mandatory protection failure has occurred. All extraction predicates require the named live destination.

## O041 — Ridge Signals

**Intent and decision:** Survey three observation points overlooking the supply road. Take the exposed short ridge path or approach through covered switchbacks; heights use real line-of-sight checks.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o041` / `scenario.operations.o041` |
| Family / force / enemy | `RECON` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 840 s after player control starts |
| Canonical fixture | Seed `1142`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(overlook_a,overlook_b,relay_approach) -> INTERACT(ridge_log,15) -> EXTRACT(squad,exit.ground). Ground paths to every point are certified.

**Partial / failure:** Partial when Two points scanned and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No recon unit lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered log sets evidence.d05.ridge; Victory sets success.o041. Apply the `RECON` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O041.asset`, `OperationsObjectives_O041.asset`, and `ScenarioSetup_O041.asset` under `Assets/Game/Configs/Operations/Missions/O041/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o041.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A scan from below terrain cannot reveal a target above an occluding ridge just because it lies inside radius. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O042 — Switchback Patrol

**Intent and decision:** Check three road bends and a remote aid post. Approach bends directly or traverse the longer protected foot loop, maintaining a safe withdrawal route.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o042` / `scenario.operations.o042` |
| Family / force / enemy | `PATROL` / `FP_LIGHT` / `EP_CELL` |
| Availability | Start: no mission prerequisite |
| Pacing / deadline | Target 8–15 min; hard deadline 900 s after player control starts |
| Canonical fixture | Seed `1143`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B12; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** VISIT(lower_bend,middle_bend,upper_bend,aid_post) -> CLEAR(road_patrol) -> HOLD(aid_post,45). PROTECT(aid_post_site,alive).

**Partial / failure:** Partial when Three bends visited and road_patrol cleared, aid post alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** No squad loss. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d05.foot_supply and sets success.o042. Apply the `PATROL` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O042.asset`, `OperationsObjectives_O042.asset`, and `ScenarioSetup_O042.asset` under `Assets/Game/Configs/Operations/Missions/O042/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRouteObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o042.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Terrain height and visual foot contact agree at each waypoint; no objective trigger floating above the road. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O043 — Remote Service Link

**Intent and decision:** Restore a hill relay and roadside service station. Separate the specialists to minimize repair time or keep a convoy together through the switchback bottleneck.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o043` / `scenario.operations.o043` |
| Family / force / enemy | `REPAIR` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1144`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(service_guards) -> (REPAIR(hill_relay) AND REPAIR(road_station)) -> HOLD(upper_junction,60). Both sites must survive; 80 Materials.

**Partial / failure:** Partial when One site restored and alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both repair specialists survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d05.service and success.o043; route.d05.service reflects restored state. Apply the `REPAIR` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O043.asset`, `OperationsObjectives_O043.asset`, and `ScenarioSetup_O043.asset` under `Assets/Game/Configs/Operations/Missions/O043/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsRepairObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o043.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Repair range uses valid approach geometry, not straight-line distance through a cliff or retaining wall. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O044 — Pass Supply Run

**Intent and decision:** Move three supply trucks to the remote aid post. The short switchback favors ambushes; the outer track takes longer but lets the escort spread out.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o044` / `scenario.operations.o044` |
| Family / force / enemy | `ESCORT` / `FP_SERVICE` / `EP_RAIDERS` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1145`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(pass_junction) -> ESCORT(supply_trucks,2,route.main OR route.safe) -> HOLD(aid_post,45). Three friendly cargo trucks; PROTECT(aid_post_site,alive).

**Partial / failure:** Partial when One truck delivered and aid post alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three trucks delivered. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory opens route.d05.vehicle_supply and sets success.o044. Apply the `ESCORT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O044.asset`, `OperationsObjectives_O044.asset`, and `ScenarioSetup_O044.asset` under `Assets/Game/Configs/Operations/Missions/O044/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o044.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A destroyed truck releases its vehicle reservations so following trucks can use a certified passing or alternate lane. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O045 — Outpost Evacuation

**Intent and decision:** Evacuate isolated personnel from a small hill landing zone. Clear its ground threat first, then shuttle passengers while keeping a reserve on the access road.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o045` / `scenario.operations.o045` |
| Family / force / enemy | `AIRLIFT` / `FP_AIR` / `EP_AIRFIELD` |
| Availability | Any attempt of local slot 1 or 2 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1146`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B30; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** CLEAR(lz_threats) -> HOLD(hill_lz,30) -> AIRLIFT(outpost_personnel,8,hill_lz,exit.air). Twelve passengers; PROTECT(aid_post_site,alive).

**Partial / failure:** Partial when Four personnel delivered and aid post alive. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All twelve delivered with both helicopters alive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d05.outpost_safe and success.o045. Apply the `AIRLIFT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O045.asset`, `OperationsObjectives_O045.asset`, and `ScenarioSetup_O045.asset` under `Assets/Game/Configs/Operations/Missions/O045/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o045.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A helicopter without enough usable Fuel for the planned leg exposes the shortage; ARIA must refuel through supported actions or select the other legal transport. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O046 — Relay Compound

**Intent and decision:** Dislodge the group controlling a relay compound without destroying the service transmitter. Infantry can clear the side lane while armor advances on the gate.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o046` / `scenario.operations.o046` |
| Family / force / enemy | `BREACH` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5; Intel confidence >=40 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1147`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(compound) -> BREACH(compound_gate) -> CLEAR(compound_guards) -> INTERACT(relay_orders,20) -> EXTRACT(squad,exit.ground). PROTECT(service_transmitter,alive).

**Partial / failure:** Partial when Gate traversed and guards cleared, transmitter alive, two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Transmitter ends at 75% health or higher. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Delivered orders set evidence.d05.relay; Victory sets milestone.d05.threat and success.o046. Apply the `BREACH` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O046.asset`, `OperationsObjectives_O046.asset`, and `ScenarioSetup_O046.asset` under `Assets/Game/Configs/Operations/Missions/O046/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsBreachObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsInteractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsReconObjectiveSystem`, `OperationsTargetObjectiveSystem`, `OperationsEvidenceCarrySystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o046.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Combat target selection distinguishes the hostile gate from the protected transmitter; ARIA must not attack every building in the compound. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O047 — Pass Denial

**Intent and decision:** Stop hostile supply trucks before they descend from the pass. Choose a broad intercept area or cover both bends with smaller teams.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o047` / `scenario.operations.o047` |
| Family / force / enemy | `INTERDICT` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 960 s after player control starts |
| Canonical fixture | Seed `1148`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** SCAN(upper_staging) -> STOP(pass_trucks,2) -> HOLD(lower_junction,45). Three hostile cargo trucks, release 150 s, earliest exit 300 s; retain 30 s minimum warning.

**Partial / failure:** Partial when One hostile truck stopped. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All three stopped without losing a tank. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory blocks route.d05.hostile_pass and sets success.o047. Apply the `INTERDICT` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O047.asset`, `OperationsObjectives_O047.asset`, and `ScenarioSetup_O047.asset` under `Assets/Game/Configs/Operations/Missions/O047/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsInterdictionObjectiveSystem`, `OperationsReconObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o047.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Increased path length cannot silently allow trucks to spawn beyond the intercept zone or bypass their ordered waypoints. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O048 — Observation Posts

**Intent and decision:** Secure two observation posts with different access routes. An early split pressures both but leaves less reserve for the enemy counterattack.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o048` / `scenario.operations.o048` |
| Family / force / enemy | `SEIZE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1020 s after player control starts |
| Canonical fixture | Seed `1149`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (HOLD(post_west,75) AND HOLD(post_east,75)) -> CLEAR(counterattack). Both zones use ground-reachable plateau footprints.

**Partial / failure:** Partial when One post secured and half of counterattack combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Both APCs survive. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets report.d05.observation_secured and success.o048; it grants no permanent omniscient map reveal. Apply the `SEIZE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O048.asset`, `OperationsObjectives_O048.asset`, and `ScenarioSetup_O048.asset` under `Assets/Game/Configs/Operations/Missions/O048/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o048.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Captured posts reveal only their declared visibility output and do not expose every hidden enemy entity to ARIA. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O049 — Remote Aid Watch

**Intent and decision:** Protect the isolated aid post and its service station until the relief window. A forward choke defense risks flanking; a close defense sacrifices route control.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o049` / `scenario.operations.o049` |
| Family / force / enemy | `DEFENSE` / `FP_GROUND` / `EP_MECHANIZED` |
| Availability | Two Victories among local slots 1–5 |
| Pacing / deadline | Target 8–15 min; hard deadline 1080 s after player control starts |
| Canonical fixture | Seed `1150`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** HOLD(aid_post,240) AND CLEAR(assault_groups). PROTECT(aid_post_site,alive); PROTECT(road_station,alive). A arrives at 90 s via switchback, B at 180 s via outer track with 30/45 s warnings.

**Partial / failure:** Partial when Both sites alive and half the assault combat entities destroyed. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** Aid post ends at 75% health or higher. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d05.readiness and success.o049. Apply the `DEFENSE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O049.asset`, `OperationsObjectives_O049.asset`, and `ScenarioSetup_O049.asset` under `Assets/Game/Configs/Operations/Missions/O049/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsTargetObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o049.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** A wave blocked by friendly occupation stages at a valid boundary; it cannot materialize inside the protected post. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.

## O050 — Highland Connection

**Intent and decision:** Restore the backup relay, move a relief convoy through the pass and keep the outpost exit available. Use air transport to reposition infantry or maintain a ground reserve.

| Contract | Implementation value |
|---|---|
| Identity | `operation.o050` / `scenario.operations.o050` |
| Family / force / enemy | `FINALE` / `FP_COMBINED` / `EP_FINALE` |
| Availability | Victory O043/O046/O049 |
| Pacing / deadline | Target 15–20 min; hard deadline 1200 s after player control starts |
| Canonical fixture | Seed `1151`, Regular, immutable launch snapshot; evidence also needs perturbation seeds |
| Delivery / gates | B60; P0–P4, P6, P5; per-entry acceptance and ARIA win required |

**Mandatory graph and authored entities:** (REPAIR(backup_relay) AND ESCORT(relief_trucks,2,route.safe)) -> HOLD(outpost_exit,90) -> EXTRACT(squad,exit.ground). Three friendly trucks; PROTECT(aid_post_site,alive). Helicopters are optional mobility.

**Partial / failure:** Partial when Backup relay restored or two trucks delivered, aid post alive and two original infantry extracted. Otherwise deadline is Defeat. Loss of a mandatory protected site, impossible required survivor/cargo/evidence minimum, or all eligible player units is immediate Defeat under the shared terminal order. Conclude appears only when the Partial predicate is true; Withdraw remains a separate confirmation.

**Optional mastery:** All trucks delivered and no aircraft lost. Does not replace a required objective or unlock a prerequisite.

**Persistent result:** Victory sets milestone.d05.stabilized and success.o050. Requires successful O043/O046/O049. Apply the `FINALE` outcome vector and actual harm from the shared rules; Partial never sets this mission’s Success/finale milestone.

**Implementation:** Create `OperationsMission_O050.asset`, `OperationsObjectives_O050.asset`, and `ScenarioSetup_O050.asset` under `Assets/Game/Configs/Operations/Missions/O050/`. Bind every role and route above; partition the finite enemy package among initial/trigger groups. Configure `OperationsControlObjectiveSystem`, `OperationsEscortObjectiveSystem`, `OperationsExtractionObjectiveSystem`, `OperationsProtectionObjectiveSystem`, `OperationsRepairObjectiveSystem`. Reuse these shared systems; no mission-specific C# controller. Add keys under `operations.o050.{title,brief,objective.*,warning.*,result.*}` and public ARIA focus/action metadata for every active node.

**Mission-specific checks:** Victory remains possible using the ground force and certified route; optional aircraft cannot become an undocumented requirement. Also prove one normal Victory, the stated Partial where reachable, mandatory-loss Defeat, Withdraw, checkpoint restore, exactly-once settlement and clean return/redeploy. Use isolated fixtures for the negative cases.

**ARIA acceptance:** Record a complete unassisted Regular win through visible controls, correct result and district settlement, and return to Operations. Then pass the per-difficulty/seed/language matrix in [ACCEPTANCE](../ACCEPTANCE.md). No injected facts, direct state changes, hidden target reads, scripted mission solution, or combat-stat/resource advantage. A failed run remains evidence of a defect; a test cannot mark this entry Accepted until ARIA can actually win.
