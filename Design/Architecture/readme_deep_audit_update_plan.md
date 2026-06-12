# Root README Deep Audit Update Plan

Date: 2026-06-12

## Purpose

Update the root `README.md` so it is a reliable project entry point again. The current README mixes current architecture guidance with stale scene paths, stale project progress, stale M01 production locks, old visual-lock workflow assumptions, and roadmap names that no longer match the current architecture contract.

This plan does not update the README directly. It identifies every area that should be replaced and the source of truth to use for the replacement.

## Current Evidence

- Current root README: `README.md`
- Current scene files:
  - `Assets/Game/Scenes/Menu.unity`
  - `Assets/Game/Scenes/Match.unity`
  - `Assets/Game/Scenes/Match/MatchSubScene.unity`
- Current bootstrap split: `Design/Architecture/menu_match_bootstrap_split_roadmap.md`
- Current architecture contract: `Design/Architecture/gameplay_solid_ecs_contract.md`
- Current Burst roadmap: `Design/Architecture/ecs_burst_hot_path_refactor_roadmap.md`
- Current GC plan: `Design/GC_Allocation_Elimination_Plan.md`
- Current performance contract: `Design/Architecture/performance_regression_contract.md`
- Current visual-layer workflows:
  - `Design/UIUX_Target_To_Canvas_Workflow_Guide.md`
  - `Design/UI_Screen_Reference_To_Icons_Panels_GreenKey_Workflow.md`
  - `Design/VisualLockLayered/WORKFLOW_V15_3D_GREENSCREEN.md`
- Current lane task state:
  - `Design/AgentTasks/M01_CRITICAL_PATH.md`
  - `Design/AgentTasks/*_current.md`
- Current project progress source and generated dashboard:
  - `Design/Project_State_Source.json`
  - `Design/Project_State_Dashboard.md`
- Current progress image:
  - `Design/Progress_Tracker.png`

## Critical Findings

### 1. Project Setup Is Stale

Current README says:
- main scene is `Assets/Game/Scenes/Game.unity`
- main subscene is `Assets/Game/Scenes/Game/GameSubScene.unity`

Current repo reality:
- persistent app/menu scene is `Assets/Game/Scenes/Menu.unity`
- match scene is `Assets/Game/Scenes/Match.unity`
- match subscene is `Assets/Game/Scenes/Match/MatchSubScene.unity`

Replacement:
- Update the project setup block to name `Menu.unity`, `Match.unity`, and `Match/MatchSubScene.unity`.
- Keep `Assets/Game/Scripts` and `Design` as current roots.
- Optionally list demo scenes separately as non-primary scenes.

### 2. Project Progress Snapshot And Progress Image Are Stale

Current README embeds `Design/Progress_Tracker.png` and says:
- latest PM review is `2026-05-21`
- overall estimate is `33%`
- forecast is `2027-03-31`
- Gate 4 QA/HCI is blocked

Current repo reality:
- `Design/Project_State_Source.json` still has `lastUpdated: 2026-05-21`.
- `Design/Project_State_Dashboard.md` is generated from that stale date.
- `Design/Progress_Tracker.png` has an older timestamp than the dashboard source and is not produced by `Tools/ProjectState/generate_project_state_dashboard.py`.
- Current lane files say the previous M01 critical path is held for a 3D fresh-start reset, so the README must not present the old progress image as current.

Replacement:
- Replace the detailed progress snapshot with a status-health note:
  - the dashboard is generated from `Design/Project_State_Source.json`;
  - the source currently needs a PM refresh before the root README should quote percent complete or forecasts;
  - current actionable status comes from `Design/AgentTasks/*_current.md` plus recent `Design/AgentReports`.
- Remove the embedded `Design/Progress_Tracker.png` from the root README unless a maintained generator is added.
- Add a follow-up task to either:
  - update `Project_State_Source.json` and regenerate `Project_State_Dashboard.md`; or
  - create a maintained progress-image generator and regenerate `Design/Progress_Tracker.png`.

### 3. Agent Entry Points Are Incomplete

Current README lists only:
- critical path
- task-board index
- PM heartbeat
- designer heartbeat
- designer current task

Current repo reality:
- `Design/AgentTasks/README.md` defines the full lane set:
  - gameplay
  - UI
  - art-atlas
  - designer
  - support-ftue
  - qa-hci
  - PM
  - visual-target is also present as a held lane.

Replacement:
- Replace the current short lane list with the full lane list from `Design/AgentTasks/README.md`.
- Add the current rule: a lane current-task file is the source of active work unless the user gives a newer direct instruction.
- Mention that most current lane files are held for the 3D fresh-start reset unless PM/user dispatches new work.

### 4. Current Production Lock Is Invalid

Current README says:
- M01 First Contact is the active playable-slice gate.
- M01 is infantry-only.
- no M02-M05, vehicles, transport, base/build, or broad combat variety should start.

Current repo reality:
- `Design/AgentTasks/M01_CRITICAL_PATH.md` says status is held for 3D fresh-start reset.
- All checked lane current-task files are held.
- Recent user-directed work has focused on architecture, GC, Burst, visual profile, and UI system work rather than the old M01-only gate.

