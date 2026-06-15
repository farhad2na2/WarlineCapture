# UnitRenderBudgetSystem Refactor Roadmap

This document owns the `UnitRenderBudgetSystem` refactor plan. The system is hot gameplay/rendering code, so the goal is a behavior-preserving decomposition into narrow ECS `*System` boundaries without changing visual policy, LOD thresholds, scheduling cadence, allocator lifetimes, or structural-change order.

## Fixed Step Count

This roadmap has 36 steps. Do not append surprise steps after step 36. If new work is discovered, update the relevant existing step and keep the final validation gate as the last step.

## Target

Target file: `Assets/Game/Scripts/Systems/UnitRenderBudgetSystem.cs`

Current size at roadmap creation: 1388 lines. This is an observation, not a hard acceptance limit. The acceptance target is single responsibility and stable frame performance.

Final target: `UnitRenderBudgetSystem` may remain only as the ECS render-budget update tick that sequences query, schedule, budget-plan, visibility-apply, and diagnostics phases through narrow `*System` owners. It must not own camera-motion policy, unit snapshot projection, budget-band planning, character/vehicle classification, LOD readiness recursion, render-safety patching, visual-state transition rules, diagnostic formatting, or broad helper surface. If the remaining type becomes pure pass-through after these steps, step 35 decides whether it should stay as the named ECS tick or be retired. No broad replacement shell may be introduced.

## Current Responsibility Inventory

- ECS query setup: creates and owns unit, all-unit-grid, spawn config/progress/initialized, and camera-reference queries.
- Runtime schedule and stability: owns update intervals, stable unit count tracking, LOD resume timing, diagnostic cadence, and early-out decisions.
- Camera motion: stores camera position/rotation snapshots and decides whether camera motion forces a render-budget update.
- Unit snapshot and projection: allocates entity/transform/component arrays, builds `UnitDistance` values, computes distance, screen visibility, viewport-edge status, and priority.
- Budget band planning: sorts units and fills detailed, mid, low, far-impostor, visible-character, and screen-edge sets under existing budget caps.
- Classification and policy: identifies character units, vehicle units, enemy units, visible-character detail policy, and forced detailed character visuals.
- LOD readiness and safety: traverses child buffers for animation readiness, material alpha, renderable visibility, safe LOD selection, render bounds patching, and LOD group patching.
- Visual state transitions: owns `UnitRenderVisualState`, readiness tags, transition stability, exclusive display readiness, and transition budget limits.
- Visibility application: adds/removes `DisableRendering`, `UnitRenderBudgetCulledTag`, `UnitRenderBudgetCulledUnitTag`, render safety tags, render bounds, and visual state components through an `EntityCommandBuffer`.
- Diagnostics: owns render-budget diagnostic toggles, light state logs, mismatch diagnostics, diagnostic sample formatting, and freeze logs.

## Public/Internal Surface Inventory Freeze

New public/internal members must not be added to `UnitRenderBudgetSystem`. Later steps may move existing static policy methods to target owners and update tests to follow those owners.

Allowed current public/static surface:

- `public void OnCreate(ref SystemState state)`
  - Target owner: retained ECS lifecycle method while the tick remains.
- `public void OnUpdate(ref SystemState state)`
  - Target owner: retained ECS lifecycle method while the tick remains.

Retired public/static policy surface:

- `public static UnitRenderVisualKind ResolveVisibleCharacterVisualKind(...)`
  - Retired in step 13; owner is now `UnitRenderBudgetCharacterPolicySystem`.
- `public static bool ShouldForceCharacterDetailVisual(bool isCharacter)`
  - Retired in step 13; owner is now `UnitRenderBudgetCharacterPolicySystem`.

## Architecture Rules

- Do not replace `UnitRenderBudgetSystem` with `UnitRenderBudgetManager`, `UnitRenderBudgetController`, `UnitRenderBudgetFacade`, `UnitRenderBudgetOrchestrator`, or another broad shell.
- New gameplay runtime types must be named `*System`, except existing `Config` assets, `Component`/`Entity` data, and Unity edge types.
- No singleton/static runtime access. Static helpers are allowed only for pure deterministic math/data with no runtime dependencies.
- Do not use reflection.
- Do not move render-budget gameplay behavior into UI, bootstrap, editor tooling, scene views, or config assets.
- Do not hide child-system wiring behind service locators, discovery scans, or a replacement composition shell.

## Performance And Behavior Rules

- Preserve current budget caps, update intervals, diagnostic intervals, camera settle/motion thresholds, transition-stability values, max visual transitions per update, viewport padding, screen-edge safety margin, always-visible LOD mask/distance, render bounds minimum extents, and all visible-character/enemy distance thresholds unless a later gameplay task explicitly approves tuning.
- Preserve the current visible-character policy: visible character units use the detailed model path, and `ShouldForceCharacterDetailVisual(true)` remains true.
- Preserve current query membership, early-out behavior, structural-change order, `EntityCommandBuffer` playback timing, child-buffer traversal semantics, readiness-tag semantics, culling tags, and render-safety patch behavior.
- Preserve hot-path allocator lifetimes and data layout. Do not add LINQ, reflection, per-frame managed collections, per-frame delegates/closures, scene searches, runtime asset loading, or direct GameObject lookup in the render-budget tick.
- Preserve diagnostic content and gating. Hot paths must not construct diagnostic strings unless the diagnostic path is enabled.

