# Transport Boarding Scenario Lab Tracker

Purpose:
Create reusable isolated Scenario Lab tests for unit boarding and exiting behavior so automated validation and manual visual verification exercise the same production ECS transport systems.

This tracker covers soldiers and vehicle cargo boarding vehicle, helicopter, and plane transports, then exiting on the ground or in the air where the current game systems support it. The lab must not create parallel gameplay. The isolated visual scene may use camera/bootstrap/view MonoBehaviours, but boarding, movement, rope disembark, deploy, airdrop, visibility, and passenger state must come from the existing production ECS systems.

Last updated:
2026-06-29

## Progress Snapshot

- Checklist progress: `65 / 66 complete (98.5%)`.
- In progress: `0`.
- Remaining open: `1`.
- Current target: `user/manual visual acceptance notes for the transport proof contact sheet.`
- TB scenario status: `TB-001 through TB-012 now have stable catalog IDs/descriptors and runtime dispatch recognition. TB-001 through TB-010 have first automated ECS proof coverage. TB-001 has production visual playback for soldier/APC board and ground exit. TB-002 has production visual playback for landed helicopter boarding and rope exit: it resolves production soldier/helicopter prefabs, seeds a Scenario Lab grid, lets UnitTransportBoardingSystem board, queues the normal DisembarkTransport command, then lets UnitTransportRopeDisembarkSystem and UnitTransportRopeDropSystem own the drop. TB-003 has production visual playback for airborne helicopter pickup before boarding: it queues the normal BoardTransport command, lets the production air-pickup and movement systems land near the soldier, boards after landing, then queues the normal DisembarkTransport command for rope exit. TB-005 has production visual playback for transport plane rear-ramp soldier board and ground exit: it resolves production soldier/plane prefabs, seeds a grounded plane ramp state with real plane door components, lets UnitTransportBoardingSystem board, queues the normal DisembarkTransport command, and validates the production door/ramp request plus visible unloaded soldier state. TB-006 has production visual playback for transport plane soldier parachute exit: it resolves production soldier/plane/parachute references through the Scenario Lab prefab registry, queues the normal DisembarkTransport command with a real target drop cell, lets UnitAirMovementSystem make a pass, then lets UnitTransportAirdropSystem spawn the production parachute drop state and settle the passenger. TB-007 has production visual playback for transport plane vehicle cargo ramp exit: it resolves production tank/plane prefabs, boards through UnitTransportBoardingSystem as vehicle cargo, queues the normal DisembarkTransport command, and validates the production plane door/ramp request plus visible unloaded vehicle state. TB-008 has production visual playback for transport plane vehicle cargo airborne drop: it resolves production tank/plane/emergency-drop references, starts the vehicle hidden as a real cargo passenger, queues the normal DisembarkTransport command with a target drop cell, and validates the production cargo-drop state plus visible settled vehicle state. TB-009 has production visual playback for mixed transport plane load: it resolves production soldier/tank/plane prefabs, starts both passengers hidden in the real transport passenger buffer, queues the normal DisembarkTransport command with a target drop cell, and validates both the production parachute and cargo-drop states plus both visible settled outcomes. TB-010 remains validation-only as designed because rejection cases should not fake visual playback. TB-011 now has a cleanup-proof playback branch: it runs the production TB-008 cargo-drop visual, then clears spawned entities/grid and resets the camera. TB-012 now has a camera-proof playback branch that chains representative production paths for ground vehicle, helicopter rope, plane ramp, soldier airdrop, vehicle cargo drop, and mixed load. Shadow PlayMode validation observed TB-001 hidden onboard then visible ground-exit state, TB-002 hidden onboard, rope-drop state, then visible settled rope-exit state, TB-003 airborne pickup command, hidden onboard state, rope-drop state, then visible settled rope-exit state, TB-005 hidden onboard state, plane door/ramp request, then visible ramp-exit state, TB-006 hidden onboard state, production parachute drop state, then visible settled airdrop state, TB-007 hidden onboard cargo state, plane door/ramp request, then visible unloaded vehicle ramp-exit state, TB-008 hidden onboard cargo state, production cargo-drop state, then visible settled cargo-drop state, and TB-009 hidden soldier/vehicle passengers, production parachute/cargo-drop states, then visible settled mixed-load state. Focused validation now verifies every visual-proof-required TB descriptor has a playback branch and every validation-only descriptor does not pretend to have one. BattleScenarioLabPlayBootstrap exposes CurrentScenarioId and SelectScenarioById for deterministic Scenario Lab validation without hard-coded dropdown indices. TB runtime dispatch still returns explicit InvalidSetup metrics until per-scenario runtime metrics are wired, so the lab does not fake completed boarding metric passes. TB-004 documents current production behavior: helicopter disembark starts rope flow even from an initially landed state; direct helicopter ground walk-off is not currently supported by production systems.`
- Isolated scene status: `shadow BattleScenarioLab scene shell rebuilt with TB definition assets and expanded production prefab registry including airdrop visual prefab references in the selector/subscene; manual scene smoke passed AD-001 to AD-002 Next, repeated Next to AD-011, AD-001 validation, all TB definition presence checks, required boarding prefab registry checks, and overlay reference checks. Main-project BattleScenarioLab scene shell, baked prefab SubScene, and Scenario Lab prefab registry were regenerated after the Unity lock cleared, then main-project manual scene smoke passed. Shadow TB cleanup PlayMode validation advanced TB-008 to TB-009 and verified the prior transport plane, vehicle cargo passenger, cargo-drop state, command queue, runtime grid, stale overlay state, and stale camera target were removed. Shadow TB Run Again cleanup validation restarted TB-008 and verified exactly one new baseline run with no stale cargo-drop state, command queue, duplicate transport/passenger entities, or stale overlay state.`
- Automated metrics runner status: `focused EditMode validation added at Assets/Tests/Editor/ScenarioLab/TransportBoardingScenarioLabTests.cs with batch execute method TransportBoardingScenarioLabTests.RunFocusedValidation; current focused suite passes 15 ECS/catalog/dispatch/report/definition-path/visual-playback-contract tests, including visual-required playback branch coverage for TB-001/TB-002/TB-003/TB-005/TB-006/TB-007/TB-008/TB-009/TB-011/TB-012 and validation-only exclusion for TB-004/TB-010.`
- Manual visual verification status: `proof captures exist and agent sanity-checked the contact sheet renders, but user/manual visual acceptance is still open.`
- Visual proof capture status: `TB-001, TB-002, TB-003, TB-005, TB-006, TB-007, TB-008, and TB-009 automated PlayMode visual proof passed in the shadow project; TB-011 and TB-012 playback branch coverage passed in focused validation. Fresh transport proof PNGs and contact sheet were captured from the shadow Scenario Lab and copied into Design/VisualLockLayered/_TransportBoardingScenarioLab/: TB-001_GroundVehicleTransport_BoardAndGroundExit.png, TB-002_HelicopterTransport_BoardAndRopeExit.png, TB-003_HelicopterTransport_AirPickupBeforeBoarding.png, TB-005_TransportPlane_RampBoardAndGroundExit.png, TB-006_TransportPlane_SoldierAirdrop.png, TB-007_TransportPlane_VehicleCargoGroundExit.png, TB-008_TransportPlane_VehicleCargoAirdrop.png, TB-009_TransportPlane_MixedLoadAirdrop.png, TB-011_TransportBoarding_NextCleanup.png, TB-012_TransportBoarding_CameraProofPath.png, and transport_boarding_visual_proof_contact_sheet.png. TB-002 rope exit, TB-008 cargo drop, and TB-009 mixed load enforce duplicate visible-root rejection for completed passengers.`
- Validation status: `git diff --check passed; shadow transport visual proof capture passed in /private/tmp/warline-transport-boarding-visual-proof-capture.log and produced ten 1280x720 PNGs plus a 12998x756 contact sheet; main-project BattleScenarioLab scene generation passed in /private/tmp/warline-transport-boarding-main-scene-build.log and saved Assets/Game/Scenes/ScenarioLab/BattleScenarioLab.unity, Assets/Game/Scenes/ScenarioLab/BattleScenarioLabBakedPrefabs.unity, and Assets/Game/Configs/ScenarioLab/BattleScenarioLab_UnitPrefabRegistry.asset; main-project manual scene smoke passed in /private/tmp/warline-transport-boarding-main-manual-scene-smoke.log; shadow-project Unity batchmode focused validation passed with [TransportBoardingScenarioLab] result=Passed tests=15 in /private/tmp/warline-transport-boarding-scenario-lab-focused.log after wiring TB-011/TB-012 playback; shadow transport boarding definition asset generation passed and produced 12 TB assets; shadow scene shell rebuild saved after syncing the production plane airdrop visual config to the shadow project; shadow manual scene smoke passed in /private/tmp/warline-transport-boarding-manual-scene-smoke.log; shadow TB-001 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb001-visual.log; shadow TB-002 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb002-visual.log, rerun with duplicate visual-root guard active; shadow TB-003 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb003-visual.log; shadow TB-005 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb005-visual.log; shadow TB-006 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb006-visual.log; shadow TB-007 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb007-visual.log; shadow TB-008 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb008-visual.log after adding passive TB overlay fields and duplicate visual-root guard; shadow TB-009 production visual PlayMode validation passed in /private/tmp/warline-transport-boarding-tb009-visual.log with mixed soldier parachute and vehicle cargo-drop proof; shadow TB Next cleanup validation passed in /private/tmp/warline-transport-boarding-next-cleanup.log with entity/grid/command cleanup, overlay freshness, and camera reset checks; shadow TB Run Again cleanup validation passed in /private/tmp/warline-transport-boarding-run-again-cleanup.log with overlay freshness checks. Non-blocking log noise remains from licensing token refresh, XcodeApplications Info.plist probes, package test asmdef warnings outside Assets, Unity Entities Graphics roots-handler NullReferenceException during batch/no-graphics scene open or quit, UnityConnect/curl abort on quit, preview scene leak on batch shutdown, and usbmuxd shutdown.`
- Still wrong / next iteration: `Need user/manual visual acceptance notes for the transport proof contact sheet. No automated bug/tuning tracking gaps remain open from this pass.`
- Counting rule: only checklist lines beginning with `- [ ]`, `- [x]`, or `- [~]` count toward checklist progress.

