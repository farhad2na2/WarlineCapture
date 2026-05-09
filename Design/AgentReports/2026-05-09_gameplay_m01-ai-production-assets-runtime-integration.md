# Lane
Gameplay

# Task
P0 integrate the M01 AI production art pack into ECS runtime and capture proof.

# Files changed
- `Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs`
- `Assets/Game/Scripts/Components/MissionRuntimeComponents.cs`
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/TacticalMaps/TacticalMapRuntimeLoader.cs`
- `Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs`
- `Assets/Tests/PlayMode/Chapter01M01PlayModeValidationTests.cs`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-idle.png`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-idle-20x9.png`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-run.png`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-selected-player-run-20x9.png`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-enemy-patrol.png`
- `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/campaign-public-m01-v2-enemy-patrol-20x9.png`

# Contracts touched
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_ai_production_asset_manifest.json`
- `Assets/Game/Art/Generated/2DISO/Chapter01/M01_AIProduction/Manifests/m01_soldier_animation_manifest_v2.json`
- `MissionRuntimeTerrainSurfaceRendererRuntime` now stores the resolved AI production tactical plate pack.
- `MissionRuntimeAtlasQuadRuntime` marker materials now use AI production marker asset ids resolved through the manifest-backed resolver.

# User-visible behavior
- Public M01 tactical runtime now uses `m01_tactical_plate_a_pot_2048x1024.png` as the active tactical ground.
- Runtime terrain also binds all three production tactical plates from the manifest for validation and future selection.
- Existing command-point decor now resolves to production `command_support` intact/damaged/destroyed sprites instead of the old tactical manifest art.
- Selection, move, and attack marker materials now resolve through the production marker manifest entries.
- Player rifle squad and enemy patrol remain ECS atlas-backed through the v2 soldier animation manifest; no SpriteRenderer/MeshRenderer unit presentation was introduced.
- Captures show production ground, command building, selected player idle/run, visible selection markers, and reachable enemy patrol view at the M01 camera scale.

# Validation run
- First Unity attempt in sandbox failed before tests with Package Manager IPC `listen EPERM`; reran with approved Unity command outside sandbox.
- Passed: `Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform EditMode -testFilter Chapter01M01AtlasQuadPresentationTests -testResults /private/tmp/warlinecapture-m01-ai-production-editmode-results.xml -logFile /private/tmp/warlinecapture-m01-ai-production-editmode.log`
- Passed: `Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.PublicCampaignLaunch_ReachesM01ProductionVisibleSlice -testResults /private/tmp/warlinecapture-m01-ai-production-playmode-results.xml -logFile /private/tmp/warlinecapture-m01-ai-production-playmode.log`
- Passed: `Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests.GameScene_M01SpritePresenterUsesEcsDrivenAtlasStateIds -testResults /private/tmp/warlinecapture-m01-ai-production-marker-playmode-results.xml -logFile /private/tmp/warlinecapture-m01-ai-production-marker-playmode.log`

# Validation result
Ready for PM/user runtime review.

EditMode result: 5/5 passed. PlayMode public campaign proof result: 1/1 passed. PlayMode state/marker proof result: 1/1 passed.

The tactical image/background, command building, soldier v2 atlases, and production markers now match the approved AI production asset style better than the previous soldier-only proof. No alpha speckle, obvious edge bleed, wrong facing, or state-transition break was observed in the captured proof. The selected marker is now visible in capture after preserving the authored cyan marker texture instead of applying the old amber tint.

# Known gaps
- The public gameplay route has one active terrain surface, so plate A is the active M01 ground while plates B/C are bound and validated in the ECS terrain runtime component. Switching between tactical plates would need a separate map/encounter selection task.
- The strategic background sprite is manifest-resolved and validated by Gameplay code, but this pass did not rebuild the Saga Map UI to display it because that would touch UI screen composition outside the tactical runtime path.
- Capture names still include `v2` because they extend the previous soldier proof helper; folder and report name identify this as the full AI production runtime pass.

# Cross-lane impacts
- Art/Atlas can review whether plate A is the intended active public M01 tactical ground, and whether the command-support building scale should be adjusted against the approved target board.
- UI may need a follow-up if PM wants `strategic.ch01.m01.background` surfaced on the Saga Map route rather than only manifest-resolved by Gameplay.
- QA can use the capture folder above plus the three validation result XML files in `/private/tmp`.

# Next recommended task
PM/user visual review of `Design/AgentReports/Captures/2026-05-09_m01-ai-production-runtime/`, then assign a focused follow-up for strategic Saga Map presentation or tactical plate selection if required.