## Required Validation Gates

Every implementation step must run:

- `git diff --check` scoped to touched files.
- Focused architecture validation once this roadmap's tests exist.

Every phase boundary must also run when feasible:

- `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation`.
- EditMode `UnitRenderBudgetSystemTests`.
- A focused runtime play-button FPS probe when a step changes update scheduling, budget planning, visual-state transition logic, render-safety patching, structural visibility apply, or diagnostics.
- Runtime visual smoke from the main Game scene when a step changes LOD readiness, visibility tags, render bounds, or impostor/detail handoff.

Use `/Users/farhad/Projects/WarlineCapture-CodexUnity1` for Unity validation.

## Phase 1: Baseline, Contract, And Surface Freeze

1. Complete: Add roadmap and baseline architecture guard
   - Add this document.
   - Add architecture contract wording that `UnitRenderBudgetSystem` is a hot mixed-responsibility ECS system that must shrink through narrow render-budget systems.
   - Add focused architecture validation entry point for this roadmap.
   - Guard the 36-step roadmap, target file, current responsibility inventory, forbidden broad replacement names, and bounded public/static policy surface.
   - Expected output: future changes cannot normalize or grow the mixed-responsibility render-budget system.

2. Complete: Freeze public/static policy surface
   - Inventory every public/static method and assign it to the final owner listed above.
   - Add or tighten a guard preventing new public/static helper surface on `UnitRenderBudgetSystem`.
   - Expected output: tests can migrate deliberately when static policy methods move.
   - Initial top-level public surface was frozen to `OnCreate`, `OnUpdate`, `ResolveVisibleCharacterVisualKind`, and `ShouldForceCharacterDetailVisual`.
   - Static visible-character policy helpers were temporary compatibility surface and targeted `UnitRenderBudgetCharacterPolicySystem`; they were retired in step 13.
   - `GameplayArchitectureContractTests.UnitRenderBudgetSystemBaselineMustStayExplicitUntilExtracted` now blocks new top-level public/internal helper methods while extraction is in progress.

3. Complete: Add deterministic behavior baseline
   - Document current `UnitRenderBudgetSystemTests` command and expected outputs.
   - Capture key runtime FPS/diagnostic scenario expectations when editor validation is stable.
   - Do not change code behavior in this step.
   - Expected output: later extraction steps have a behavior/performance comparison point.
   - Baseline EditMode command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter UnitRenderBudgetSystemTests -testResults /private/tmp/warline-unit-render-budget-baseline.xml -logFile /private/tmp/warline-unit-render-budget-baseline.log`
   - Baseline architecture command:
     `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation -logFile /private/tmp/warline-unit-render-budget-architecture.log`
   - Expected `UnitRenderBudgetSystemTests` outputs:
     - Moving visible characters use `UnitRenderVisualKind.Detail`.
     - Moving visible characters without animatable mid/low mesh LOD still fall back to `Detail`.
     - Idle distant visible characters stay on the detailed model path.
     - `ShouldForceCharacterDetailVisual(true)` is true and false for non-character units.
     - High tactical camera character scale remains `1` at camera height `80` and `16` at camera height `200`.
     - High-camera character impostors face the camera plane while vehicle impostors keep world-forward orientation.
   - Current baseline validation attempt:
     - The focused EditMode command exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, but Unity did not emit `/private/tmp/warline-unit-render-budget-baseline.xml` or a TestRunner summary in `/private/tmp/warline-unit-render-budget-baseline.log`. Do not treat that command as a confirmed pass; rerun through a stable Test Runner/CI invocation before behavior-changing render-budget steps depend on it.
   - No fresh runtime FPS probe was recorded for this doc/test-guard step. The probe remains required when later steps change scheduling, budget planning, visual handoff, render safety, structural visibility apply, or diagnostics.

## Phase 2: Query, Scheduling, And Camera Motion

4. Complete: Extract ECS query ownership
   - Create `UnitRenderBudgetQuerySystem`.
   - Move query creation and query membership definitions out of `UnitRenderBudgetSystem`.
   - Preserve `RequireForUpdate<RuntimeGameplayStateComponent>()`.
   - Expected output: render-budget query shape has one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetQuerySystem.cs`.
   - Moved unit, all-unit-grid, spawn config/progress/initialized, and camera-reference query creation into `UnitRenderBudgetQuerySystem.Context`.
   - `UnitRenderBudgetSystem` now stores the query context and no longer owns direct `EntityQuery` fields.
   - `RequireForUpdate<RuntimeGameplayStateComponent>()`, query membership, and diagnostic query counts were preserved.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