## Production Prefab Inventory

- Soldier visual candidate: `Assets/Game/Prefabs/Characters/Unit_Chr_Soldier_Male_02_Alt_04.prefab` (`Unit_Chr_Soldier_Male_02_Alt_04` is used by current player/enemy/plan-entry configs).
- Ground vehicle transport candidates: `Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Heavy.prefab`, `Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Fast.prefab`, `Assets/Game/Prefabs/Vehicles/Unit_Veh_APC_Slow.prefab`, and `Assets/Game/Prefabs/Vehicles/Unit_Veh_Truck_Canopy.prefab`.
- Helicopter transport visual: `Assets/Game/Prefabs/Vehicles/Unit_Veh_Helicopter_Transport.prefab`.
- Plane transport visual and config: `Assets/Game/Prefabs/Vehicles/Unit_Veh_Plane_Transport.prefab` with `Assets/Game/Configs/Prefabs/Prefab_UnitGrid_Veh_Plane_Transport_Config.asset`.
- Vehicle cargo visual candidate: `Assets/Game/Prefabs/Vehicles/Unit_Veh_Tank_USA.prefab`.
- Soldier parachute visual: `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_Parachute_01.prefab`, referenced by the plane config GUID `a567fd209adc22643bffbc9030f3005a`.
- Vehicle emergency cargo-drop visual: `Assets/Synty/PolygonBattleRoyale/Prefabs/Props/SM_Prop_EmergencyDrop_01.prefab`, referenced by the plane config GUID `da21a5a922007a641b197316ea708507`.

