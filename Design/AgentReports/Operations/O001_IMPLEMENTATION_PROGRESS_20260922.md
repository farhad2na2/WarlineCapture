# O001 implementation progress — 2026-09-22

Status: **In development. Not player-ready.** No R1–R6 exit gate is claimed complete.

## Tested foundations

- Shared map/scenario validators accept the bounded Operations grammar and retain existing-mode validation.
- The shared profile stores serializable Operations state, the attempt journal and profile commit revision. An exclusive write lease and revision check reject stale writers; unknown future schemas are read-only.
- Operations state and cumulative account reward deltas commit in one atomic profile replacement. Identical transaction retries cannot duplicate credits/XP or overwrite newer Campaign progress.
- `OperationsProfileCommandService` runs the existing strategic reducer and publishes its result only after durable commit. A real temporary-directory restart test proves deployment reservation and AP cost persist and a duplicate deploy does not charge again.
- `OperationsReconObjectiveSystem` reads shared ECS entities, not the prototype tactical world. Tests cover explicit 15-second scans, first-scan wave latch, interrupted channels, pause, original roster eligibility, dropped/recovered evidence, safe ground extraction, Partial/Conclude, stale session requests, embarked units and immutable terminal results.
- Scan/interaction geometry currently uses the map's blocked ground footprints; this needs validation against authored Old Quarter geometry during integration. Reinforcement latches are not yet connected to actual wave arrivals.

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

The normal dashboard still does not launch this ECS recon implementation. Next: explicit launch/content configuration, real map and complete roster, normal HUD scan/recover/extraction controls, finite waves, durable actual-world checkpoints, result settlement and clean cross-mode return. Then manual/visible-input ARIA runs, EN/FA verification and target-device/unfamiliar-player acceptance. The tested system alone does not satisfy these gates.
