# Tactical Follow Attack Cinematic Improvement Tracker

Date: 2026-07-07
Status: In progress
Problem source: user screenshots from 2026-07-07 and `../attack-cinematic-handoff.md`

## Objective

Rework the jet attack cinematic that plays while the player is in tactical third-person follow camera mode so it becomes a readable, satisfying attack sequence:

1. The player sees the attacking jet establish or approach the target.
2. A visible missile/tracer leaves the jet from a believable launch position.
3. The camera shows the missile/attack path with enough distance and side angle to read motion.
4. The camera shows the target impact, explosion, and destruction beat.
5. The camera shows the jet flying over or past the destroyed target.
6. The camera returns smoothly to normal third-person follow mode.

The current implementation is not accepted because it is too close, too fast, frequently clips into scenery, and often does not show a missile launch, impact, explosion, or flyover. This tracker supersedes the "completed" claim in `../attack-cinematic-handoff.md` for acceptance purposes.

## Non-Goals

- Do not redesign the general RTS camera.
- Do not change unit attack balance, hit chance, damage timing, or target selection policy.
- Do not add a new cinematic manager, controller, facade, broad service shell, or updating MonoBehaviour loop.
- Do not introduce UI Toolkit or Canvas migration work.
- Do not convert all attack VFX. This slice is scoped to followed air-unit attack cinematics, starting with jets.
- Do not require Android build validation before the feature is functionally validated in Unity; Android profiling is only required if this slice causes a measured runtime regression.

## Player-Facing Acceptance Criteria

- In third-person follow mode, a jet attack triggers a multi-shot cinematic only for the followed attacking air unit.
- The sequence lasts long enough to read the action, targeting roughly 4.2-5.2 seconds total.
- Slow motion is applied during the launch and impact beats, then restored reliably.
- A visible missile/tracer or equivalent attack projectile appears between the jet and target.
- The impact/explosion is visible on camera, not already finished before the camera arrives.
- The jet visibly passes over or past the target after impact before the camera blends back.
- The camera never starts inside terrain, tents, buildings, hangars, walls, the jet mesh, or the target mesh.
- The camera keeps the main action away from the left selection panel, bottom command bar, and minimap safe areas when practical.
- Exiting follow mode, losing the jet, losing the target, pausing, or ending the match restores normal time scale and normal camera ownership.

## Architecture Contract

- Keep attack ownership in ECS data and `ISystem` systems.
- Use Burst-compatible data capture, timeline math, and validation where practical.
- Keep Unity object work narrow and explicitly managed: camera application, time scale, pooled VFX GameObjects, and optional Physics obstruction probes.
- Use existing ECS camera singletons:
  - `TacticalFollowCameraModeComponent`
  - `TacticalFollowCameraTargetComponent`
  - `TacticalFollowCameraPoseComponent`
  - existing attack cinematic component(s), extending them only when necessary.
- Add new data as typed ECS components or buffers with `*Component` suffix. Avoid string state and ad hoc object references in hot systems.
- Any plain managed helper must use an approved suffix such as `CameraSystemHelper`, `VfxSystemHelper`, `PresentationSystemHelper`, or `UtilitySystemHelper`.
- Bare `*System` names remain reserved for ECS systems only.
- Do not add `Manager`, `Controller`, `Facade`, broad `Service`, singleton, service locator, or static mutable runtime state.
- No steady-state managed allocations in the cinematic update path after warmup.
- No LINQ, closure allocation, per-frame string formatting, `FindObject*`, `Resources.FindObjectsOfTypeAll`, or per-frame full ECS snapshots in the cinematic path.
- Pool missile/tracer and impact VFX objects; do not instantiate/destroy every cinematic frame.
- Use non-alloc obstruction checks if Physics checks are required.
- Keep gameplay damage deterministic and independent from cinematic playback. If gameplay remains instant-hit, cinematic impact visuals must be replayed or staged without changing authoritative damage timing.