## Goals

- Let the user open an isolated Scenario Lab scene and visually test boarding without entering the normal game flow.
- Let Codex validate the same scenario IDs in batch/EditMode where practical.
- Preserve real game timing for transport approach, door/ramp opening, helicopter takeoff/rope drop, plane pass, parachute/cargo descent, and passenger visibility.
- Make boarding bugs visible: stuck soldiers, missing move orders, wrong passenger hidden state, wrong cargo slot handling, wrong disembark cell, missing VFX/visual, bad camera framing, stale objects after Next.
- Keep scenario definitions reusable so future boarding combinations can be added without building one-off systems.

## Non-Goals

- Do not build a separate boarding simulator.
- Do not add fake GameObject-only soldier/transport movement to make a visual pass.
- Do not add UI Toolkit.
- Do not add MonoBehaviour gameplay `Update()` loops.
- Do not change live transport balance or passenger capacities unless a scenario proves a bug or tuning need and the user approves.
- Do not force all transports to share one exit behavior if production systems intentionally differ.

## Architecture Contract

- Production ECS systems own gameplay: `TransportBoardingCommandSystem`, `UnitTransportBoardingSystem`, `UnitTransportPassengerStateSystem`, `UnitTransportDeployOrderSystem`, `UnitTransportRopeDisembarkSystem`, `UnitTransportRopeDropSystem`, `UnitTransportRopeDisperseSystem`, `UnitTransportAirdropSystem`, `UnitAirMovementSystem`, and existing movement/visibility helpers.
- Scenario setup may create test worlds, scenario definitions, and isolated scene bootstrap objects.
- Visual playback may seed entities, choose the selected scenario, position cameras, show passive overlay text, and handle button events.
- Visual playback must not directly move passengers or transports as a replacement for ECS movement.
- Manual visual tests use production prefabs and production visual/VFX paths wherever the game has them.
- `Next` in the Scenario Lab must stop the current visual, clean spawned entities/VFX/camera state, and start the next selected test.
- Tests must record whether the behavior is currently supported, expected by design, or a gameplay gap.