5. Complete: Extract runtime schedule state
   - Create `UnitRenderBudgetScheduleSystem`.
   - Move update interval, diagnostic interval, LOD resume frame, stable unit count, and early-out decisions.
   - Preserve `UpdateIntervalFrames`, `DiagnosticIntervalFrames`, and camera-motion forced update behavior.
   - Expected output: update cadence is isolated and testable.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetScheduleSystem.cs`.
   - Moved `_nextUpdateFrame`, `_nextDiagnosticFrame`, `_lodResumeFrame`, `_budgetStable`, and `_stableUnitCount` into `UnitRenderBudgetScheduleSystem`.
   - `UnitRenderBudgetSystem.OnUpdate` now delegates stable-budget early-out, update-frame cadence, diagnostic cadence, and final stability recording to the schedule system.
   - Camera-motion detection still owns camera snapshots until step 6, but its LOD resume and diagnostic-reset side effects now route through `UnitRenderBudgetScheduleSystem.MarkCameraMotion`.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

6. Complete: Extract camera motion policy
   - Create `UnitRenderBudgetCameraMotionSystem`.
   - Move camera snapshot state and motion thresholds.
   - Preserve `CameraSettleFrames`, `CameraMoveThresholdSq`, and `CameraRotateThresholdDegrees`.
   - Expected output: camera motion no longer lives in the render-budget tick body.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetCameraMotionSystem.cs`.
   - Moved camera snapshot state, position/rotation comparison, settle-frame handling, and motion thresholds into the camera motion system.
   - `UnitRenderBudgetSystem.OnUpdate` now calls `_cameraMotionSystem.IsCameraMotionActive(camera, ref _scheduleSystem, Time.frameCount)`.
   - The camera motion system still drives schedule reset through `UnitRenderBudgetScheduleSystem.MarkCameraMotion`, preserving the previous LOD resume and diagnostic reset behavior.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

7. Complete: Route `OnUpdate` through query/schedule/camera systems
   - Update the tick to delegate query, schedule, and camera decisions.
   - Preserve early return order and no-camera behavior.
   - Expected output: no behavior change, smaller update preamble.
   - `OnCreate` delegates query construction to `UnitRenderBudgetQuerySystem`.
   - `OnUpdate` preserves the same order: play-request guard, camera-reference guard, camera-motion evaluation, stable-budget early-out, update-cadence early-out, then render-budget work.
   - Stable-budget, update-frame, diagnostic-frame, and camera-settle state now route through `UnitRenderBudgetScheduleSystem`.
   - Camera snapshot/motion policy now routes through `UnitRenderBudgetCameraMotionSystem`.
   - Validation:
     - Covered by the step 6 architecture validation run because both steps were implemented in the same tick-preamble slice.

## Phase 3: Snapshot, Distance, And Budget Planning

8. Complete: Extract unit snapshot collection
   - Create `UnitRenderBudgetSnapshotSystem`.
   - Move entity, transform, faction, grid, movement behavior, and source-prefab snapshot collection.
   - Preserve allocator lifetimes and disposal order.
   - Expected output: snapshot arrays have one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetSnapshotSystem.cs`.
   - Moved the current `UnitQuery.ToEntityArray(Allocator.Temp)` and `UnitQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp)` snapshot collection behind `UnitRenderBudgetSnapshotSystem.Create`.
   - `UnitRenderBudgetSystem` now consumes `UnitRenderBudgetSnapshotSystem.Snapshot` and no longer calls query snapshot-array APIs directly.
   - Disposal order is preserved inside `Snapshot.Dispose`: transforms dispose before units, matching the previous using-declaration teardown order.
   - Faction, grid, movement behavior, and source-prefab values remain queried from `EntityManager`/lookups until the later distance, classification, and policy extraction steps; no new arrays were introduced in this step to avoid changing hot-path data layout.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

9. Complete: Extract distance and viewport projection
   - Create `UnitRenderBudgetDistanceSystem`.
   - Move distance, screen visibility, viewport-edge, and priority projection.
   - Preserve viewport padding and screen-edge safety margin values.
   - Expected output: projection math is separate from budget application.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetDistanceSystem.cs`.
   - Moved unit distance, camera viewport, screen-edge, and priority projection into `UnitRenderBudgetDistanceSystem.Collect`.
   - `UnitRenderBudgetSystem` now consumes `UnitRenderBudgetDistanceSystem.UnitDistance` through a type alias and no longer owns direct viewport projection logic.
   - `AlwaysDetailedDistanceSq`, `VisibleCharacterViewportPadding`, and `VisibleCharacterEdgeSafetyMargin` are still sourced from the existing render-budget constants and passed into the distance system to avoid tuning changes.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

10. Complete: Extract sort and priority policy
   - Create `UnitRenderBudgetSortSystem`.
   - Move `UnitDistance` comparison and priority ordering.
   - Preserve stable distance/priority ordering behavior.
   - Expected output: budget planning can reuse sorted projections.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetSortSystem.cs`.
   - Moved `UnitDistanceComparer` and the `distances.AsArray().Sort(...)` call behind `UnitRenderBudgetSortSystem.Sort`.
   - Priority ordering remains `Priority` first, then `DistanceSq`, matching the previous comparer.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

11. Complete: Extract budget band planning
   - Create `UnitRenderBudgetBandSystem`.
   - Move detailed, mid, low, far-impostor, visible-character, and edge-near set construction.
   - Preserve `MaxDetailedUnits`, `MaxMidLodUnits`, `MaxLowLodUnits`, `MaxUpdatesPerFrame`, and all distance thresholds.
   - Expected output: band planning has one owner and the tick only consumes a plan.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetBandSystem.cs`.
   - Moved current detailed, mid, and low LOD set construction into `UnitRenderBudgetBandSystem.Create`.
   - Preserved the existing two-pass detailed selection order, then mid selection, then low selection.
   - `UnitRenderBudgetSystem` still owns the later far-impostor and visible-character decisions until the visual planning steps; this step did not change those thresholds or decisions.
   - `MaxDetailedUnits`, `MaxMidLodUnits`, `MaxLowLodUnits`, and `AlwaysDetailedDistanceSq` are still sourced from the existing constants and passed into the band system to avoid tuning changes.
   - Band `NativeHashSet` disposal order is preserved in `Plan.Dispose`: low, mid, then detailed.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