Replacement:
- Remove the current production-lock block.
- Replace it with:
  - "No root README production lock is active."
  - "Current work is controlled by `Design/AgentTasks/*_current.md` and direct PM/user instructions."
  - "Do not resume old M01/2D target-lock tasks from report history unless explicitly reactivated."

### 5. UI/UX Roadmap Summary Needs Workflow Refresh

Current README still says:
- accepted target method is a new generated `1672 x 941` landscape target.
- page titles use `Oxanium-Bold SDF`; other text uses `Oxanium-Light SDF`.

Current repo reality:
- `Design/UIUX_Implementation_High_Level_Spec.md` and `Design/UIUX_Implementation_Detailed_Spec.md` require all new/modified runtime UI text to use `Oxanium-Medium SDF` unless PM approves an exception.
- The active visual-layer workflow includes green-screen layer generation and clean chroma-key removal:
  - `Design/UI_Screen_Reference_To_Icons_Panels_GreenKey_Workflow.md`
  - `Design/VisualLockLayered/WORKFLOW_V15_3D_GREENSCREEN.md`
- The active target-to-canvas workflow requires target reference, layer pack, layer manifest, contact sheet, mapping table, Unity capture, focused crops, and explicit target-match status.

Replacement:
- Replace fixed `1672 x 941` wording with a general rule:
  - target/reference resolution must be explicit per surface;
  - generated layer packs must include reference, layers, manifest, contact sheet, and README;
  - green-key source sheets must be cut with measured clean-green validation when used.
- Replace font guidance with `Oxanium-Medium SDF` as the default for all new/modified runtime UI text.
- Keep the important rule that full-screen mockups must not ship as UI.
- Add green-key cleanup acceptance:
  - no pure key-green pixels;
  - no suspicious green border pixels;
  - alpha crop to actual sprite bounds;
  - checkerboard previews for review.

### 6. Gameplay Roadmap Summary Uses Stale Type Names

Current README lists planned systems such as:
- `ObjectiveManager`
- `MissionResultData`
- `PlayerProfileState`
- `SagaProgress`
- `OperationState`

Current architecture issue:
- The current contract prefers domain runtime types ending in `Entity`, `Component`, or `System`; configs end in `Config`; UI reference holders end in `View`; services are shell-edge only.
- `Manager`, broad state/data/session naming, and project-facing controller/presenter/bridge patterns are explicitly discouraged.

Replacement:
- Replace concrete future class names with capability areas:
  - mode/session launch payload and route requests;
  - scenario setup configs;
  - objective/result/reward ECS components and systems;
  - profile/progression persistence through shell-edge services and config/data boundaries;
  - Campaign/Operations/Skirmish flow systems;
  - AI profile and encounter config assets.
- If specific names are needed, make them architecture-aligned:
  - `GameModeConfig`
  - `ScenarioSetupConfig`
  - `GameLaunchPayloadComponent` or route/request data where appropriate
  - `ObjectiveSystem`
  - `MissionResultSystem`
  - `RewardSystem`
  - `ProfileProgressionSystem`
  - `OperationDistrictSystem`
  - `AIProfileConfig`
  - `EncounterTemplateConfig`

### 7. Runtime Pattern Needs Additional Explicit Cleanup

Current README is partly updated but should be tightened because this section historically carried old bootstrap/singleton guidance.

Required replacement emphasis:
- `GameBootstrap` is retired and must not be restored.
- App/menu lifetime belongs to `MenuBootstrapView` and `MenuBootstrapSystem`.
- Match lifetime belongs to `MatchSceneView` and `MatchBootstrapSystem`.
- UI shell startup belongs only at the shell edge.
- Domain startup/composition is split across narrow `*StartupSystem` and `*CompositionSystem` owners.
- Building runtime and placement must not use `BuildingPlacementSystem.Instance`, public facades, or singleton-style access.
- Building placement/runtime work must go through narrow building systems and ECS boundary buffers/components.
- No new broad `Controller`, `Presenter`, `Manager`, `Bridge`, `Button`, `Facade`, or service-locator shells.

### 8. Scene And Subscene Rules Mention Old Subscene

Current README says:
- `GameSubScene.unity` may still contain ECS authoring components.

Current repo reality:
- the active subscene is `Assets/Game/Scenes/Match/MatchSubScene.unity`.

Replacement:
- Replace `GameSubScene.unity` with `Match/MatchSubScene.unity`.
- Make clear that legacy `Game.unity` should not be referenced as the active scene.

### 9. Performance Direction Still Has M01-Specific Priority Flows

Current README priority flows include:
- public M01 launch
- M01 select/move
- M01 attack/result

Current direction:
- Performance work is broader now: Match GC allocation optimization, Burst hot-path refactor, assembly split, UI shell, and runtime interaction flows.

Replacement:
- Replace M01-only flows with:
  - boot to menu shell;
  - menu-to-match loading and transition;
  - match steady-state after warmup;
  - selection, move, attack, build drawer, transport, projectile/missile, minimap, and UI shell interaction flows;
  - AI steady-state and battle/spike scenarios;
  - rendering/impostor/minimap/attack-trace hot paths.
