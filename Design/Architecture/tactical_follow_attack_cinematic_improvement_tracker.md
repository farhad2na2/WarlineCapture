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

Overall implementation progress: 8% (7/84 implementation checklist items complete).

Progress is checklist-based. Each implementation or validation checkbox below counts as one item. Documentation creation and index links are not counted as implementation progress.

| Phase | Status | Complete | Total | Progress | Notes |
|---|---|---:|---:|---:|---|
| 0. Baseline and proof capture | In progress | 7 | 8 | 88% | User screenshots plus code inspection confirm current failure path; Unity reproduction still required. |
| 1. ECS event and data contract | Not started | 0 | 10 | 0% | Define the accepted data ownership for cinematic request, phase, projectile, impact, and abort state. |
| 2. Timeline and phase sequencing | Not started | 0 | 10 | 0% | Replace instant camera cut behavior with a staged sequence. |
| 3. Cinematic missile and impact VFX | Not started | 0 | 10 | 0% | Make launch, missile travel, and impact visible even when gameplay damage is instant. |
| 4. Shot solver and obstruction safety | Not started | 0 | 12 | 0% | Solve wide readable camera positions and prevent clipping. |
| 5. Follow-camera/time-scale integration | Not started | 0 | 9 | 0% | Preserve normal follow camera ownership and restore time scale reliably. |
| 6. Tests and architecture guardrails | Not started | 0 | 13 | 0% | Cover pure math, state transitions, no drift, and compile validation. |
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

- [ ] Decide whether to extend `TacticalFollowAttackCinematicStateComponent` or add separate request/projectile/impact components.
- [ ] Add or adapt a typed cinematic request component for source entity, target entity, launch position, impact position, attack direction, attack kind, and requested start time.
- [ ] Add or adapt a typed cinematic phase component/state for phase, elapsed unscaled time, last applied phase, launch-fired flag, impact-fired flag, flyover-fired flag, abort reason, and completion state.
- [ ] Add or adapt a typed cinematic projectile component for current projectile position/progress if projectile timing is ECS-owned.
- [ ] Ensure all new runtime data uses component/buffer naming that follows the architecture contract.
- [ ] Keep target/source entity liveness checks ECS-owned and deterministic.
- [ ] Keep gameplay damage application independent from cinematic visual playback.
- [ ] Define fallback behavior for missing jet, missing target, invalid attack direction, and destroyed target.
- [ ] Define retrigger rules so repeated attack frames do not spam cinematic restarts.
- [ ] Update architecture tests/classification if a new non-Burst ECS system is intentionally managed.

Exit criteria:

- The cinematic has a clear ECS data contract that can be tested without Unity scene objects.

## Phase 2: Timeline And Phase Sequencing

- [ ] Split current one-shot impact hold into explicit establish/launch, missile path, impact, flyover, and return phases.
- [ ] Drive phase progression from unscaled cinematic time so slow motion does not shorten the sequence.
- [ ] Use deterministic phase constants in a pure helper instead of scattered magic values.
- [ ] Start launch phase only after the followed attacking jet is confirmed.
- [ ] Fire the cinematic projectile/tracer at a defined launch beat.
- [ ] Trigger impact visuals only when the cinematic projectile reaches impact beat.
- [ ] Hold the explosion/destruction beat long enough for the camera to read it.
- [ ] Delay flyover until impact visuals have begun.
- [ ] Prevent the same attack request from starting multiple overlapping cinematics.
- [ ] Cleanly finish, abort, or hand back camera ownership on all exit paths.

Exit criteria:

- A simulated timeline can prove the expected phase order and event beats without requiring the full scene.

## Phase 3: Cinematic Missile And Impact VFX

