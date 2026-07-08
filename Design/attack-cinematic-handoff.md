# Handoff: Jet Attack Cinematic Camera (3rd-person follow mode)

> Current acceptance note, 2026-07-08: the corrective implementation is now the active behavior. The original 2026-07-07 handoff below remains useful historical context, but acceptance and remaining work are tracked in `Architecture/tactical_follow_attack_cinematic_improvement_tracker.md`.

## Current Implemented Behavior

The tactical follow attack cinematic now owns the followed air-unit attack sequence instead of relying on the same-frame normal attack VFX playback:

1. The followed jet attack is captured after `UnitAttackSystem` and before normal attack VFX playback.
2. Captured same-source muzzle and impact requests are copied into the cinematic timeline, then consumed so impact VFX cannot fire before the camera reaches the impact beat.
3. The sequence plays launch, missile path, impact, flyover, and return frames through the existing tactical follow camera pose singletons.
4. The flyover shot tracks the real source aircraft when it still exists, with projected path fallback only if the source is lost.
5. The existing pooled missile trail view is reused with cinematic-readable trail duration and width.

## Current Validation Evidence

- Runtime compile passed: `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`.
- Editor compile passed: `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- Editor test compile passed: `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`.
- Focused follow-camera command validation passed: `/private/tmp/warline-tactical-follow-command-validation-4.log`, `[TacticalFollowCameraModeCommandValidation] result=Passed tests=44`.
- Real Match playmode proof passed: `/private/tmp/warline-attack-cinematic-playmode-validation-escalated-12.log`, `[TacticalFollowAttackCinematicPlayModeValidation] result=Passed`.
- Real Match playmode proof with cinematic-only perf/GC sampling passed: `/private/tmp/warline-attack-cinematic-playmode-validation-perf-2.log`, `performance=Passed limitAvgMs=50.00 limitMaxMs=150.00 perfSamples=526 avgFrameMs=8.71 maxFrameMs=35.71 gcDelta=0/0/0`.
- ECS/Burst architecture guard passed: `/private/tmp/warline-attack-cinematic-architecture-validation-15.log`, `[EcsBurstHotPathArchitectureValidation] result=Passed tests=10`.
- Captured proof frames:
  - `/private/tmp/warline-attack-cinematic-playmode/01-launch.png`
  - `/private/tmp/warline-attack-cinematic-playmode/02-missile-path.png`
  - `/private/tmp/warline-attack-cinematic-playmode/03-impact.png`
  - `/private/tmp/warline-attack-cinematic-playmode/04-flyover.png`
  - `/private/tmp/warline-attack-cinematic-playmode/05-return.png`

## Intentional Managed Boundaries

- `TacticalFollowAttackCinematicSystem` remains an `ISystem` but is intentionally not Burst-compiled because it owns `UnityEngine.Time.timeScale` restoration and consumes entity requests around managed presentation timing.
- `MissileTrailVfxView` remains a pooled Unity-object presentation boundary; it does not own damage, targeting, or gameplay policy.
- Editor playmode proof helpers are validation-only and are not runtime loops.

## Performance/GC Validation

- Performance/GC validation is covered by the real Match playmode proof. The sampler excludes screenshot render/PNG capture frames and verifies the cinematic update path has no GC collection during measured non-capture frames.

## Goal
When the player follows a jet in 3rd-person mode (`ToggleFollowMode`) and it attacks, play a
satisfying multi-phase cinematic instead of the current 1.15s static cut to the impact point:

1. **Launch shot** (~1.15s, slow-motion 0.3×) — camera beside/behind the jet's wing, looking
   past the jet toward the target; you see the missile/tracer leave.
2. **Impact shot** (~1.45s, slow-mo, ramping back to 1× in the last 0.35s) — hard cut to a low
   camera near the target looking back at the explosion with the jet incoming; slow orbital drift.
3. **Flyover shot** (~1.7s, normal speed) — camera behind the wreck, panning up to track the jet
   flying over the destroyed target; camera eases toward the follow pose near the end.
4. **Return** — temporary target clears; the existing damped follow-camera transition takes over.

## Key architecture facts (already verified — do NOT re-explore)

- **Jet attacks are instant-hit.** `UnitAttackSystem` enqueues `UnitAttackVfxRequest` entities
  (`MuzzleFlash` + `Impact` **in the same frame**; damage applied same frame). There is no missile
  projectile entity for jets. `UnitAttackVfxRequestSystem` (in `UnitAttackVfxSystems.cs`) plays the
  VFX and **destroys all request entities each frame** — so the cinematic system must run
  `[UpdateAfter(typeof(UnitAttackSystem))] [UpdateBefore(typeof(UnitAttackVfxRequestSystem))]`
  (the current system already has these attributes).
- **Camera pipeline**: ECS singletons → managed apply.
  - `TacticalFollowCameraModeComponent` (singleton entity "TacticalFollowCameraMode") — follow-mode
    state incl. `HasTemporaryTarget`, `TemporaryTargetKind`, `ReturnHoldUntilTime`.
  - `TacticalFollowCameraTargetComponent` (singleton "TacticalFollowCameraTarget").
  - `TacticalFollowCameraPoseComponent` (singleton "TacticalFollowCameraPose") — desired camera
    position/rotation/lookAt/FOV/damping.
  - `TacticalFollowCameraModeSystemHelper.RefreshActiveTargetAndPose(em, context, currentTime)`
    (called every frame from managed code in `SelectionGameplayStartupSystemHelper`, line ~625)
    resolves targets and **overwrites the pose singleton** via its private `BuildPose`.
  - `SelectionGameplayStartupSystemHelper.UpdateTacticalFollowCameraPose()` (line ~655) reads the
    pose singleton and queues `QueueUpdateTacticalFollowPose`; `RtsCameraSystem.UpdateTacticalFollowPose`
    (line ~563) SmoothDamps the real camera using `pose.PositionDampingSeconds` as smoothTime.
    **If smoothTime <= 0.0001 it snaps instantly** — that's how we do hard cuts between shots.
  - Managed layer auto-resets SmoothDamp velocity when desired position jumps > 1.25u or pose
    `Source` changes (SelectionGameplayStartupSystemHelper line ~684). Vertical-jitter suppression
    only applies to `Source == BaseTarget`, so cinematic poses must use
    `Source = TacticalFollowCameraPoseSource.TemporaryMissile` (helper's `BuildPose` already does).
- **Nothing else in the codebase touches `Time.timeScale`** — the cinematic owns it, but must
  restore it on every exit path and only modify it when `Application.isPlaying`.
- Particle/VFX playback uses scaled time, so slow-mo automatically stretches the explosion —
  masking the fact that the explosion actually spawns at fire time.

## Work already completed (compiles only once the system is rewritten!)

1. **`Assets/Game/Scripts/Components/TacticalFollowCameraComponents.cs`** — added:
   - `TacticalFollowAttackCinematicPhase` enum (None/Launch/Impact/Flyover)
   - `TacticalFollowAttackCinematicStateComponent : IComponentData` with fields:
     `Active`, `LastAppliedPhase`, `ElapsedUnscaledSeconds`, `SourceEntity`, `TargetEntity`,
     `LaunchPosition`, `ImpactPosition`, `AttackDirection`, `TimeScaleApplied` (byte),
     `SavedTimeScale`, `LastEndedElapsedTime`, `HasEnded` (byte).
2. **`Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicHelper.cs`** (new, complete) —
   pure static evaluation, no world access:
   - Constants: `LaunchDurationSeconds=1.15`, `ImpactDurationSeconds=1.45`,
     `FlyoverDurationSeconds=1.7`, `TotalDurationSeconds`, `SlowMotionTimeScale=0.3`,
     `TimeScaleRampSeconds=0.35`, `RetriggerCooldownSeconds=6`.
   - `EvaluatePhase(elapsed, out phaseElapsed)`, `IsFinished(elapsed)`,
     `EvaluateTimeScale(elapsed)` (0.3 → smoothstep ramp to 1 during last 0.35s of Impact phase),
   - `ShotContext` (launchPos, impactPos, flat attackDir, jetPos, hasJet), `Shot`
     (CameraPosition/LookAt/FieldOfView/PositionDampingSeconds),
   - `EvaluateShot(phase, phaseElapsed, in context)` — all three shots implemented,
   - `BuildPose(in Shot, bool snapToShot)` → `TacticalFollowCameraPoseComponent` with
     `Source=TemporaryMissile`, damping 0 when snapping (phase-entry hard cut),
   - `BuildTarget(targetEntity, impactPosition, attackDirection)` → AttackImpact-kind target with
     `Center = impactPosition` (keeps old test assertion `Center == targetPosition` valid).

## Completion Notes

Completed on 2026-07-07 in `/Users/farhad/Projects/WarlineCapture-Clone`.

- Replaced the old 1.15s static attack-impact hold with a stateful three-phase cinematic:
  slow-motion launch shot, slow-motion impact shot with ramp back to 1x, flyover shot, then
  smooth return to the existing third-person follow pose.
- Added `TacticalFollowAttackCinematicSystem` state ownership so the system continues ticking
  after the one-frame attack VFX requests are consumed.
- Added a follow-camera helper guard so active attack cinematic poses are not overwritten by the
  normal temporary-target pose builder.
- Added helper math coverage in `TacticalFollowAttackCinematicHelperTests` and updated the
  existing follow-camera integration validation.
- Validation:
  - `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`
  - `/private/tmp/warline-attack-cinematic-helper-validation.log`:
    `[TacticalFollowAttackCinematicHelperValidation] result=Passed tests=7`
  - `/private/tmp/warline-attack-cinematic-follow-validation.log`:
    `[TacticalFollowCameraModeCommandValidation] result=Passed tests=37`

## Original Remaining Work Checklist

### A. Rewrite `Assets/Game/Scripts/Systems/TacticalFollowAttackCinematicSystem.cs`

Replace the whole body. Keep: namespace `Game.Runtime`, `partial struct ... : ISystem`, both
`UpdateAfter/UpdateBefore` attributes, NO `[BurstCompile]` (uses UnityEngine.Time/Application).
Keep the existing private statics `IsAttackCutawayRequest` and `IsFollowedAirAttackSource` and
`NormalizeFlatOrFallback` (reuse verbatim from current file).

Structure:

- `OnCreate`: queries for mode (`RequireForUpdate`), target, pose, cinematic state. Only
  `RequireForUpdate(_modeQuery)` — the system must tick every frame while a cinematic is active
  (the old `RequireForUpdate(_requestQuery)` must be removed).
- `OnUpdate`:
  - Ensure singleton state entity exists (create with `TacticalFollowAttackCinematicStateComponent`,
    name "TacticalFollowAttackCinematicState"; same lazy pattern as `EnsureTargetEntity` in the
    current file).
  - If `state.Active != 0` → `UpdateActiveCinematic`, else → `TryStartCinematic`.
- `TryStartCinematic`:
  - Read mode; bail if `mode.Enabled==0 || mode.HasBaseTarget==0` or another temporary cutaway is
    active (`mode.HasTemporaryTarget != 0 && (mode.ReturnHoldUntilTime <= 0 || now < mode.ReturnHoldUntilTime)`
    — same as old `IsTemporaryCutawayActive`, `now = (float)SystemAPI.Time.ElapsedTime`).
  - Retrigger cooldown: bail if `cin.HasEnded != 0 && now - cin.LastEndedElapsedTime < RetriggerCooldownSeconds`.
  - Scan `SystemAPI.Query<RefRO<UnitAttackVfxRequest>>()`: find a request passing
    `IsAttackCutawayRequest` + `IsFollowedAirAttackSource(em, modeEntity, mode, request.Source)`.
    Prefer MuzzleFlash for `LaunchPosition = request.PlaybackPosition`
    (fallback `SourcePosition`); if an Impact request from the same source exists in the same
    frame use its `PlaybackPosition` as `ImpactPosition`, else `request.TargetPosition`.
  - On match: fill state (`Active=1`, `ElapsedUnscaledSeconds=0`, `LastAppliedPhase=None`,
    `SourceEntity=request.Source`, `TargetEntity=request.Target`,
    `AttackDirection = NormalizeFlatOrFallback(impact - launch)`), set mode:
    `HasTemporaryTarget=1`, `TemporaryTargetKind=AttackImpact`, `TemporaryTargetEntity=request.Target`,
    `TemporaryTargetStartedTime=now`, `ReturnHoldUntilTime=0` (0 = "no expiry" while we own it).
    Apply slow-mo (see time-scale section). Write target via helper `BuildTarget`, write pose via
    `BuildPose(EvaluateShot(Launch, 0, ctx), snapToShot:true)`.
- `UpdateActiveCinematic`:
  - Abort path: if `mode.Enabled==0 || mode.HasTemporaryTarget==0 || mode.TemporaryTargetKind != AttackImpact`
    (user exited follow mode / cleared externally) → restore timescale, `Active=0`, `HasEnded=1`,
    `LastEndedElapsedTime=now`, return. Do not touch mode.
  - Advance clock in **unscaled** seconds: `elapsed += SystemAPI.Time.DeltaTime / divisor` where
    `divisor = TimeScaleApplied != 0 ? max(0.01, SavedTimeScale * EvaluateTimeScale(elapsedBefore)) : 1`.
    (When not playing / in tests the timescale is never applied, so divisor is 1 and tests can
    drive the clock via `World.SetTime`.)
  - Finish path: if `IsFinished(elapsed)` → restore timescale, clear mode temporary fields
    (`HasTemporaryTarget=0`, kind None, entity Null, `ReturnHoldUntilTime=0`), `Active=0`,
    `HasEnded=1`, `LastEndedElapsedTime=now`. The helper then rebuilds the base pose next frame →
    smooth damped return to 3rd person (managed velocity reset fires on Source change).
  - Otherwise: resolve live jet (`SourceEntity` exists && has `LocalTransform` → jetPos, hasJet;
    else hasJet=false — helper handles fallbacks). Build `ShotContext` from state.
    `phase = EvaluatePhase(elapsed, out phaseElapsed)`; `snap = phase != state.LastAppliedPhase`
    (produces the hard cut on each phase entry, single frame of damping 0);
    write target (`BuildTarget`) + pose (`BuildPose(EvaluateShot(...), snap)`) singletons;
    update `LastAppliedPhase`, apply `EvaluateTimeScale(elapsed)`.
- Time-scale management (all guarded by `UnityEngine.Application.isPlaying`):
  - Apply: on first application set `SavedTimeScale = UnityEngine.Time.timeScale`,
    `TimeScaleApplied=1`; then `UnityEngine.Time.timeScale = SavedTimeScale * EvaluateTimeScale(elapsed)`.
  - Restore: if `TimeScaleApplied != 0` → `UnityEngine.Time.timeScale = SavedTimeScale`,
    `TimeScaleApplied=0`.
  - `OnDestroy`: restore if applied (safety net).

### B. Defer in `TacticalFollowCameraModeSystemHelper` (Assets/Game/Scripts/Systems/TacticalFollowCameraModeSystemHelper.cs)

While a cinematic is active, the helper must NOT overwrite the pose the cinematic wrote:

1. Add private static:
   ```csharp
   private static bool HasActiveAttackCinematic(EntityManager em)
   {
       using EntityQuery query = em.CreateEntityQuery(
           ComponentType.ReadOnly<TacticalFollowAttackCinematicStateComponent>());
       if (query.IsEmptyIgnoreFilter)
           return false;
       return em.GetComponentData<TacticalFollowAttackCinematicStateComponent>(
           query.GetSingletonEntity()).Active != 0;
   }
   ```
2. In `RefreshActiveTargetAndPose(em, context, currentTime)` — right after the block that sets
   `mode.HasBaseTarget = 1; mode.BaseTargetKind = ...; mode.BaseTargetEntity = ...` (line ~181)
   and BEFORE `bool temporaryHoldExpired = ...`, insert:
   ```csharp
   if (mode.HasTemporaryTarget != 0 &&
       mode.TemporaryTargetKind == TacticalFollowCameraTargetKind.AttackImpact &&
       HasActiveAttackCinematic(em))
   {
       em.SetComponentData(modeEntity, mode);
       PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
       return true;
   }
   ```
3. In `TryContinueTemporaryTargetWithoutBase` (line ~964, handles jet dying mid-cinematic), insert
   the same check at the very top (before the missile branch), minus the `em.SetComponentData(modeEntity, mode)`
   line (mode unchanged there):
   ```csharp
   if (mode.HasTemporaryTarget != 0 &&
       mode.TemporaryTargetKind == TacticalFollowCameraTargetKind.AttackImpact &&
       HasActiveAttackCinematic(em))
   {
       PublishUiReadModel(em, modeEntity, TacticalCommandReasonCode.None, context);
       return true;
   }
   ```
   (This keeps the cinematic playing to completion even if the jet is destroyed; when it ends,
   the next refresh exits follow mode via the existing `ExitFollowMode` path.)

### C. Tests (`Assets/Tests/Editor/TacticalFollowCameraModeCommandSystemHelperTests.cs`)

Existing tests to update (both at ~line 1046–1123):

- `FollowedAirUnitAttackVfxCreatesImpactCutawayThenReturns`:
  - After `cinematicSystem.Update` + `RefreshActiveTargetAndPose(_em, default, 10.1f)`:
    keep asserts for `HasTemporaryTarget==1`, `TemporaryTargetKind==AttackImpact`,
    `TemporaryTargetEntity==target`, target kind AttackImpact, `Center==targetPosition`,
    pose source TemporaryMissile. CHANGE: `ReturnHoldUntilTime` is now `0` while active
    (replace `Assert.Greater(mode.ReturnHoldUntilTime, 10.1f)` with `Assert.AreEqual(0f, ...)`),
    and additionally assert the cinematic state singleton is `Active==1`.
  - For the return leg: instead of just refreshing at t=12, advance world time past the total
    duration and update the cinematic system so it finishes:
    ```csharp
    _world.SetTime(new TimeData(15d, 5f)); // dt 5 > TotalDurationSeconds (4.3)
    cinematicSystem.Update(_world.Unmanaged);
    Assert.IsTrue(_system.RefreshActiveTargetAndPose(_em, default, 15.1f));
    ```
    then keep the existing "returned to base" asserts (`HasTemporaryTarget==0`, pose source
    BaseTarget).
- `UnfollowedAirUnitAttackVfxDoesNotCreateImpactCutaway`: should pass unchanged; optionally also
  assert cinematic state is inactive/absent.

New test file `Assets/Tests/Editor/TacticalFollowAttackCinematicHelperTests.cs` (same asmdef as
the existing editor tests) — pure math tests, no world needed:
- Phase boundaries: elapsed 0 → Launch; `LaunchDurationSeconds` → Impact;
  `Launch+Impact` → Flyover; `TotalDurationSeconds` → None + `IsFinished` true.
- Timescale: 0 → 0.3; just before ramp start → 0.3; `Launch+Impact` → 1; mid-ramp strictly
  between 0.3 and 1; after total → 1.
- Launch shot anchors near jet: camera within ~15u of jet position, lookAt ahead of jet along
  attack dir (dot(lookAt - jetPos, dir) > 0), FOV 30.
- Impact shot anchors near impact: camera within ~15u of impact, lookAt near impact.
- Flyover: at phaseElapsed near end, lookAt is closer to jet position than to impact
  (pan-up tracking); with `hasJet=false` it doesn't NaN and looks toward impact/dir fallback.
- `BuildPose(snapToShot:true)` → `PositionDampingSeconds == 0`; false → > 0.
- `BuildTarget` → kind AttackImpact, `Center == impactPosition`.

### D. Verify

- Compile + run editor tests via the Unity editor (a `unity` MCP server is configured for this
  project — use its tools to compile/run edit-mode tests; load via ToolSearch, e.g. query "unity").
  Run at least the two updated tests and the new helper test fixture.
- Optional play-mode sanity: Menu scene quick-launches a match (see memory index); select a jet,
  order an attack, toggle 3rd-person follow ("3rd person camera mode" button), observe:
  slow-mo launch shot → cut to explosion → speed ramps back → flyover → smooth return.
  Check `Time.timeScale` returns to 1 after the cinematic and after exiting follow mode mid-cinematic.

## Tuning notes (if the user wants adjustments later)
- All shot framing constants live at the top of `TacticalFollowAttackCinematicHelper`.
- Damping values are in *scaled* seconds (SmoothDamp uses scaled `Time.deltaTime`), so during
  slow-mo an 0.12s damping ≈ 0.4s real-time response — intentional.
- The launch shot has a subtle push-in (`LaunchPushInScale`), the impact shot a 16° orbital
  drift, the flyover an exit blend toward the follow pose (`FlyoverExitBlend*`) to shorten the
  hand-back distance.