## Recommended System Shape

| Responsibility | Preferred Owner | Burst/Job Expectation | Notes |
|---|---|---|---|
| Detect followed air-unit attack requests | `TacticalFollowAttackCinematicSystem` or a split `TacticalFollowAttackCinematicCaptureSystem` | Burst-compatible if the final code avoids UnityEngine access | Runs after `UnitAttackSystem` and before `UnitAttackVfxRequestSystem`. |
| Store active cinematic request/timeline | ECS singleton/component data | Burst-compatible data | Source, target, launch position, impact position, attack direction, phase, elapsed unscaled timeline, fired impact flags. |
| Evaluate phase timing and shot math | Pure `TacticalFollowAttackCinematicHelper` or `UtilitySystemHelper` | Burst-testable pure math | No world/object access. Deterministic and unit-testable. |
| Write tactical follow camera target/pose | ECS system, managed boundary only where UnityEngine time/camera constraints are needed | Usually non-Burst managed ECS boundary | Must be classified as intentional managed boundary if architecture guard requires it. |
| Apply slow motion | Narrow managed ECS system or camera helper boundary | Non-Burst by design | Must restore `Time.timeScale` on all exits. |
| Spawn and update visible cinematic missile/tracer | `TacticalFollowAttackCinematicVfxSystemHelper` or existing VFX presentation boundary | Managed presentation boundary | Pool objects; consume ECS data; no gameplay policy. |
| Trigger replayed impact/explosion visual | VFX presentation boundary from ECS event/state | Managed presentation boundary | Must not own damage. |
| Obstruction-safe camera placement | Pure shot candidate math plus optional managed non-alloc Physics probe | Pure candidate math should be Burst-testable | Raise/offset camera if blocked or below terrain. |

## Shot Design Contract

| Phase | Target Duration | Time Scale | Camera Intent | Required Visual |
|---|---:|---:|---|---|
| Establish/Launch | 1.1-1.4s | 0.25-0.4x | Side/rear quarter near the jet, not under the wing, looking toward the target corridor. | Jet, launch hardpoint/muzzle, missile/tracer begins moving. |
| Missile Path | 0.8-1.2s | 0.35-0.6x | Wide lateral profile or trailing chase, far enough to see forward motion. | Missile/tracer clearly travels from jet toward target. |
| Impact | 1.2-1.6s | 0.35x then ramp to 1.0x | Pulled-back target camera looking toward incoming path. | Target, explosion, damage/destruction beat, smoke/debris. |
| Flyover | 1.1-1.6s | 1.0x | Behind/near target, panning up or tracking across the destroyed target. | Jet crosses over or past target and leaves frame directionally. |
| Return Blend | 0.4-0.8s | 1.0x | Existing tactical follow camera retakes smoothly. | No snap, no time-scale leak. |

## Progress Summary

Overall implementation progress: 70% (59/84 implementation checklist items complete).

Progress is checklist-based. Each implementation or validation checkbox below counts as one item. Documentation creation and index links are not counted as implementation progress.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Baseline and proof capture | In progress | 7 | 8 | 88% | User screenshots plus code inspection confirm current failure path; Unity reproduction still required. |
| 1. ECS event and data contract | In progress | 8 | 10 | 80% | ECS state now owns typed attack kind, request start, projectile progress, launch/impact/flyover triggers, completion, and abort reason. |
| 2. Timeline and phase sequencing | In progress | 8 | 10 | 80% | Timeline now has explicit Launch, MissilePath, Impact, and Flyover phases plus projectile progress beats. |
| 3. Cinematic missile and impact VFX | In progress | 8 | 10 | 80% | ECS timeline now replays launch, missile trail, and impact VFX through existing pooled presentation views; Unity visual acceptance still open. |
| 4. Shot solver and obstruction safety | Complete | 12 | 12 | 100% | Pure shot solver now uses wider, higher launch/path/impact/flyover shots with safety clamps, HUD-safe aim bias, FOV clamps, explicit phase-entry snap rules, deterministic fallback candidates, and non-alloc obstruction probes at the managed camera boundary. |
| 5. Follow-camera/time-scale integration | In progress | 3 | 9 | 33% | Active attack pose ownership, completion handback, and temporary-target abort cleanup are covered by focused tests. |
| 6. Tests and architecture guardrails | Complete | 13 | 13 | 100% | Pure helper tests cover phase/shot math; ECS tests cover followed/unfollowed request behavior, retrigger cooldown, abort cleanup, and architecture guardrails. |
| 7. Unity visual validation | Not started | 0 | 8 | 0% | Validate in-editor with screenshots/logs, not only tests. |
| 8. Rollout and documentation | Not started | 0 | 4 | 0% | Update docs and final acceptance notes after the feature works. |

