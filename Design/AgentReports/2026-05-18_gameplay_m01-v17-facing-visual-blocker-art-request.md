# Lane
Gameplay

# Task
P0 M01 v17 runtime soldier direction verification after user rejection.

# Files changed
- `Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerNE_enemyNE_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerSE_enemyNE_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerSW_enemyNE_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerNW_enemyNE_1920x1080.png`
- `Design/AgentReports/Captures/M01_V17_Runtime_PlayerFacingMatrix_NE_SE_SW_NW.png`
- `Design/AgentReports/2026-05-18_gameplay_m01-v17-facing-visual-blocker-art-request.md`

# Contracts touched
- `Design/Architecture/gameplay_solid_ecs_contract.md`: diagnostic override is editor-only and does not add bootstrap mission policy.
- `Design/M01_FirstContact_Production_Contract.md`: M01 mission/map/unit ids remain unchanged.
- `Design/AgentReports/2026-05-18_pm_art-atlas-m01-v17-accepted-gameplay-binding.md`: v17 binding remains accepted only as a package-binding proof, not visual direction approval.

# User-visible behavior
The latest user review rejects the runtime player soldier direction. Gameplay generated real in-game captures for all four available v17 player facings using the existing loading/main-menu/custom-game/match flow and the same M01 camera/HUD.

Runtime player-facing matrix:

- `Design/AgentReports/Captures/M01_V17_Runtime_PlayerFacingMatrix_NE_SE_SW_NW.png`
- Order: player `NE`, `SE`, `SW`, `NW`
- Enemy was held at `NE` for these captures so the bottom/player direction can be judged in isolation.

Visual result: none of the four available v17 player facings gives a clean bottom-soldiers-look-to-top-of-screen read in the actual runtime camera. `NE` and `SE` read as side/front-left or side/right. `SW` and `NW` read as back-left/back-right, not the target top direction.

# Validation run
Runtime captures:

```bash
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV17PlayerNeEnemyNe -logFile /private/tmp/warlinecapture-m01-game-flow-v17-playerNE-enemyNE.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV17PlayerSeEnemyNe -logFile /private/tmp/warlinecapture-m01-game-flow-v17-playerSE-enemyNE.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV17PlayerSwEnemyNe -logFile /private/tmp/warlinecapture-m01-game-flow-v17-playerSW-enemyNE.log
/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -projectPath /Users/farhad/Projects/WarlineCapture -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameSceneViaExistingFlowV17PlayerNwEnemyNe -logFile /private/tmp/warlinecapture-m01-game-flow-v17-playerNW-enemyNE.log
```

Matrix generation:

```bash
magick Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerNE_enemyNE_1920x1080.png -crop 520x300+330+450 /private/tmp/m01_player_NE_crop.png
magick Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerSE_enemyNE_1920x1080.png -crop 520x300+330+450 /private/tmp/m01_player_SE_crop.png
magick Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerSW_enemyNE_1920x1080.png -crop 520x300+330+450 /private/tmp/m01_player_SW_crop.png
magick Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_v17_playerNW_enemyNE_1920x1080.png -crop 520x300+330+450 /private/tmp/m01_player_NW_crop.png
magick /private/tmp/m01_player_NE_crop.png /private/tmp/m01_player_SE_crop.png /private/tmp/m01_player_SW_crop.png /private/tmp/m01_player_NW_crop.png +append Design/AgentReports/Captures/M01_V17_Runtime_PlayerFacingMatrix_NE_SE_SW_NW.png
```

Diff hygiene:

```bash
git diff --check -- Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs
```

# Validation result
- All four runtime capture commands exited 0.
- Normal route proof remained intact: `WARLINECAPTURE_M01_GAME_FLOW_LAUNCHED splash=1 main=1 quickCustom=1 match=1 activeMission=saga.ch01.m01.first_contact`.
- Frame proof:
  - player `NE`: `frame=idle_v17_baked_shadow.NE`
  - player `SE`: `frame=idle_v17_baked_shadow.SE`
  - player `SW`: `frame=idle_v17_baked_shadow.SW`
  - player `NW`: `frame=idle_v17_baked_shadow.NW`
- Focused `git diff --check`: passed.
- Visual result: blocked. Available v17 player facings do not satisfy the required target direction.

# Known gaps
- The player squad direction cannot be fixed cleanly with the current v17 facings.
- The current ECS renderer also uses one facing for the whole four-soldier squad. If Art delivers per-soldier target-facing cells, Gameplay will need to bind per-soldier facing data rather than one squad-wide facing.
- No final visual approval should be claimed from v17.

# Cross-lane impacts
- Art/Atlas must provide a corrected M01 soldier package with bottom/player soldiers facing the top of the screen in the exact M01 camera read, with baked shadows matching the map lighting.
- Art/Atlas should also provide top/enemy soldiers facing down-screen with the same camera-read guarantee.
- PM should keep QA/UI/HCI held for M01 visual approval until corrected soldier direction art is available and Gameplay rebinding is proven.

# Next recommended task
Route Art/Atlas for a v18 direction-locked baked-shadow soldier atlas. Required deliverable: accepted player and enemy idle/animation facings that visually match the M01 target in runtime camera space, not just compass labels. Gameplay can then bind v18 through the existing ECS path and regenerate runtime proof.