12. Complete: Extract character and vehicle classification
   - Create `UnitRenderBudgetClassificationSystem`.
   - Move character/vehicle/enemy classification and source-name rules.
   - Preserve `UnitMovementBehavior.UsesVehicleMotion` and `Unit_Chr_` source-name behavior.
   - Expected output: classification logic is reusable without pulling in the full tick.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetClassificationSystem.cs`.
   - Moved character classification out of `UnitRenderBudgetSystem`.
   - Preserved the current behavior: units with `UnitMovementBehavior.UsesVehicleMotion != 0` are not characters, and remaining candidates must have a `UnitSourcePrefabKey` beginning with `Unit_Chr_`.
   - Existing enemy classification is centralized through `FactionIdentity` so neutral faction `0` remains non-commandable and non-player.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

13. Complete: Extract visible-character policy
   - Create `UnitRenderBudgetCharacterPolicySystem`.
   - Move visible-character detailed path policy and high-camera impostor scale/rotation helpers.
   - Preserve current tests and static behavior while migrating call sites.
   - Expected output: public static policy surface leaves the mixed system.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetCharacterPolicySystem.cs`.
   - Moved visible-character detailed-path policy and forced character-detail policy out of `UnitRenderBudgetSystem`.
   - `UnitRenderBudgetSystem` no longer exposes `ResolveVisibleCharacterVisualKind` or `ShouldForceCharacterDetailVisual` as public static helpers.
   - The policy still returns `UnitRenderVisualKind.Detail` for visible characters and `isCharacter` for the force-detail check, preserving current behavior.
   - High-camera impostor scale/rotation helpers already live in `UnitImpostorRenderSystem`, so no behavior moved for those helpers in this step.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1` after syncing `UnitImpostorRenderSystem` into the validation clone because its public test helpers were present in the main workspace but missing there.

14. Complete: Migrate render-budget policy tests
   - Update `UnitRenderBudgetSystemTests` to target `UnitRenderBudgetCharacterPolicySystem`.
   - Keep the same expected values and edge cases.
   - Expected output: tests no longer instantiate or depend on policy helpers from the mixed tick system.
   - `Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs` now exercises `UnitRenderBudgetCharacterPolicySystem`.
   - Existing expected values and edge cases are unchanged.
   - Architecture guard now allows only `OnCreate` and `OnUpdate` as public top-level surface on `UnitRenderBudgetSystem`.
   - Validation:
     - Focused `UnitRenderBudgetSystemTests` batch command exited `0` in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`, but Unity again did not emit `/private/tmp/warline-unit-render-budget-step14-tests.xml` or a TestRunner summary. Do not treat that as a confirmed test pass; rerun through a stable Test Runner/CI invocation before relying on it as a behavioral gate.

## Phase 4: LOD Readiness, Visual State, And Safety

15. Complete: Extract LOD reference resolution
   - Create `UnitRenderBudgetLodReferenceSystem`.
   - Move visual-root, LOD child, group, and mesh LOD reference resolution.
   - Preserve child-buffer traversal order.
   - Expected output: LOD lookup is isolated.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetLodReferenceSystem.cs`.
   - Moved unit detail root, mid/low LOD prefab, mid/low LOD instance, mesh LOD, and mesh LOD group lookups behind `UnitRenderBudgetLodReferenceSystem`.
   - `UnitRenderBudgetSystem` now consumes `UnitRenderBudgetLodReferenceSystem.UnitReferences` in the main decision loop and diagnostics instead of directly resolving visual reference components.
   - Render safety patching now uses `TryResolveMeshLod` and `TryResolveMeshLodGroup`; actual safety ownership still moves in step 20.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct unit LOD visual-reference component lookups.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

16. Complete: Extract animation readiness checks
   - Create `UnitRenderBudgetAnimationReadinessSystem`.
   - Move animation-index, material-alpha, and animated render readiness recursion.
   - Preserve fallback-to-detail behavior.
   - Expected output: animation readiness can be tested independently.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetAnimationReadinessSystem.cs`.
   - Moved recursive `MaterialAnimationIndex` lookup, material-alpha completion lookup, and animated visual readiness recursion into `UnitRenderBudgetAnimationReadinessSystem`.
   - `UnitRenderBudgetSystem` now delegates animatable mid/low checks, visual handoff readiness checks, and diagnostic alpha-readiness sampling to the animation readiness system.
   - `UnitRenderVisualReadyTag` ownership intentionally remains in `UnitRenderBudgetSystem` until step 19.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct animation/material readiness recursion.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