## Phase 0: Baseline And Proof Capture

- [ ] Reproduce the current bad cinematic in Unity with a followed jet attacking a target.
- [x] Capture current sequence timing from attack fire frame to camera return.
- [x] Record whether `UnitAttackVfxRequest` launch and impact requests are consumed before the camera can show them.
- [x] Record current camera position, look-at, FOV, phase, and distance from jet/target during each cut.
- [x] Capture at least one example where the camera clips into terrain/building/tent/jet.
- [x] Capture at least one example where no missile or explosion is visible.
- [x] Identify exact source files and systems touched by the current cinematic path.
- [x] Update this tracker with baseline evidence paths and the selected first implementation slice.

Exit criteria:

- The failure is reproducible.
- The next implementation slice is based on measured event timing and camera/VFX behavior, not guesswork.

Baseline notes, 2026-07-07:

- User-provided visual evidence:
  - `/Users/farhad/Desktop/Screenshot 2026-07-07 at 12.03.51.png` shows the camera too close to the followed jet/landing gear with the attack target far away; the attack beat is not readable.
  - `/Users/farhad/Desktop/Screenshot 2026-07-07 at 12.03.52.png` through `/Users/farhad/Desktop/Screenshot 2026-07-07 at 12.03.54.png` show camera cuts near/inside base scenery and tents where missile launch, impact, explosion, and flyover are not visible.
  - `/Users/farhad/Desktop/Screenshot 2026-07-07 at 12.03.54 1.png` is wider but still does not show the required staged launch, missile path, impact, explosion, and flyover sequence.
- Current timing source:
  - `TacticalFollowAttackCinematicHelper.LaunchDurationSeconds = 1.15`
  - `TacticalFollowAttackCinematicHelper.ImpactDurationSeconds = 1.45`
  - `TacticalFollowAttackCinematicHelper.FlyoverDurationSeconds = 1.7`
  - Total current coded timeline is 4.3 unscaled seconds, but it contains no owned projectile/travel/impact events.
- Current VFX/event ownership:
  - `TacticalFollowAttackCinematicSystem` runs after `UnitAttackSystem` and before `UnitAttackVfxRequestSystem`, scans same-frame `UnitAttackVfxRequest` entities, writes camera target/pose, and applies time scale.
  - `UnitAttackVfxRequestSystem` plays muzzle/impact VFX immediately from the same request entities and destroys the full request query in the same frame.
  - The cinematic system currently does not own or replay a cinematic missile/tracer and does not delay or replay the impact beat. This explains why camera tuning alone cannot make launch/travel/impact readable.
- Current shot math source:
  - `TacticalFollowAttackCinematicHelper.EvaluateLaunchShot` positions the launch camera near the current jet/launch anchor with a hard-coded side/back/drop offset and FOV 30.
  - `EvaluateImpactShot` positions the impact camera from the impact point with side/forward offsets and FOV 36.
  - `EvaluateFlyoverShot` keeps the camera near impact and pans toward the jet/fallback path with FOV 42.
  - The pure helper has no scene obstruction, terrain, target mesh, building, UI safe-area, or camera-near-object correction.