- Keep explicit GC/Burst rules:
  - Match hot paths target `0 B/frame` after warmup.
  - GC optimization is profiler-callstack evidence first.
  - Burst/jobs are expected for compatible pure frequent ECS data transforms.
  - Avoid per-frame arrays, LINQ, closures, boxing, string formatting, sync points, and `ToEntityArray` / `ToComponentDataArray` in frequent paths.

### 10. Testing Section Needs Current Validation Routing

Current README says:
- full EditMode suite is a recommended gate.

Current repo evidence:
- Recent reports note that Unity `-runTests` can exit `0` without producing XML/TestRunner summary.
- Current reliable explicit runners include:
  - `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation`
  - `EcsBurstHotPathArchitectureTests.RunFocusedValidation`
  - `EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests`
  - focused execute-method runners for touched systems.

Replacement:
- Keep targeted focused tests as first gate.
- State that Unity compile/import and explicit execute-method validations are authoritative when TestRunner XML is unreliable.
- Use `EcsBurstFullEditorValidationRunner.RunAllNonExplicitTests` as the current broad editor validation path until TestRunner output is reliable.
- Keep Android build/device validation for mobile/rendering acceptance.

### 11. Design Documentation Tree May Be Stale

Current README embeds:
- `Design/VisualTargets/DesignDocumentationTree.svg`
- `Design/VisualTargets/UIFlowNavigationTree.svg`

Current metadata:
- `DesignDocumentationTree.svg` timestamp is 2026-05-23.
- `UIFlowNavigationTree.svg` timestamp is 2026-05-21.
- Architecture, scene split, GC, and Burst docs changed after those dates.

Replacement:
- Either regenerate these SVGs from current `Design/README.md` / UI routing docs, or label them as historical until regenerated.
- Do not keep old diagrams as "current" if they omit the menu/match bootstrap split, GC/Burst roadmap, or 3D fresh-start reset.

## Proposed README Structure

Replace the README with this higher-signal structure:

1. Project summary
2. Current setup
   - Unity version and package versions
   - active scenes: Menu, Match, MatchSubScene
   - primary script/design roots
3. Current status
   - current lane-task source of truth
   - dashboard/progress refresh status
   - no old M01 production lock unless reactivated
4. Source of truth
   - design index
   - architecture contract
   - menu/match bootstrap split
   - GC plan
   - Burst roadmap
   - performance contract
   - UI visual workflows
5. Architecture summary
   - asmdef boundaries
   - ECS/runtime pattern
   - bootstrap split
   - UI `*View` rules
   - forbidden broad names/patterns
6. Product direction
   - 3D single-map RTS
   - Campaign/Operations/Skirmish
   - current visual-lock direction
7. UI/UX workflow
   - target/layer pack gate
   - green-key workflow
   - canvas conversion proof
   - TMP font rule
8. Performance and validation
   - GC allocation target
   - Burst/job direction
   - current validation commands/runners
9. Implementation rules
   - no old bootstrapper
   - no full-screen mockup UI
   - no hierarchy lookup
   - no broad controller/presenter/bridge/manager
   - preserve `.meta`
10. Notes
   - generated folders
   - baking/subscene note

## Update Steps

1. Update `README.md` project setup scene paths.
2. Replace the project-status snapshot with a status-source note and remove or clearly mark the stale progress image.
3. Replace agent entry points with the full lane list from `Design/AgentTasks/README.md`.
4. Replace the M01 production-lock section with current 3D fresh-start/lane-file routing.
5. Refresh UI/UX workflow to use the current target-to-canvas and green-key workflows.
6. Replace stale gameplay roadmap type names with architecture-aligned capability areas.
7. Tighten Runtime Pattern around current bootstrap split and no building singleton/facade access.
8. Update Scene/Subscene rules to `Match/MatchSubScene.unity`.
9. Replace M01-specific performance flows with broader Match/runtime/GC/Burst flows.
10. Update Testing to prefer explicit execute-method validations where TestRunner XML is unreliable.
11. Decide whether to regenerate or remove the design/progress images:
    - regenerate `Design/Project_State_Dashboard.md` only after `Project_State_Source.json` is updated;
    - remove `Design/Progress_Tracker.png` from README until a maintained image generator exists;
    - regenerate documentation-tree and UI-flow SVGs if they are meant to be current.
12. Run markdown sanity checks:
    - `git diff --check -- README.md`
    - link existence check for all referenced local docs/assets
    - stale-term scan for `Game.unity`, `GameSubScene`, `GameBootstrap.BeginGameplay`, `BuildingPlacementSystem.Instance`, old M01 production lock, `1672 x 941`, `Oxanium-Light`, and `Oxanium-Bold`.

## Follow-Up Decisions Needed

- PM/user must decide whether `Design/Project_State_Source.json` should be refreshed now or whether the README should simply mark the dashboard as stale.
- PM/user must decide whether `Design/Progress_Tracker.png` should be removed from the README or whether a maintained generator should be added.
- PM/user must decide whether current docs should continue to mention the old M01 gate as historical context, or only through archived reports/task files.

