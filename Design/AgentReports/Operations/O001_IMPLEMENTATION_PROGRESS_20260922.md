# O001 implementation progress — 2026-09-22

Status: **In development. Not player-ready.** No R1–R6 exit gate is claimed complete.

## Tested foundations

- Shared map/scenario validators accept the bounded Operations grammar and retain existing-mode validation.
- The shared profile stores serializable Operations state, the attempt journal and profile commit revision. An exclusive write lease and revision check reject stale writers; unknown future schemas are read-only.
- Operations state and cumulative account reward deltas commit in one atomic profile replacement. Identical transaction retries cannot duplicate credits/XP or overwrite newer Campaign progress.
- `OperationsProfileCommandService` runs the existing strategic reducer and publishes its result only after durable commit. A real temporary-directory restart test proves deployment reservation and AP cost persist and a duplicate deploy does not charge again.
- `OperationsReconObjectiveSystem` reads shared ECS entities, not the prototype tactical world. Tests cover explicit 15-second scans, first-scan wave latch, interrupted channels, pause, original roster eligibility, dropped/recovered evidence, safe ground extraction, Partial/Conclude, stale session requests, embarked units and immutable terminal results.
- Scan/interaction geometry currently uses the map's blocked ground footprints; this needs validation against authored Old Quarter geometry during integration. Finite reserve entities now consume those latches through the shared movement queue; arrival behavior still needs a full playthrough.

## Validation

macOS GUI-licensed repository wrapper, Unity 6000.5.2f1:

```text
Tools/CI/invoke_unity_macos.sh --timeout 600
  --log /private/tmp/o001-shared-world-editmode-3.log --
  -runTests -testPlatform EditMode
  -testFilter OperationsReconWorldTests;OperationsProfilePersistenceTests;SaveServiceTests;OperationMapIdentityConfigTests
  -testResults /private/tmp/o001-shared-world-editmode-3.xml
```

Wrapper exit **0**; NUnit **60 passed, 0 failed, 0 skipped**, completed 2026-09-22 16:43:01 UTC. The original save test expected the retired `rush` preset; corrected its expectation to the existing `base_assault` migration behavior without changing that migration.

An earlier broader run also included `ScriptArchitectureAlignmentContractTests` and `EcsBurstHotPathArchitectureTests`: 123 tests, 111 passed, 12 failed. One was the new persistence test's invalid fixture command ID, corrected and passing in the focused run. Eleven architecture failures report existing code outside these implementation changes: array snapshots/mutation debt, seven unclassified non-Burst systems, ScriptableObject reads in Skirmish rules, static localization dictionaries in prototype Operations content, and UI lookup/config/camera dependencies. These failures were not waived or hidden; a clean-baseline rerun was not performed. Full output: `/private/tmp/o001-foundation-editmode-2.log` and matching XML.

## Remaining integration

The normal dashboard now launches the shared world, but no full manual/visible-input win has been accepted. Remaining: complete outcome/return/replay validation, startup-failure refund and recovery choices, actual-world checkpoints, tactical balance and geometry review, ARIA coverage, EN/FA layout verification, cross-mode journeys and target-device/unfamiliar-player acceptance. These are still required; deployment smoke is not player readiness.

## Normal-route integration checkpoint

- Added a typed Operations UI gateway, briefing and live objective controls through the existing shell. The route uses `MatchSceneView` and the same map, input, movement, combat and rendering systems as the other modes.
- Authored an Operations logical map over the validated dense-city physical source, three reachable scan locations, relay evidence and a ground exit. The finite roster is 16 original player infantry, 12 initial enemies and two four-unit reserves. Construction/production is disabled; the authored package retains its 80 Materials and no Oil/Fuel.
- Fixed normal-profile deployment: Unity JSON can materialize a missing run as an empty object, so new-run eligibility uses `OperationsSaveMigration.HasActiveRun`. Physical-source reuse now recognizes the Operations launch owner and still rejects stale hashes and mismatched identities.
- Added inherited-scenery ownership cleanup and replay startup reset. The shared pause Exit command routes into Operations withdrawal confirmation. Full lifecycle behavior is under validation.
- Fixed the opening camera: perspective-footprint clamping originally left the starting squad off-screen. Added an 80 m camera framing margin around the unchanged playable area.
- Launch smoke `/private/tmp/o001-normal-route-smoke-7.log` exited 0 with `result=Passed`, 16 original infantry, 36 total owned entities including reserves, five seconds of real simulation and the starting soldier at viewport `(0.50, 0.50)`. Screenshot: `Build/EditorEvidence/O001SharedWorldLaunch.png`. Input provenance is **button-event integration smoke**, not manual input or an ARIA win.
- Focused integration run `/private/tmp/o001-integration-editmode-3.log` and XML: **74 passed, 0 failed**, wrapper exit 0, no C#/Burst compiler errors. Later patrol/lifecycle additions require their newer validation run.
- The first lifecycle-smoke attempt failed to compile because the Editor assembly lacked a direct Operations Contracts reference. The reference is corrected; that failed attempt is not accepted evidence.
- Editor teardown reports a pre-existing-looking missing-script error while saving `SCN08_MatchHudContent.prefab`. Its cause has not been established on a clean baseline; retain the log and investigate before clean-build acceptance. Earlier focused tests also reported existing persistent-allocation teardown leaks. A passing smoke marker does not waive either issue.