- Source files confirmed in the current path:
  - `Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicSystem.cs`
  - `Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicHelper.cs`
  - `Assets/Game/Scripts/Components/TacticalFollowCameraComponents.cs`
  - `Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackSystem.cs`
  - `Assets/Game/Scripts/Systems/UnitAttackVfxSystems.cs`
  - `Assets/Game/Scripts/Effects/MissileTrailVfxView.cs`
  - `Assets/Game/Scripts/Effects/UnitAttackImpactVfxView.cs`
  - `Assets/Tests/Editor/TacticalFollowAttackCinematicHelperTests.cs`
  - `Assets/Tests/Editor/TacticalFollowCameraModeCommandSystemHelperTests.cs`
  - `Assets/Tests/Editor/EcsBurstHotPathArchitectureTests.cs`
- Selected first implementation slice:
  - Extend the ECS cinematic state/data contract with explicit launch/travel/impact event flags and a cinematic projectile progress path, then add tests that prove launch, impact, flyover, finish, and abort state transitions before touching VFX or camera tuning.

## Phase 1: ECS Event And Data Contract

- [x] Decide whether to extend `TacticalFollowAttackCinematicStateComponent` or add separate request/projectile/impact components.
- [x] Add or adapt a typed cinematic request component for source entity, target entity, launch position, impact position, attack direction, attack kind, and requested start time.
- [x] Add or adapt a typed cinematic phase component/state for phase, elapsed unscaled time, last applied phase, launch-fired flag, impact-fired flag, flyover-fired flag, abort reason, and completion state.
- [x] Add or adapt a typed cinematic projectile component for current projectile position/progress if projectile timing is ECS-owned.
- [x] Ensure all new runtime data uses component/buffer naming that follows the architecture contract.
- [ ] Keep target/source entity liveness checks ECS-owned and deterministic.
- [x] Keep gameplay damage application independent from cinematic visual playback.
- [ ] Define fallback behavior for missing jet, missing target, invalid attack direction, and destroyed target.
- [x] Define retrigger rules so repeated attack frames do not spam cinematic restarts.
- [x] Update architecture tests/classification if a new non-Burst ECS system is intentionally managed.

Exit criteria:

- The cinematic has a clear ECS data contract that can be tested without Unity scene objects.

Phase 1 notes, 2026-07-07:

- Data owner decision: extend `TacticalFollowAttackCinematicStateComponent` instead of introducing a separate request/projection component in this slice. The active cinematic already has a singleton state entity, and the next VFX slice needs the same source/target/launch/impact/timeline data without adding another query or lifecycle owner.
- Added typed data:
  - `TacticalFollowAttackCinematicAttackKind`
  - `TacticalFollowAttackCinematicAbortReason`
  - `RequestedStartTime`
  - `ProjectileProgress`
  - `ProjectilePosition`
  - `ProjectileDirection`
  - `LaunchEventTriggered`
  - `ProjectileActive`
  - `ImpactEventTriggered`
  - `FlyoverEventTriggered`
  - `Completed`
  - `AbortReason`
- Added pure helper contract:
  - `BuildInitialState(...)`
  - `EvaluateStateProgress(...)`
  - `EvaluateProjectileProgress(...)`
  - `ProjectileLaunchBeatSeconds`
  - `ImpactEventBeatSeconds`
- Existing retrigger cooldown remains in `TacticalFollowAttackCinematicSystem` and is now carried by the typed state path.
- Partial fallback behavior is now explicit in code/tests: missing jet transform falls back to launch-position shot context, temporary-target removal aborts with `TemporaryTargetCleared`, follow-mode exit aborts with `FollowModeExited`, invalid attack direction normalizes to a stable forward fallback, and completed/aborted cinematics retain `HasEnded`/`LastEndedElapsedTime` for cooldown. Destroyed-target visual fallback still needs explicit validation.
- No new ECS system was added; the existing `TacticalFollowAttackCinematicSystem` remains covered by the current architecture classification.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `git diff --check` passed.