17. Complete: Extract renderable query predicates
   - Create `UnitRenderBudgetRenderableQuerySystem`.
   - Move renderable visibility, safe LOD, and renderable entity checks.
   - Preserve disabled/culling tag semantics.
   - Expected output: recursive renderability rules have one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetRenderableQuerySystem.cs`.
   - Moved recursive renderable visibility checks, recursive renderable-existence checks, safe visible-character LOD checks, and renderable entity predicates into `UnitRenderBudgetRenderableQuerySystem`.
   - `UnitRenderBudgetAnimationReadinessSystem` now asks the renderable query system for renderable entity checks, avoiding duplicate renderability rules.
   - `UnitRenderBudgetSystem` delegates renderability and safe-LOD checks through `UnitRenderBudgetRenderableQuerySystem` in the decision loop and diagnostics.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct renderable query predicates.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

18. Complete: Extract visual state transition policy
   - Create `UnitRenderBudgetVisualStateSystem`.
   - Move stable visual state resolution, transition frame checks, transition budget, and `UnitRenderVisualState` writes.
   - Preserve `VisualTransitionStableFrames` and `MaxVisualStateTransitionsPerUpdate`.
   - Expected output: transition rules are separate from structural apply.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetVisualStateSystem.cs`.
   - Moved stable visual state resolution, transition frame checks, transition budget gating, and `UnitRenderVisualState` add/set writes into `UnitRenderBudgetVisualStateSystem`.
   - Moved `VisualTransitionStableFrames = 2` and `MaxVisualStateTransitionsPerUpdate = 32` into the visual state system without tuning changes.
   - `UnitRenderBudgetSystem` now delegates visual state resolution through `_visualStateSystem` while retaining the existing counters for diagnostics and stability checks.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct visual-state transition policy.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

19. Complete: Extract visual readiness tagging
   - Create `UnitRenderBudgetReadinessSystem`.
   - Move `UnitRenderVisualReadyTag` decisions and exclusive display readiness checks.
   - Preserve readiness tag add/remove behavior.
   - Expected output: readiness semantics have one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetReadinessSystem.cs`.
   - Moved exclusive-display readiness wrappers, `UnitRenderVisualReadyTag` checks, same-frame ready tag cache checks, and `UnitRenderVisualReadyTag` ECB adds into `UnitRenderBudgetReadinessSystem`.
   - The readiness system delegates animated render readiness to `UnitRenderBudgetAnimationReadinessSystem` and renderable predicates to `UnitRenderBudgetRenderableQuerySystem`.
   - `UnitRenderBudgetSystem` now delegates visual handoff readiness in the decision loop and diagnostics.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct visual readiness tag policy.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

20. Complete: Extract render safety patching
   - Create `UnitRenderBudgetRenderSafetySystem`.
   - Move LOD group patching, render bounds min-extents patching, and safety tag application.
   - Preserve `RenderBoundsMinExtents`, `AlwaysVisibleLodMask`, and `AlwaysVisibleLodDistance`.
   - Expected output: safety patching no longer lives in the tick.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetRenderSafetySystem.cs`.
   - Moved render bounds min-extents patching, Mesh LOD mask patching, Mesh LOD group distance/mask patching, recursive child traversal, same-frame safety cache checks, and `UnitRenderSafetyPatchedTag` ECB adds into `UnitRenderBudgetRenderSafetySystem`.
   - Moved `RenderBoundsMinExtents`, `AlwaysVisibleLodMask`, and `AlwaysVisibleLodDistance` into the render safety system without tuning changes.
   - `UnitRenderBudgetSystem` now delegates visible detail/mid/low safety patching through `_renderSafetySystem`.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct render safety patching.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

## Phase 5: Decision, Visibility Apply, And Impostor Tags

21. Complete: Extract desired visual planning
   - Create `UnitRenderBudgetVisualPlanSystem`.
   - Move desired visual kind resolution from budget bands plus classification/readiness.
   - Preserve character-detail and enemy-impostor decisions.
   - Expected output: per-unit desired visual state is produced before apply.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetVisualPlanSystem.cs`.
   - Moved desired detail/mid/low/far visual planning, protected visible-character policy application, enemy impostor override, near-visible character fallback, detail-until-ready fallback, handoff readiness fallback, and forced character-detail policy into `UnitRenderBudgetVisualPlanSystem`.
   - Moved desired visual helper methods, including diagnostics fallback resolution, out of `UnitRenderBudgetSystem`.
   - The visual plan system returns the same counter increments that feed existing freeze/render-budget diagnostics.
   - `UnitRenderBudgetSystem` now builds a per-unit visual-plan request and consumes the returned desired visual before visual-state stabilization.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct desired visual planning.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

22. Complete: Extract per-unit decision loop
   - Create `UnitRenderBudgetDecisionSystem`.
   - Move the loop that combines snapshots, bands, readiness, and transition state into apply requests.
   - Preserve processing order and `MaxUpdatesPerFrame`.
   - Expected output: tick no longer owns per-unit decision branching.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetDecisionSystem.cs`.
   - Moved the sorted-distance per-unit loop into `UnitRenderBudgetDecisionSystem.Process`, including classification, LOD reference reads, visual plan creation, visual-state stabilization, render-safety requests, visibility-change request gathering, and far-impostor show/hide request gathering.
   - `UnitRenderBudgetSystem.OnUpdate` now builds a decision context, consumes returned counters, and preserves the existing structural apply loops and ECB playback order.
   - Visibility-change recursion is temporarily owned by the decision system until step 23 extracts it to `UnitRenderBudgetVisibilityChangeSystem`.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct per-unit render decision branching.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