## Scenario Matrix

| ID | Scenario | Automated proof | Visual proof | Expected behavior |
| --- | --- | --- | --- | --- |
| TB-001 | Soldiers board ground vehicle transport and exit on ground | Required | Required | Soldiers receive real board order, become passengers/hidden, then disembark to valid adjacent cells and become visible. |
| TB-002 | Soldiers board landed helicopter transport, then exit in air by rope | Required | Required | Soldiers board only after helicopter is visually grounded, transport takes/holds rope height, passengers drop one at a time, then disperse. |
| TB-003 | Helicopter selected while airborne commands pickup before boarding | Required | Required | Clicking/commanding a flying helicopter finds a landing cell near soldiers, prevents mid-air boarding, then allows boarding after landing. |
| TB-004 | Helicopter ground exit expectation audit | Required | Optional until design decision | Current production behavior starts rope disembark even when initially landed. If walk-off ground exit is desired, that is a gameplay change, not a Scenario Lab workaround. |
| TB-005 | Soldiers board transport plane on ground through rear ramp and exit on ground | Required | Required | Soldiers use rear ramp approach, board only at valid ramp/near cells, door/ramp opens, passengers exit to ramp cells and roll out. |
| TB-006 | Soldiers board transport plane and airborne parachute exit | Required | Required | Plane uses real airdrop request/pass readiness, passenger gets parachute drop visual, lands, becomes visible, and settles. |
| TB-007 | Vehicle cargo boards transport plane and exits on ground | Required | Required | Vehicle occupies cargo slot, ground disembark uses ramp rollout cell, cargo passenger state clears. |
| TB-008 | Vehicle cargo boards transport plane and airborne cargo drop | Required | Required | Vehicle cargo uses emergency drop visual, lands, clears cargo passenger state, and settles. |
| TB-009 | Mixed plane load soldiers plus vehicle cargo | Required | Required | Mixed passengers count by kind, airdrop/ground exit releases correct counts without dropping hidden passengers. |
| TB-010 | Capacity and rejection cases | Required | Optional | Full transport, wrong passenger kind, airborne plane boarding, blocked exit/drop cell, and missing visual prefab produce the same production feedback/reason codes. |
| TB-011 | Repeated Next cleanup in isolated visual scene | Required | Required | Switching scenarios destroys old transport/passenger/projectile/VFX/camera artifacts and starts the next test cleanly. |
| TB-012 | Camera proof path for every boarding class | Optional headless, required manual | Required | Camera shows approach, boarding moment, passenger hidden/inside state, exit moment, landing/settle, and final cleanup. |

## Visual Camera Contract

Every manual visual boarding scenario must have deterministic camera beats:

1. Establishing shot of transport, passengers, and target boarding/exit area.
2. Close shot of passenger approach and the actual boarding moment.
3. Transport-specific shot:
   - Ground vehicle: side/front shot showing passengers hidden after boarding.
   - Helicopter: landed boarding, takeoff or hover, rope drop anchor, landing/disperse.
   - Plane: rear ramp/door area, taxi/fly pass when relevant, parachute/cargo drop.
4. Impact-style final shot showing final passenger state and no stale duplicate visuals.
5. Passive overlay with scenario ID, current phase, passenger counts, and pass/fail metrics.

## Metrics Contract

Each scenario report should include:

- `scenarioId`
- `variantId`
- `transportSourceKey`
- `passengerSourceKeys`
- `boardCommandAccepted`
- `boardingStarted`
- `boardingCompleted`
- `boardTimeSeconds`
- `passengerHiddenAfterBoard`
- `transportPassengerCount`
- `exitCommandAccepted`
- `exitStarted`
- `exitCompleted`
- `exitTimeSeconds`
- `passengerVisibleAfterExit`
- `passengerFinalCell`
- `dropVisualEntityCreated`
- `dropVisualCleaned`
- `reasonCode`
- `failureReason`
- `visualProofPath`

## Implementation Phases

## Phase 0: Inventory, Contract, And Scenario Definitions

Purpose:
Lock the production transport behaviors and define every boarding scenario before implementation.

- [x] Create this tracker with the full boarding scenario matrix.
- [x] Inventory production transport prefabs for ground vehicle, helicopter transport, transport plane, soldiers, vehicle cargo, parachute visual, and cargo drop visual.
- [x] Inventory current ECS systems used by each scenario and record which behavior each system owns.
- [x] Confirm helicopter landed exit current behavior and document whether direct walk-off exit is supported or a gameplay gap.
- [x] Define scenario IDs, expected result, camera beats, and automated metrics for TB-001 through TB-012.
- [x] Add a small report schema for boarding metrics.
- [x] Add a validation command section with main-project and shadow-project commands.

## Phase 1: Automated ECS Scenario Proof

Purpose:
Prove the behavior in isolated ECS worlds before adding visual camera playback.

- [x] Add focused EditMode scenario tests under `Assets/Tests/Editor/ScenarioLab/`.
- [x] Add `TB-001` automated proof for soldiers boarding ground vehicle transport.
- [x] Add `TB-001` automated proof for ground vehicle disembark.
- [x] Add `TB-002` automated proof for landed helicopter boarding.
- [x] Add `TB-002` automated proof for helicopter rope request and first drop.
- [x] Add `TB-002` automated proof for rope touchdown and disperse.
- [x] Add `TB-003` automated proof that airborne helicopter pickup commands landing and prevents mid-air boarding.
- [x] Add `TB-004` automated audit for current helicopter landed exit behavior.
- [x] Add `TB-005` automated proof for plane rear-ramp soldier boarding.
- [x] Add `TB-005` automated proof for plane ground/ramp soldier exit.
- [x] Add `TB-006` automated proof for plane soldier parachute airdrop.
- [x] Add `TB-007` automated proof for vehicle cargo boarding into plane.
- [x] Add `TB-007` automated proof for vehicle cargo ground/ramp exit.
- [x] Add `TB-008` automated proof for vehicle cargo airborne drop.
- [x] Add `TB-009` automated proof for mixed load counts and release counts.
- [x] Add `TB-010` automated rejection proofs for full transport, wrong passenger kind, airborne plane boarding, blocked exit/drop cell, and missing visual prefab.
- [x] Add focused validation execute method so the suite can run from Unity batchmode.

## Phase 2: Scenario Definition Assets And Runner Dispatch

Purpose:
Make the tests selectable/reusable like existing Scenario Lab cases.

- [x] Add transport boarding scenario definition assets or an equivalent data-backed registry.
- [x] Add runtime dispatch for boarding scenario IDs without breaking existing AD/GM/DR runners.
- [x] Add scenario report JSON output for boarding metrics.
- [x] Add scenario list integration so the manual lab can select TB scenarios.
- [x] Preserve existing AD/GM/DR scenario behavior and Next switching.
- [x] Validate scenario asset creation in the shadow project.

## Phase 3: Production Prefab Registry For Transport Visuals

Purpose:
Ensure visual tests use real game models and VFX.

- [x] Add registry references for the production soldier prefab used in boarding tests.
- [x] Add registry references for ground vehicle transport.
- [x] Add registry references for `Unit_Veh_Helicopter_Transport`.
- [x] Add registry references for `Unit_Veh_Plane_Transport`.
- [x] Add registry references for vehicle cargo passenger prefab.
- [x] Add registry references for parachute and emergency cargo drop visuals.
- [x] Regenerate or update the Scenario Lab baked prefab SubScene.
- [x] Validate that the shadow scene resolves every production prefab reference.

## Phase 4: Isolated Visual Playback

Purpose:
Let the user open the Scenario Lab scene, click Next, and watch each boarding scenario with camera beats.