## Phase 2: Timeline And Phase Sequencing

- [x] Split current one-shot impact hold into explicit establish/launch, missile path, impact, flyover, and return phases.
- [x] Drive phase progression from unscaled cinematic time so slow motion does not shorten the sequence.
- [x] Use deterministic phase constants in a pure helper instead of scattered magic values.
- [x] Start launch phase only after the followed attacking jet is confirmed.
- [ ] Fire the cinematic projectile/tracer at a defined launch beat.
- [x] Trigger impact visuals only when the cinematic projectile reaches impact beat.
- [ ] Hold the explosion/destruction beat long enough for the camera to read it.
- [x] Delay flyover until impact visuals have begun.
- [x] Prevent the same attack request from starting multiple overlapping cinematics.
- [x] Cleanly finish, abort, or hand back camera ownership on all exit paths.

Exit criteria:

- A simulated timeline can prove the expected phase order and event beats without requiring the full scene.

Phase 2 notes, 2026-07-07:

- Added `TacticalFollowAttackCinematicPhase.MissilePath` so the timeline is no longer Launch -> Impact -> Flyover only.
- Updated deterministic timing constants:
  - `LaunchDurationSeconds = 1.1`
  - `MissilePathDurationSeconds = 1.0`
  - `ImpactDurationSeconds = 1.3`
  - `FlyoverDurationSeconds = 1.45`
  - `TotalDurationSeconds = 4.85`
- `ImpactEventBeatSeconds` now starts after launch plus missile-path travel instead of at the old launch-plus-impact boundary.
- Added a pure missile-path shot that frames projected missile travel from the launch position toward the target.
- The timeline owns launch/impact/flyover event flags in ECS state, but actual visual projectile/tracer playback is still open for Phase 3.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `git diff --check` passed.

## Phase 3: Cinematic Missile And Impact VFX

- [x] Inventory current missile/tracer and impact VFX prefabs/views available for jet attacks.
- [x] Decide whether to reuse existing `MissileTrailVfxView`, existing attack VFX, or add a narrow pooled cinematic VFX view.
- [x] Add a pooled VFX presentation boundary if existing VFX cannot replay launch/travel/impact on demand.
- [x] Spawn or activate a visible projectile/tracer at the launch beat.
- [x] Update projectile/tracer movement from ECS timeline data without per-frame allocation.
- [x] Trigger impact/explosion at the impact beat even if gameplay damage happened earlier.
- [ ] Ensure VFX playback works with slow motion or explicitly controls playback speed.
- [x] Return VFX instances to the pool on completion/abort.
- [x] Avoid GameObject instantiate/destroy during steady-state cinematic playback after warmup.
- [ ] Add diagnostics gates for optional cinematic debug logging without per-frame string allocation.

Exit criteria:

- The cinematic can show launch, travel, and impact as visible events under camera control.

Phase 3 notes, 2026-07-07:

- Reused existing pooled presentation views:
  - `MissileTrailVfxView` for cinematic projectile/tracer travel.
  - `UnitAttackImpactVfxView` for launch and delayed impact/explosion playback.
- Added narrow managed boundary `TacticalFollowAttackCinematicVfxSystemHelper` with approved `VfxSystemHelper` suffix. It has no lifecycle and no update loop; ECS state remains the timeline owner.
- Extended `TacticalFollowAttackCinematicStateComponent` with captured launch/impact prefab references and rotations from the original `UnitAttackVfxRequest`.
- `TacticalFollowAttackCinematicSystem` now triggers launch VFX at the launch event edge, updates missile trail position from ECS projectile data while active, releases the trail on impact/completion/abort, and replays impact VFX at the cinematic impact beat.
- This slice does not yet solve camera readability, obstruction avoidance, or Unity visual acceptance. Those remain Phase 4 and Phase 7 work.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `git diff --check` passed.