The current implementation is an integration candidate. Persistent active-world recovery, complete mission acceptance and release certification remain open.

### Follow-up validation and corrections

- `/private/tmp/o001-integration-editmode-5.log` and XML: **79 passed, 0 failed**, wrapper exit 0, with no C#/Burst compiler errors. Includes replay-boundary reset/rejection, building and baked-vehicle ownership isolation, patrol orders yielding to combat/pause/terminal, and first-scan target retention.
- Content build `/private/tmp/o001-shared-content-build-6.log` exited 0, passed reachability/finite-roster validation and localization import, and reports `hudMissingScripts=0` on the actual shared HUD prefab before Play Mode. This narrows the earlier teardown warning without treating it as explained.
- Restored the published 30/45-second reserve delays and 80 Materials. The authored initial/reserve split is explicitly 12/4/4; this candidate placement needs tactical review before accepting it over the generic package split.
- The first saved-result integration used a 64-character hex digest where the Operations token contract allows 60 characters. Changed to URL-safe base64 of the full SHA-256 digest (43 characters, no truncation). The next live run confirmed durable withdrawal: reservation cleared and AP 2.
- That run then found an empty briefing after return: the shell's return route did not itself reopen the Operations middle panel. Added the explicit post-return menu-panel request; the complete redeployment journey is being rerun.
- Evidence focus, interaction and world marker now remain locked until all three scans; the HUD reports recovery progress and carried evidence. Full input/pacing/EN–FA review remains outstanding.

### Completed integration-smoke journey

`/private/tmp/o001-lifecycle-smoke-4.log` exited **0** with:

```text
[OperationsReconLaunchSmokeValidation] result=Passed journey=deploy-withdraw-save-return-redeploy input=button-event-smoke original=16 total=36 ap=1
```

This covers normal menu deployment, five seconds of live simulation, visible starting troops, withdrawal confirmation, durable settlement without AP refund/recharge, complete owned-actor cleanup, a functioning Operations briefing after return, and a fresh paid deployment. It does **not** cover a manual win, tactical challenge, active-world checkpoint restoration, platform acceptance or unfamiliar-player usability.

Final content/catalog rebuild `/private/tmp/o001-shared-content-build-7.log`: wrapper exit **0**, roster/reachability pass, `hudMissingScripts=0`, and 368 total configured localization entries including the O001 additions.

### Startup failure recovery

The shared map loader now forwards Operations load errors to the mission owner. A failed queue, explicit load/spawn error, or 180-second startup timeout stops simulation and submits the durable TechnicalFailure command. Return waits for a saved refund; save failures retry every five seconds using the same command identity. English and Persian recovery messages are in the shipping catalog.

- `/private/tmp/o001-recovery-editmode-1.log` and XML: **23 passed, 0 failed**, wrapper exit 0. The new disk test verifies exactly-once refund across service restart and subsequent paid redeployment.
- `/private/tmp/o001-recovery-content-build.log`: wrapper exit 0, content/reachability pass, `hudMissingScripts=0`, localization import passed with 370 entries.
- `/private/tmp/o001-recovery-smoke-1.log`: wrapper exit 0 with `[OperationsReconLaunchSmokeValidation] result=Passed journey=deploy-failed-startup-refund-return-redeploy input=button-event-smoke original=16 total=36 ap=2`.

The smoke deliberately injects a startup failure in the Editor fixture. It verifies refund/return/redeployment, not a normal-input mission win or recovery of an interrupted combat world. Full logs remain at the paths above; no additional raw machine logs are published in this change.