23. Complete: Extract visibility-change collection
   - Create `UnitRenderBudgetVisibilityChangeSystem`.
   - Move recursive show/hide change collection for visual roots and children.
   - Preserve child-buffer traversal, `DisableRendering`, and `UnitRenderBudgetCulledTag` semantics.
   - Expected output: visibility mutation requests are gathered by one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetVisibilityChangeSystem.cs`.
   - Moved recursive show/hide request collection out of `UnitRenderBudgetDecisionSystem` into `UnitRenderBudgetVisibilityChangeSystem`.
   - Preserved child-buffer traversal and the same `Disabled`, `DisableRendering`, and `UnitRenderBudgetCulledTag` request semantics.
   - `UnitRenderBudgetDecisionSystem` now delegates visibility-change request collection while continuing to own the per-unit decision order until later apply-policy extraction steps.
   - Architecture guard now prevents the decision system from regaining recursive visibility-change collection.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

24. Complete: Extract far-impostor tag policy
   - Create `UnitRenderBudgetImpostorTagSystem`.
   - Move `UnitRenderBudgetCulledUnitTag` add/remove decisions.
   - Preserve far-impostor thresholds and tag timing.
   - Expected output: unit-level impostor culling tags are not mixed with visibility recursion.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetImpostorTagSystem.cs`.
   - Moved unit-level far-impostor add/remove request decisions out of `UnitRenderBudgetDecisionSystem` into `UnitRenderBudgetImpostorTagSystem`.
   - Preserved the existing timing: the decision system still gathers requests during the per-unit pass, and `UnitRenderBudgetSystem.OnUpdate` still applies `UnitRenderBudgetCulledUnitTag` after decisions and before child visibility structural apply.
   - Architecture guard now prevents the decision system from regaining direct `UnitRenderBudgetCulledUnitTag` request decisions.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

25. Complete: Extract structural visibility apply
   - Create `UnitRenderBudgetVisibilityApplySystem`.
   - Move `EntityCommandBuffer` structural add/remove calls for visibility, readiness, visual state, safety patching, and bounds patching.
   - Preserve ECB lifetime and playback order.
   - Expected output: structural mutation has one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetVisibilityApplySystem.cs`.
   - Moved the post-decision structural apply loops for unit far-impostor tags, child visibility tags, and `DisableRendering` into `UnitRenderBudgetVisibilityApplySystem.Apply`.
   - Moved `renderStateEcb.Playback(em)` and dispose into the apply system for the normal non-empty update path, preserving the previous order after far-impostor tag updates and child visibility tag updates.
   - Existing readiness, visual-state, render-safety, and bounds writes are still generated as ECB requests by their narrow systems and are played back through this apply owner; a later route-through-plan/apply step can further reduce the tick body without changing request generation.
   - Architecture guard now prevents `UnitRenderBudgetSystem` from regaining direct structural visibility apply.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

26. Complete: Route `OnUpdate` through plan/apply systems
   - Reduce the tick body to snapshot, plan, apply, and diagnostic phase calls.
   - Preserve runtime order and no new managed allocations.
   - Expected output: `UnitRenderBudgetSystem` becomes a narrow coordinator.
   - The tick now routes query/schedule/camera, snapshot, distance/sort/band planning, per-unit decision processing, and visibility apply through narrow render-budget systems.
   - `UnitRenderBudgetSystem.OnUpdate` no longer owns the per-unit decision loop, recursive visibility-change collection, far-impostor request policy, or post-decision structural apply loops.
   - Added architecture guard coverage that requires routing through `UnitRenderBudgetDecisionSystem.Process` and `UnitRenderBudgetVisibilityApplySystem.Apply`, and blocks the extracted apply loops from returning to the tick.
   - Diagnostics still remain in the tick until phase 6 extracts diagnostic state/light/mismatch/freeze responsibilities.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

## Phase 6: Diagnostics, Performance, And Ownership Decision

27. Complete: Extract diagnostic state and counters
   - Create `UnitRenderBudgetDiagnosticStateSystem`.
   - Move counters, sample gates, and diagnostic frame tracking.
   - Preserve disabled-by-default behavior.
   - Expected output: diagnostics state is separate from render decisions.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetDiagnosticStateSystem.cs`.
   - Moved the disabled-by-default render-budget diagnostic gate, `DiagnosticIntervalFrames = 120`, diagnostic sample length gate, diagnostic frame tracking, and frame counter projection into `UnitRenderBudgetDiagnosticStateSystem`.
   - Removed diagnostic frame tracking from `UnitRenderBudgetScheduleSystem`; camera motion now resets the diagnostic frame gate through `UnitRenderBudgetDiagnosticStateSystem.ResetDiagnosticFrame()` at the same moved/rotated camera point as before.
   - `UnitRenderBudgetSystem.OnUpdate` now consumes `FrameCounters` from diagnostic state instead of owning the extracted per-frame counter locals.
   - Freeze logging remains in the tick until step 30 extracts freeze diagnostics.
   - Architecture guard now prevents diagnostic enable state and diagnostic frame tracking from returning to the tick or schedule system.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