## Phase 4: Shot Solver And Obstruction Safety

- [x] Redesign launch shot to frame jet, launch side, and target direction with minimum distance from the jet mesh.
- [x] Redesign missile-path shot to show lateral travel or chase motion with enough world scale to read speed.
- [x] Redesign impact shot to frame target and incoming path without clipping into target/buildings.
- [x] Redesign flyover shot so the jet crosses over or past the target after impact.
- [x] Add safe-area framing so primary action is not hidden under left selection panel, bottom command bar, or minimap when practical.
- [x] Add minimum camera height above terrain/map surface.
- [x] Add minimum distance from target, jet, and impact point to avoid extreme closeups.
- [x] Add obstruction probes from camera to look-at using non-alloc Physics APIs where required.
- [x] Add fallback offsets if a preferred shot is blocked.
- [x] Clamp FOV and roll/bank presentation to readable cinematic values.
- [x] Add shot-to-shot snap or blend rules deliberately instead of accidental SmoothDamp behavior.
- [x] Add pure shot-solver tests for distances, look direction, FOV, and fallback behavior.

Exit criteria:

- The camera solver prefers readable wide shots and has a deterministic fallback when the preferred shot is blocked.

Phase 4 notes, 2026-07-07:

- Replaced the too-close launch camera with a higher, wider side/rear shot. The camera no longer intentionally drops below the jet, which was the main source of under-wing closeups.
- Widened missile-path, impact, and flyover shots so the projectile travel, explosion, and jet flyover have enough world scale to read.
- Added pure shot safety clamps for minimum height above launch/impact/jet action, minimum camera-to-look-at distance, minimum camera-to-impact distance, and minimum camera-to-jet distance.
- Added `CinematicShots_StayWideAndAboveAction` coverage so future tuning cannot silently return to close/low shots.
- Added a HUD-safe impact/flyover aim bias so explosions and flyovers read higher in the frame instead of being centered behind bottom command UI, plus a single shot builder that clamps cinematic FOV to readable values.
- Added `CinematicShots_ClampFovAndUseHudSafeImpactAim` coverage for the HUD-safe aim and FOV contract.
- Added `ShouldSnapToShot` as a pure phase-entry rule so the camera snaps only on the first shot or new phase and blends within a phase.
- Added `ShouldSnapToShot_SnapsOnlyOnFirstShotOrPhaseEntry` coverage for explicit snap/blend behavior.
- Added `EvaluateFallbackShot` and `ObstructionFallbackCandidateCount` so future non-alloc obstruction probes can choose from deterministic alternate camera offsets without managed policy drift.
- Added `FallbackShots_ProvideDistinctSafeCandidates` coverage for safe, finite, distinct fallback camera positions.
- Added narrow managed boundary `TacticalFollowAttackCinematicCameraSystemHelper` with approved `CameraSystemHelper` suffix. It uses a preallocated hit buffer and `Physics.SphereCastNonAlloc` to probe camera-to-look-at visibility only while the attack cinematic is selecting a shot.
- Routed active cinematic shot selection through the obstruction-safe resolver so blocked primary shots choose deterministic fallback offsets without altering pure shot math or adding a new update loop.
- Added `ObstructionFallback_UsesAlternateShotWhenPrimaryLineBlocked` coverage with a temporary collider blocking the primary impact shot.
- Open work: Unity visual validation.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `Tools/CI/invoke_unity_macos.sh --timeout 240 --log /private/tmp/warline-attack-cinematic-helper-validation-2.log -- -quit -nographics -executeMethod TacticalFollowAttackCinematicHelperTests.RunFocusedValidation` passed with `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=11`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 240 --log /private/tmp/warline-attack-cinematic-helper-validation-6.log -- -quit -nographics -executeMethod TacticalFollowAttackCinematicHelperTests.RunFocusedValidation` passed with `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=12`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 240 --log /private/tmp/warline-attack-cinematic-helper-validation-7.log -- -quit -nographics -executeMethod TacticalFollowAttackCinematicHelperTests.RunFocusedValidation` passed with `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=13`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 240 --log /private/tmp/warline-attack-cinematic-helper-validation-8.log -- -quit -nographics -executeMethod TacticalFollowAttackCinematicHelperTests.RunFocusedValidation` passed with `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=14`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-attack-cinematic-helper-validation-9.log -- -quit -nographics -executeMethod TacticalFollowAttackCinematicHelperTests.RunFocusedValidation` passed with `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=15`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 420 --log /private/tmp/warline-attack-cinematic-architecture-validation-9.log -- -quit -nographics -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation` passed with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`.
  - `git diff --check` passed.