- [ ] Inventory current missile/tracer and impact VFX prefabs/views available for jet attacks.
- [ ] Decide whether to reuse existing `MissileTrailVfxView`, existing attack VFX, or add a narrow pooled cinematic VFX view.
- [ ] Add a pooled VFX presentation boundary if existing VFX cannot replay launch/travel/impact on demand.
- [ ] Spawn or activate a visible projectile/tracer at the launch beat.
- [ ] Update projectile/tracer movement from ECS timeline data without per-frame allocation.
- [ ] Trigger impact/explosion at the impact beat even if gameplay damage happened earlier.
- [ ] Ensure VFX playback works with slow motion or explicitly controls playback speed.
- [ ] Return VFX instances to the pool on completion/abort.
- [ ] Avoid GameObject instantiate/destroy during steady-state cinematic playback after warmup.
- [ ] Add diagnostics gates for optional cinematic debug logging without per-frame string allocation.

Exit criteria:

- The cinematic can show launch, travel, and impact as visible events under camera control.

## Phase 4: Shot Solver And Obstruction Safety

- [ ] Redesign launch shot to frame jet, launch side, and target direction with minimum distance from the jet mesh.
- [ ] Redesign missile-path shot to show lateral travel or chase motion with enough world scale to read speed.
- [ ] Redesign impact shot to frame target and incoming path without clipping into target/buildings.
- [ ] Redesign flyover shot so the jet crosses over or past the target after impact.
- [ ] Add safe-area framing so primary action is not hidden under left selection panel, bottom command bar, or minimap when practical.
- [ ] Add minimum camera height above terrain/map surface.
- [ ] Add minimum distance from target, jet, and impact point to avoid extreme closeups.
- [ ] Add obstruction probes from camera to look-at using non-alloc Physics APIs where required.
- [ ] Add fallback offsets if a preferred shot is blocked.
- [ ] Clamp FOV and roll/bank presentation to readable cinematic values.
- [ ] Add shot-to-shot snap or blend rules deliberately instead of accidental SmoothDamp behavior.
- [ ] Add pure shot-solver tests for distances, look direction, FOV, and fallback behavior.

Exit criteria:

- The camera solver prefers readable wide shots and has a deterministic fallback when the preferred shot is blocked.

## Phase 5: Follow-Camera And Time-Scale Integration

- [ ] Guard normal tactical follow pose refresh so it cannot overwrite active cinematic poses.
- [ ] Restore normal pose ownership immediately after cinematic completion or abort.
- [ ] Apply slow motion only while `Application.isPlaying`.
- [ ] Store and restore the previous `Time.timeScale` on completion, abort, destroy, and follow-mode exit.
- [ ] Ensure pausing or scene shutdown cannot leave the game in slow motion.
- [ ] Ensure target/jet destruction mid-cinematic uses the documented fallback and still restores time scale.
- [ ] Keep UI read-model/follow-mode status stable while the cinematic temporary target is active.
- [ ] Avoid new sync points or job completions in camera integration.
- [ ] Add tests for time-scale restoration and temporary-target ownership.

Exit criteria:

- Follow mode, cinematic mode, and normal camera mode hand off cleanly without time-scale leaks.

## Phase 6: Tests And Architecture Guardrails

- [ ] Add or update pure helper tests for phase boundaries.
- [ ] Add or update pure helper tests for time-scale ramp values.
- [ ] Add or update pure helper tests for launch shot framing.
- [ ] Add or update pure helper tests for missile-path shot framing.
- [ ] Add or update pure helper tests for impact shot framing.
- [ ] Add or update pure helper tests for flyover shot framing.
- [ ] Add ECS-system tests for followed-air-unit request capture.
- [ ] Add ECS-system tests for unfollowed attack requests being ignored.
- [ ] Add ECS-system tests for retrigger cooldown.
- [ ] Add ECS-system tests for abort/finish cleanup.
- [ ] Run compile validation for runtime, editor, and tests.
- [ ] Run architecture validation for naming, assembly boundaries, and Burst/hot-path classification.
- [ ] Run `git diff --check`.

Exit criteria:

- The implementation passes code-level validation before visual handoff.

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