28. Complete: Extract light diagnostics
   - Create `UnitRenderBudgetLightDiagnosticSystem`.
   - Move light render-budget state logs.
   - Preserve message content and gating.
   - Expected output: light logs have one owner.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetLightDiagnosticSystem.cs`.
   - Moved `LogRenderBudgetStateLight` out of `UnitRenderBudgetSystem` into the light diagnostic system.
   - Preserved the light diagnostic log message content, including `[UnitRenderBudgetState]`, `light=1`, camera-motion flag, visible-character counters, and detailed cap.
   - The disabled-by-default diagnostic gate remains in `UnitRenderBudgetDiagnosticStateSystem`; the tick only routes to the light diagnostic system when that gate opens.
   - Architecture guard now prevents the light diagnostic method and `light=1` message path from returning to `UnitRenderBudgetSystem`.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

29. Complete: Extract mismatch diagnostics
   - Create `UnitRenderBudgetMismatchDiagnosticSystem`.
   - Move LOD mismatch and detail/impostor mismatch diagnostics.
   - Preserve diagnostic sample content.
   - Expected output: mismatch logs do not live in the hot tick body.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetMismatchDiagnosticSystem.cs`.
   - Moved `LogMidLodDiagnostics`, diagnostic sample formatting, and visual-root diagnostic descriptions out of `UnitRenderBudgetSystem`.
   - Preserved `[UnitRenderVisibilityDiag]`, the mismatch-free `[UnitRenderBudgetState]` message, sample formatting, visible-character counters, and impostor-band fields.
   - The mismatch diagnostic system uses `UnitRenderBudgetDiagnosticStateSystem.ShouldAppendDiagnosticSample` for the sample length gate introduced in step 27.
   - Architecture guard now prevents mismatch diagnostic methods and warning-message content from returning to `UnitRenderBudgetSystem`.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

30. Complete: Extract freeze diagnostics
   - Create `UnitRenderBudgetFreezeDiagnosticSystem`.
   - Move freeze timing and threshold log emission.
   - Preserve `FreezeLogThresholdSeconds` and disabled-by-default behavior.
   - Expected output: freeze logging is isolated.
   - Added `Assets/Game/Scripts/Systems/UnitRenderBudgetFreezeDiagnosticSystem.cs`.
   - Moved the disabled-by-default freeze log gate, `FreezeLogThresholdSeconds = 0.05d`, elapsed threshold check, and `[FreezeDetect:ECS] UnitRenderBudgetSystem` message emission out of the tick.
   - Preserved freeze diagnostic message content and counter values through `UnitRenderBudgetDiagnosticStateSystem.FrameCounters`.
   - `UnitRenderBudgetSystem.OnUpdate` now only routes elapsed time, distances, detailed count, camera-motion state, counters, and visible-character threshold values into the freeze diagnostic system.
   - Architecture guard now prevents freeze diagnostic gate, threshold, and log content from returning to `UnitRenderBudgetSystem`.
   - Validation:
     - `git diff --check` passed for touched render-budget files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

31. Complete: Migrate direct diagnostics to ECS logging boundary where applicable
   - Route render-budget diagnostics through existing ECS diagnostics/logging patterns where feasible.
   - Preserve gated string construction.
   - Expected output: hot gameplay diagnostics follow the architecture contract.
   - Added `UnitRenderBudgetDiagnosticLogComponents`, `UnitRenderBudgetDiagnosticLogSystem`, and `UnitRenderBudgetDiagnosticLogFlushSystem`.
   - Moved render-budget diagnostic emission to an ECS diagnostic log buffer; `UnitRenderBudgetSystem`, light diagnostics, mismatch diagnostics, and freeze diagnostics no longer call `Debug.Log*` directly.
   - Preserved disabled diagnostic gates before string construction; the shell-edge flush system is now the only direct Unity logger for render-budget diagnostics.
   - Validation:
     - `git diff --check` passed for touched render-budget files and docs.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

32. Complete: Add focused budget and transition tests
   - Add or update EditMode tests for budget caps, visible-character detail policy, enemy thresholds, transition stability, and readiness fallback.
   - Avoid tests that require the full Game scene unless they are runtime smoke validations.
   - Expected output: extracted policy systems have deterministic coverage.
   - Added focused `UnitRenderBudgetSystemTests` coverage for detailed/mid/low budget caps, enemy impostor threshold selection, missing-LOD readiness fallback, and visual-state transition stability.
   - Kept visible-character detail policy coverage on `UnitRenderBudgetCharacterPolicySystem`.
   - Added architecture validation guards so the focused test names remain present while the refactor continues.
   - Validation:
     - `git diff --check` passed for touched render-budget test files and roadmap.
     - `UnitRenderBudgetSystemTests.RunFocusedValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

33. Complete: Add structural visibility apply tests
   - Add focused tests for tag add/remove behavior, readiness tag behavior, and render-safety patch requests.
   - Preserve component semantics.
   - Expected output: visibility apply can be refactored without visual regressions.
   - Added focused `UnitRenderBudgetSystemTests` coverage for structural show/hide tag mutation, far-impostor unit tags, ready-tag enqueue/playback behavior, render-bounds safety patching, and safety-tag replay suppression.
   - Added architecture validation guards so the structural visibility test names remain present.
   - Validation:
     - `git diff --check` passed for touched render-budget test files and roadmap.
     - `UnitRenderBudgetSystemTests.RunFocusedValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