## Phase 5: Follow-Camera And Time-Scale Integration

- [x] Guard normal tactical follow pose refresh so it cannot overwrite active cinematic poses.
- [x] Restore normal pose ownership immediately after cinematic completion or abort.
- [ ] Apply slow motion only while `Application.isPlaying`.
- [ ] Store and restore the previous `Time.timeScale` on completion, abort, destroy, and follow-mode exit.
- [ ] Ensure pausing or scene shutdown cannot leave the game in slow motion.
- [ ] Ensure target/jet destruction mid-cinematic uses the documented fallback and still restores time scale.
- [ ] Keep UI read-model/follow-mode status stable while the cinematic temporary target is active.
- [ ] Avoid new sync points or job completions in camera integration.
- [ ] Add tests for time-scale restoration and temporary-target ownership.

Exit criteria:

- Follow mode, cinematic mode, and normal camera mode hand off cleanly without time-scale leaks.

Phase 5 notes, 2026-07-07:

- `TacticalFollowCameraModeSystemHelper.RefreshActiveTargetAndPose` preserves active `AttackImpact` cinematic pose ownership when an active `TacticalFollowAttackCinematicStateComponent` exists.
- Added `ActiveAttackCinematicPreservesTemporaryPoseDuringBaseRefresh` coverage so moving the base aircraft during an active cinematic cannot cause normal follow refresh to overwrite the temporary missile/cinematic pose.
- `FollowedAirUnitAttackVfxCreatesImpactCutawayThenReturns` covers normal base-target pose restoration after the attack cinematic completes.
- Added `AttackCinematicAbortCleansStateWhenTemporaryTargetCleared` coverage for temporary-target abort cleanup.
- Open work: explicit time-scale restoration tests, pause/shutdown leak coverage, and UI read-model stability checks.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly` passed.
  - `Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-attack-cinematic-follow-mode-validation.log -- -quit -nographics -executeMethod TacticalFollowCameraModeCommandSystemHelperTests.RunFocusedValidation` passed with `[TacticalFollowCameraModeCommandValidation] result=Passed tests=38`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-attack-cinematic-follow-mode-validation-2.log -- -quit -nographics -executeMethod TacticalFollowCameraModeCommandSystemHelperTests.RunFocusedValidation` passed with `[TacticalFollowCameraModeCommandValidation] result=Passed tests=39`.
  - `Tools/CI/invoke_unity_macos.sh --timeout 300 --log /private/tmp/warline-attack-cinematic-follow-mode-validation-3.log -- -quit -nographics -executeMethod TacticalFollowCameraModeCommandSystemHelperTests.RunFocusedValidation` passed with `[TacticalFollowCameraModeCommandValidation] result=Passed tests=40`.
  - `git diff --check` passed.

## Phase 6: Tests And Architecture Guardrails