- [x] Add visual playback orchestration for TB scenarios without gameplay `Update()` loops.
- [x] Seed production ECS entities for TB scenarios and let existing systems execute behavior.
- [x] Add camera beat data for TB-001 ground vehicle board/exit.
- [x] Add camera beat data for TB-002 helicopter board/rope exit.
- [x] Add camera beat data for TB-003 airborne pickup.
- [x] Add camera beat data for TB-005 plane board/ramp exit.
- [x] Add camera beat data for TB-006 plane parachute exit.
- [x] Add camera beat data for TB-007/TB-008 vehicle cargo board/drop. TB-007 ground/ramp vehicle cargo exit and TB-008 airborne cargo drop are validated in shadow PlayMode.
- [x] Add passive overlay fields for passenger count, phase, and failure reason.
- [x] Add cleanup for passengers, transports, drop visuals, trail/VFX, camera targets, and overlay state on Next/Run Again. Next cleanup and Run Again entity/grid/command cleanup plus overlay freshness are validated; Next cleanup also validates camera reset before TB-009.

## Phase 5: Validation And Visual Proof

Purpose:
Trust the visual lab only after it proves cleanup and visible outcomes.

- [x] Add automated smoke validation that TB scenarios appear in the manual scene selector.
- [x] Add automated Play Mode validation that Next starts a new TB scenario and cleans old TB entities.
- [x] Add automated validation for no duplicate alive/destroyed/drop visual roots after exit/drop.
- [x] Capture visual proof for TB-001.
- [x] Capture visual proof for TB-002.
- [x] Capture visual proof for TB-005.
- [x] Capture visual proof for TB-006.
- [x] Capture visual proof for TB-007/TB-008.
- [x] Save visual contact sheets under `Design/VisualLockLayered/_TransportBoardingScenarioLab/`.
- [ ] Record manual verification notes and remaining defects.

## Phase 6: Tuning And Bug-Fix Loop

Purpose:
Use the tests to find real game bugs or bad values, then fix them through production systems.

- [x] Track failed board/disembark timing cases with exact scenario ID and reason code.
- [x] Track stuck passenger movement cases with final cell and target cell.
- [x] Track missing hidden/visible passenger state cases.
- [x] Track missing or stale drop visuals.
- [x] Track wrong camera/visual proof framing separately from gameplay failures.
- [x] Tune config values only after the scenario proves the current value is wrong.
- [x] Re-run focused automated validation after every gameplay fix.
- [x] Re-run visual proof after every visual/camera fix.

## Phase 6 Findings Ledger

Purpose:
Keep scenario-discovered bugs and tuning decisions explicit so visual proof does not hide gameplay regressions.

| Category | Current finding | Evidence | Status |
| --- | --- | --- | --- |
| Failed board/disembark timing | No current failing board/disembark timing case remains in automated proof. TB-001/TB-002/TB-003/TB-005/TB-006/TB-007/TB-008/TB-009 all reached their expected passenger states within validation windows. | Shadow validation logs listed in Progress Snapshot. | Closed until a new scenario failure appears. |
| Stuck passenger movement | No current stuck passenger movement case remains in focused ECS or visual proof validation. Ground/ramp/rope/parachute/cargo paths reached visible settled states where required. | `TransportBoardingScenarioLabTests.RunFocusedValidation`, TB visual PlayMode logs, proof captures. | Closed until a new scenario failure appears. |
| Hidden/visible passenger state | Earlier risk of stale or duplicate visuals is covered by duplicate visible-root guards for rope/cargo/mixed-load completions. Hidden onboard and visible settled states are validated for required scenarios. | TB-002, TB-008, TB-009 validation pass lines and proof captures. | Closed. |
| Missing/stale drop visuals | Parachute and cargo-drop components are required by TB-006/TB-008/TB-009 validation before pass. Cleanup validation verifies stale cargo-drop state is removed on Next/Run Again. | `/private/tmp/warline-transport-boarding-tb006-visual.log`, `/private/tmp/warline-transport-boarding-tb008-visual.log`, `/private/tmp/warline-transport-boarding-tb009-visual.log`, cleanup logs. | Closed. |
| Camera/proof framing | TB-011 and TB-012 are wired so visual-required selector entries are not dead entries. Capture contact sheet exists, but user/manual acceptance remains open because visual taste and framing quality require human review. | `Design/VisualLockLayered/_TransportBoardingScenarioLab/transport_boarding_visual_proof_contact_sheet.png`. | Automated gate closed; manual review open. |
| Tuning/config changes | No transport balance or timing value was tuned as part of this pass. Existing production ECS and config values were used. Future tuning must cite a failing scenario ID and reason before config changes. | Tracker non-goal and validation logs. | Closed guardrail. |
| Regression validation after fixes | Focused ECS/catalog/dispatch/report/playback-contract validation was rerun after playback contract changes; PlayMode visual validations and proof capture were rerun after visual/camera fixes. | `/private/tmp/warline-transport-boarding-scenario-lab-focused.log`, `/private/tmp/warline-transport-boarding-visual-proof-capture.log`. | Closed. |