34. Complete: Performance and allocation audit
   - Audit hot-path allocations, Native container lifetimes, structural-change counts, and diagnostics string construction.
   - Record runtime FPS probe expectations in this roadmap.
   - Expected output: refactor proves it did not trade architecture for frame drops.
   - Static audit:
     - LOD caps and cadence remain unchanged: detailed `12`, mid `36`, low `48`, max updates `4096`, update interval `10`.
     - Hot tick Native containers remain `Allocator.Temp` and same-frame disposed: snapshot arrays, safety/ready hash sets, distance and visibility request lists, and budget-band sets.
     - Structural visibility mutations remain centralized in `UnitRenderBudgetVisibilityApplySystem`; readiness and render-safety ECB tags remain in their narrow systems.
     - Render-budget diagnostic queue creation is lazy, so the diagnostic flush system has no queue to process while diagnostics remain disabled.
     - Diagnostic string formatting is limited to disabled-gated diagnostic paths and ECS diagnostic enqueue helpers; direct Unity logging remains isolated to `UnitRenderBudgetDiagnosticLogFlushSystem`.
     - Static scan found no LINQ projections/filters, scene searches, `Resources.Load`, reflection, or `Allocator.Persistent` in `UnitRenderBudget*.cs`.
   - Runtime FPS probe expectation:
     - In the main Game scene after warmup with diagnostics disabled, render-budget refactor should not add managed GC allocation or change the pre-refactor 60 FPS target behavior.
     - When diagnostics are enabled for validation, log messages may allocate strings only after the diagnostic/freeze gate opens; this is excluded from normal gameplay FPS probes.
   - Added architecture validation guard for render-budget hot-path risk markers.
   - Validation:
     - `git diff --check` passed for touched render-budget files and docs.
     - `UnitRenderBudgetSystemTests.RunFocusedValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

35. Complete: Final tick ownership decision
   - Decide whether `UnitRenderBudgetSystem` remains as the named ECS render-budget tick or is retired after all responsibilities moved.
   - If it remains, enforce that it exposes only lifecycle methods and delegates to narrow systems.
   - If it is retired, delete the file and update all references.
   - Expected output: no broad facade remains.
   - Decision: keep `UnitRenderBudgetSystem` as the named ECS render-budget tick.
   - Reason: it still owns the hot update boundary, runtime/camera/schedule gates, Native container lifetimes, and sequencing of query, distance/sort/band, decision, structural apply, and diagnostics phases. The extracted systems own the actual policies, formatting, readiness, render safety, and visibility mutations.
   - Architecture guards now enforce no broad render-budget `Manager`, `Controller`, `Facade`, or `Orchestrator`, no public/internal helper surface beyond `OnCreate`/`OnUpdate`, no direct diagnostic logging in hot diagnostic owners, and no hot-path LINQ/scene-search/reflection markers.
   - Validation:
     - `git diff --check` passed for touched render-budget architecture files.
     - `GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation` passed in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`.

36. Complete: Validation gate
   - Run architecture validation, `UnitRenderBudgetSystemTests`, focused visibility/render tests, runtime visual smoke, and FPS probe when feasible.
   - Update this roadmap with exact validation commands and results.
   - Expected output: compile-clean, behavior-preserving, and no architecture allowlist debt remains.
   - Validation:
     - `git diff --check -- Assets/Game/Scripts/Systems/UnitRenderBudgetSystem.cs Assets/Game/Scripts/Systems/UnitRenderBudget*.cs Assets/Game/Scripts/Components/UnitRenderBudget*.cs Assets/Tests/Editor/UnitRenderBudgetSystemTests.cs Assets/Tests/Editor/GameplayArchitectureContractTests.cs Design/Architecture/unit_render_budget_system_refactor_roadmap.md Design/Architecture/gameplay_solid_ecs_contract.md Design/Architecture/performance_regression_contract.md` passed.
     - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod UnitRenderBudgetSystemTests.RunFocusedValidation -logFile /private/tmp/warline-unit-render-budget-step36-focused.log` passed with `[UnitRenderBudgetFocusedValidation] result=Passed tests=13`.
     - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod GameplayArchitectureContractTests.RunUnitRenderBudgetArchitectureBatchValidation -logFile /private/tmp/warline-unit-render-budget-step36-architecture-final.log` passed with `[UnitRenderBudgetArchitectureValidation] result=Passed methods=3`.
     - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod RuntimeFpsPlayButtonProbe.Run -logFile /private/tmp/warline-unit-render-budget-step36-fps.log` completed the Game play-button runtime smoke and FPS probe.
     - FPS probe report `/private/tmp/warlinecapture-runtime-fps-probe.json`: `result=completed`, `clickedGameButton=true`, `requestFallbackUsed=false`, `sampleCount=14025`, `avgFps=314.49`, `minFps=2.80`, `maxFps=342.73`, `frameRateDiagCount=1`.
     - The runtime probe captured one Unity editor QuickSearch startup indexing `ArgumentOutOfRangeException` from `UnityEditor.Search.SearchDatabase`, plus existing startup hitches from BuildingPlacement/RuntimeCity warmup. No `UnitRenderBudget` exception, warning, or diagnostic failure was captured.
   - Result: all 36 planned steps are complete; the render-budget architecture contract, focused policy/visibility/render-safety tests, and runtime play-button smoke pass without render-budget regressions or temporary architecture allowlist debt.