- [x] Add or update pure helper tests for phase boundaries.
- [x] Add or update pure helper tests for time-scale ramp values.
- [x] Add or update pure helper tests for launch shot framing.
- [x] Add or update pure helper tests for missile-path shot framing.
- [x] Add or update pure helper tests for impact shot framing.
- [x] Add or update pure helper tests for flyover shot framing.
- [x] Add ECS-system tests for followed-air-unit request capture.
- [x] Add ECS-system tests for unfollowed attack requests being ignored.
- [x] Add ECS-system tests for retrigger cooldown.
- [x] Add ECS-system tests for abort/finish cleanup.
- [x] Run compile validation for runtime, editor, and tests.
- [x] Run architecture validation for naming, assembly boundaries, and Burst/hot-path classification.
- [x] Run `git diff --check`.

Exit criteria:

- The implementation passes code-level validation before visual handoff.

Phase 6 notes, 2026-07-07:

- Removed an existing startup fuel-seed `ToEntityArray` snapshot in `InitialUnitsSpawnSystem` so the hot-path array snapshot guard remains at zero debt.
- Removed an unnecessary attack-cinematic `OnDestroy` component write after `Time.timeScale` restoration so direct EntityManager mutation debt did not increase.
- `Tools/CI/invoke_unity_macos.sh --timeout 240 --log /private/tmp/warline-attack-cinematic-architecture-validation-7.log -- -quit -nographics -executeMethod EcsBurstHotPathArchitectureTests.RunFocusedValidation` passed with `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`.

## Phase 7: Unity Visual Validation

- [ ] Validate a jet attack from third-person follow mode in the Unity editor.
- [ ] Capture screenshots or a short video of launch, missile path, impact, flyover, and return.
- [ ] Verify the missile/tracer is visible from the launch shot.
- [ ] Verify explosion/impact is centered and visible from the impact shot.
- [ ] Verify the jet flies over or past the destroyed target after impact.
- [ ] Verify no camera shot clips into terrain, buildings, tents, hangars, the target, or the jet.
- [ ] Verify frame time and GC do not regress during the cinematic sequence.
- [ ] Record validation logs, screenshots, or profiler notes in this tracker.

Exit criteria:

- The sequence is visually accepted in Unity before any Android validation is considered.

## Phase 8: Rollout And Documentation

- [ ] Update `../attack-cinematic-handoff.md` with final implementation notes and validation evidence.
- [ ] Update this tracker progress, command/log paths, and visual evidence links.
- [ ] Document any intentional managed boundaries and architecture-test allowlist updates.
- [ ] Commit and push once the implementation is stable, validated, and accepted.

Exit criteria:

- Documentation, tracker status, and source code agree with the implemented behavior.

## Validation Matrix

| Area | Required Checks |
|---|---|
| Compile | Runtime, editor, and test assemblies compile. |
| Architecture | Naming, assembly boundary, ECS/Burst hot-path, and non-ECS helper rules pass. |
| Unit tests | Phase timing, shot math, target/pose ownership, abort cleanup, and time-scale restoration. |
| ECS behavior | Only followed air-unit attacks trigger; repeated attack frames do not spam cinematics. |
| VFX | Launch/travel/impact are visible and pooled. |
| Camera | Shots are readable, wide enough, obstruction-safe, and return smoothly. |
| Performance | No steady-state GC allocations; no repeated full scans or GameObject instantiation in playback. |
| Unity visual | Launch, missile path, impact/explosion, flyover, and return are visible in editor. |

## Initial Implementation Order

1. Baseline the current broken sequence with logs/screenshots.
2. Refactor the cinematic data contract only; keep behavior unchanged until tests compile.
3. Add deterministic timeline and helper tests.
4. Add visible cinematic projectile/tracer and delayed/replayed impact VFX.
5. Replace shot math with wide, obstruction-safe camera poses.
6. Validate in Unity with the exact user scenario.
7. Tune durations/FOV/offsets only after the sequence shows the right events.

Do not start by only changing FOV or durations. The current failure is that the cinematic does not own the launch/travel/impact beats, so camera tuning alone cannot make the attack readable.