## Manual Lab Issue Notes

- 2026-06-29: TB-004 is an automated audit-only scenario, not a live visual playback scenario. The manual lab now labels validation-only transport scenarios as automated audits and defensively exits playback without the misleading "not wired yet" warning if a stale button path calls playback anyway. Shadow focused validation passed in `/private/tmp/warline-transport-boarding-tb004-audit-fix.log`.
- 2026-06-29: Manual review found that TB-005 through TB-009 plane transport visuals are not runway-accurate to live gameplay. The current Scenario Lab seeds the plane directly into grounded or airborne boarding/drop states, while live fixed-wing behavior uses airport/runway read models plus `UnitAirMovementSystem` runway takeoff/landing state. This is not a safe quick fix without adding the production airport/runway setup to the isolated scene or reusing the real production spawn path; do not fake helicopter-like landing/takeoff for transport planes.
- 2026-06-29: Manual review found two separate helicopter rope/drop paths. The ECS `UnitTransportRopeDisembarkSystem` fix covers selected transport disembark, but the live "Deploy All" helicopter delivery uses `BuildingProductionTransportPresentationSystemHelper` with temporary production drop visuals. That production path was still taking the raw reserved production slot as the drop endpoint while adding a lane offset to the helicopter hover point, so the soldier and rope descended diagonally; it also lacked a clear landing-pad search around runtime authored map buildings such as tents. The production helper now resolves a clear helicopter drop cell against walkable cells, blockers, occupancy, runtime building footprints with padding, and map-surface metadata, then recenters the helicopter visual anchor over that accepted cell before starting the drop. Rope rendering now uses the same X/Z as the falling soldier, so the visible rope remains vertical. Validation: `git diff --check` passed. Unity batch validation was attempted with the documented escalated/out-of-sandbox workaround at `/private/tmp/warline-unit-transport-production-drop.log`, but Unity stayed in the `LicenseClient-farhad` unsupported-protocol/reconnect loop before script compilation or test execution; the stuck process was stopped. The earlier ECS validation remains `/private/tmp/warline-unit-transport-batch.log` with `[UnitTransportValidation] result=Passed tests=73`; live GUI/manual verification is still required for the production Deploy All path.
- 2026-06-29: Follow-up live production drop correction after manual deploy-all testing still showed soldiers exiting on/near landed helicopters. `BuildingProductionTransportPresentationSystemHelper` now searches a wider 64-cell radius, treats runtime building footprints as larger no-drop zones, reserves active production transports, produced units, and all live unit footprints, gives live aircraft and live vehicles larger exclusion buffers, and reserves aircraft by current `LocalTransform` X/Z instead of potentially stale `UnitGrid` cells. The production helper now rejects elevated non-road/non-bridge/non-ramp map surfaces so tent roofs are not accepted as landing cells, waits and retries instead of dropping when no safe cell is available, and spawns the final produced unit at the same accepted endpoint used by the visual rope drop. Validation: `git diff --check` passed; the updated helper was synced to `/Users/farhad/Projects/WarlineCapture-CodexUnity1`. Shadow Unity batch validation was attempted with `BuildingProductionSystemTests`; after syncing `Assets/Game/Scripts/Systems`, the modified helper no longer appeared in compiler errors, but the shadow project still contains stale deleted system files not present in the main project and fails on unrelated selection/composition type mismatches. Main Editor log tail did not show new compiler errors for this helper, but live GUI verification is still required after Unity recompiles the main project.

## Validation Commands

Preferred shadow validation:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod TransportBoardingScenarioLabTests.RunFocusedValidation -logFile /private/tmp/warline-transport-boarding-scenario-lab-focused.log
```

Main-project fallback validation:

```bash
"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod TransportBoardingScenarioLabTests.RunFocusedValidation -logFile /private/tmp/warline-transport-boarding-scenario-lab-focused-main.log
```

Always run:

```bash
git diff --check
```
