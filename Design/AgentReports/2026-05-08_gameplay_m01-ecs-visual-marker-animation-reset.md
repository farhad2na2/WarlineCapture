# Lane
Gameplay

# Task
Resolve the P0 M01 selected-readability rejection gate by replacing the rejected runtime `GameObject` atlas wrapper path with ECS entity visuals, resetting infantry marker/scale/animation behavior, and adding validation that rejects legacy public M01 visible presentation.

# Files changed
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/2026-05-08_gameplay_m01-ecs-visual-marker-animation-reset.md`

# Contracts touched
- Public M01 unit visible presentation now creates ECS render entities with `MaterialMeshInfo`, `LocalToWorld`, `MissionRuntimeEcsVisualTag`, and no runtime `GameObject` atlas root.
- The rejected `M01RuntimeEcsAtlasQuads` root is no longer created for public M01 unit visuals.
- `MissionRuntimeAtlasQuadRuntime` keeps legacy compatibility fields but M01 validation now requires `Instance`, `Renderer`, `MeshFilter`, `SoldierRenderers`, and `SelectionRenderers` to remain unused/null or empty for public unit visuals.
- M01 infantry scale is fixed to the user-observed readability target near `0.15`.
- Player rifle squad renders as four ECS soldier visual entities; enemy patrol renders as one ECS soldier visual entity.
- Selection markers render as per-soldier ECS marker entities using the Art/Atlas `selection_ring` texture with grounded warm/amber sizing.
- Command target markers render as ECS marker entities using `move_destination_ring`/`attack_target_ring`, positioned from `UnitTarget`/`EngageTarget`, and constrained to small world-space dimensions.
- The legacy ECS mesh suppression system now suppresses untagged legacy mesh visuals while allowing tagged production M01 ECS visual entities.

# User-visible behavior
- M01 public launch no longer uses the rejected `GameObject`/`MeshRenderer` atlas wrapper for unit visuals.
- Command squad and hostile patrol use individual soldier atlas slices, not the rejected grouped temporary infantry sprite path.
- Moving infantry advances a run/move visual loop; idle state resets to idle.
- The command squad reads at the smaller `0.15` scale and preserves sprite aspect through the ECS quad layout.
- Selected soldiers show small grounded selection rings instead of placeholder yellow squares.
- Move/attack target feedback is a small world marker at the intended point, not a screen-covering green marker.
- The right-side hostile patrol uses the same alive infantry state contract and validation rejects old separate destroyed/red artifact visuals on alive enemies.
- Selection remains validated through the public selection controller and full selected unit/body flow.

# Validation run
- Focused ECS visual, marker, animation, and scale validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds -testResults /private/tmp/warlinecapture-m01-ecs-visual-focused-results.xml -logFile /private/tmp/warlinecapture-m01-ecs-visual-focused.log`
- Full M01 public route and gameplay validation:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-ecs-visual-full-results.xml -logFile /private/tmp/warlinecapture-m01-ecs-visual-full.log`
- Static patch hygiene:
  `git diff --check -- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Systems/M01LegacyEcsRenderingSuppressionSystem.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

# Validation result
- Focused ECS visual test passed `1/1`, `0` failed. Results: `/private/tmp/warlinecapture-m01-ecs-visual-focused-results.xml`. Log: `/private/tmp/warlinecapture-m01-ecs-visual-focused.log`.
- Full `Chapter01M01PlayModeValidationTests` passed `8/8`, `0` failed. Results: `/private/tmp/warlinecapture-m01-ecs-visual-full-results.xml`. Log: `/private/tmp/warlinecapture-m01-ecs-visual-full.log`.
- The full suite includes the public campaign and quick-custom M01 routes through Main Menu/Saga Map/briefing/loadout/deploy coverage.
- Automated evidence now checks ECS visual entities, no rejected runtime wrapper root, no unsuppressed legacy ECS meshes, animation phase/local pose movement, selected marker size/texture/placement, target marker size/texture/placement, infantry scale, alive enemy atlas state, and suppression of legacy destroyed/model artifacts.
- Non-blocking Unity shutdown noise remained in logs: preview-scene leak warning, persistent allocation leak summary, debugger/usbmuxd shutdown warnings.

# User-feedback matrix
- UFB-2026-05-08-01: GameObject renderer wrapper rejected. Addressed by ECS `MaterialMeshInfo` visual entities and validation that `M01RuntimeEcsAtlasQuads`, `Instance`, `Renderer`, `MeshFilter`, `SoldierRenderers`, and `SelectionRenderers` are not used for public M01 unit visuals.
- UFB-2026-05-08-02: Huge green target marker. Addressed by small ECS command target marker using `move_destination_ring`/`attack_target_ring`, world-space scale `<= 0.32 x 0.12`, and target-position validation.
- UFB-2026-05-08-03: Wrong/crouched/sitting animation. Addressed by resolving alive idle/move/attack atlas state ids and validating move animation phase/local pose changes instead of destroyed/hit idle artifacts.
- UFB-2026-05-08-04: Scale too large/squashed. Addressed by infantry scale `0.15` and validation range `0.145-0.155`.
- UFB-2026-05-08-05: Red flashing sitting enemy/object on right. Addressed by hostile patrol using alive soldier atlas states and legacy/destroyed visual suppression validation.
- UFB-2026-05-08-06: Foot-pixel selection. Addressed by public selection-controller coverage plus per-soldier grounded ECS selection rings across the formation.
- UFB-2026-05-08-07: Placeholder yellow selection squares. Addressed by Art/Atlas `selection_ring` texture validation and warm grounded marker material checks.
- UFB-2026-05-08-08: Process failure. Addressed by focused and full automated validation plus this immediate handoff report with exact commands, logs, and result paths.

# Known gaps
- Validation ran in the unlocked Unity mirror `/Users/farhad/Projects/WarlineCapture-CodexUnity1`; source edits are in `/Users/farhad/Projects/WarlineCapture` and were synced into the mirror for batchmode.
- This pass uses automated measurement/evidence, not a new manual screenshot/video capture. The focused test records animation movement through phase/local pose deltas and marker/scale through runtime state assertions.
- Final atlas art is still marked temporary by the existing M01 presenter contract until Art/Atlas delivers final multi-frame production atlases.

# Cross-lane impacts
- PM/QA can review the Gameplay handoff as ready for the selected-readability gate.
- UI should keep the static `WorldCommandMarkerLayer` disabled for this path; command markers are now owned by Gameplay ECS world visuals.
- Art/Atlas marker assets are consumed directly by Gameplay runtime materials: `selection_ring`, `move_destination_ring`, and `attack_target_ring`.
- Designer scale/marker constraints are now enforced in PlayMode validation.

# Next recommended task
PM/QA should review the selected-readability Gameplay handoff and rerun or capture the public M01 visual route for human-facing acceptance. If accepted, assign the next Gameplay priority through `Design/AgentTasks/gameplay_current.md`.
