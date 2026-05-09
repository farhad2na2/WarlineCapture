# Lane

Gameplay

# Task

P0 integrate v2 soldier atlas into M01 ECS runtime and capture proof.

# Files changed

- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Tests/Editor/Chapter01M01SpritePresenterTests.cs`
- `Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-idle.png`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-idle-20x9.png`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-run.png`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-run-20x9.png`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-enemy-patrol.png`
- `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-enemy-patrol-20x9.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01.png`
- `Design/AgentReports/Captures/2026-05-08_m01-public-launch/campaign-public-m01-20x9.png`
- `Design/AgentReports/2026-05-09_gameplay_m01-soldier-v2-runtime-integration.md`

# Contracts touched

- M01 player rifle squad and enemy patrol now use `m01_soldier_animation_manifest_v2.json` through the ECS atlas-quad runtime path.
- Runtime source remains ECS/Entities Graphics atlas quads. No `SpriteRenderer`, `MeshRenderer` wrapper, or legacy unit model presentation was introduced.
- Presenter state ids now resolve per state:
  - idle -> `.idle`
  - move -> `.move` mapped to manifest `run`
  - attack -> `.attack` mapped to manifest `fire`
  - damaged -> `.damaged`
  - destroyed -> `.death`
- V2 manifest data drives atlas texture path, faction split, facing id, state id, frame order, fps, loop flag, atlas rect, pivot, and normalized bounds.
- Player and enemy atlases remain separate.
- Procedural move bob/scale is disabled for final v2 atlas soldiers; movement is driven by manifest frame UV changes instead.
- Enemy tint is disabled for final v2 atlas soldiers so the approved atlas color is not distorted.

# User-visible behavior

- The public M01 runtime now displays v2 soldier atlas frames for the player rifle squad and enemy patrol.
- Selected player idle, selected player run, and enemy patrol proof captures were produced from the public campaign path at M01 camera scale.
- The rest of the runtime scene still uses the current M01 map/HUD/marker presentation; this task did not integrate the approved production map plates, building atlases, or marker pack.

# Validation run

- EditMode:
  - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter Chapter01M01SpritePresenterTests -testResults /private/tmp/warlinecapture-gameplay-v2-presenter-editmode-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-presenter-editmode.log`
  - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform EditMode -testFilter Chapter01M01AtlasQuadPresentationTests -testResults /private/tmp/warlinecapture-gameplay-v2-atlasquad-editmode-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-atlasquad-editmode.log`
- PlayMode:
  - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_ReachesM01ProductionVisibleSlice -testResults /private/tmp/warlinecapture-gameplay-v2-runtime-playmode-proof-rerun-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-runtime-playmode-proof-rerun.log`
  - `"/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity" -batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds -testResults /private/tmp/warlinecapture-gameplay-v2-presenter-playmode-final-results.xml -logFile /private/tmp/warlinecapture-gameplay-v2-presenter-playmode-final.log`
- Diff hygiene:
  - `git diff --check -- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Campaign/Chapter01M01SpritePresenterCatalog.cs Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs Assets/Tests/Editor/Chapter01M01SpritePresenterTests.cs Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`

# Validation result

- EditMode passed:
  - `Chapter01M01SpritePresenterTests`: 3/3 passed.
  - `Chapter01M01AtlasQuadPresentationTests`: 4/4 passed.
- PlayMode passed:
  - `PublicCampaignLaunch_ReachesM01ProductionVisibleSlice`: 1/1 passed.
  - `GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds`: 1/1 passed.
- Generated proof captures:
  - `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-idle.png`
  - `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-selected-player-run.png`
  - `Design/AgentReports/Captures/2026-05-09_m01-v2-runtime/campaign-public-m01-v2-enemy-patrol.png`
  - plus 20:9 variants for each.
- Visual comparison:
  - Soldiers: v2 atlas soldiers are visible in runtime and no longer use the old individual soldier sheet or rejected mini-squad fallback.
  - Background/map: does not match the approved target package yet; the current public M01 runtime still uses the existing clean tan tactical map, not the approved darker ruined true-isometric background.
  - Markers: do not yet match the approved marker target package; current command/selection/HUD markers remain from the existing runtime path.
  - Image style overall: not final target quality because only the soldier runtime was integrated in this task.
  - Edge bleed/alpha speckle: no obvious edge bleed or alpha speckle was visible in the reviewed captures, but QA should inspect at device scale.
  - Sliding/facing: manifest-driven run frames advance in PlayMode; no procedural bob is applied. Facing is resolved from move/attack target direction with SE fallback.

# Known gaps

- Player squad is backed by 4 ECS soldier render entities, but at current M01 camera scale the proof captures read as two dominant visible silhouettes. Formation spacing/scale should be reviewed by PM/QA before final visual approval.
- Ground selection rings are present by ECS validation, but they are subtle in the proof captures and may need visual tuning after marker pack integration.
- Attack currently maps to the manifest `fire` sequence. The manifest `aim` sequence is available but not separately represented by the current `MissionRuntimeSpriteVisualState` enum.
- Runtime still lacks approved AI production tactical map plates, building atlases, and marker assets.
- No mobile memory profiling was run for the two 4096x1792 v2 soldier atlases.
- No device capture/video was produced; this handoff uses Unity PlayMode PNG captures.

# Cross-lane impacts

- PM/user can review the v2 runtime proof captures for acceptance or route specific fixes.
- QA/HCI can now validate runtime soldier scale, selected readability, idle/run continuity, facing, alpha bleed, and marker readability.
- Art/Atlas should only re-enter if PM/QA requests atlas repack/padding, scale-specific sprite adjustments, or visible art fixes.
- UI is not directly changed, but current HUD panels partially cover the runtime proof frame and should be considered during final selected-readability review.

# Next recommended task

PM/user should review the v2 runtime captures. If accepted, route QA/HCI for selected-readability validation; if rejected, the highest-value next Gameplay fix is squad formation/selection readability at M01 camera scale, followed by marker-pack integration once PM assigns that scope.
