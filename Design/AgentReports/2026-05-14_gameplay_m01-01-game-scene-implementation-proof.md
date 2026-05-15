# Lane
Gameplay

# Task
Implement the actual Game scene M01-01 tactical start/no-selection state and provide runtime visual-match proof against `M01-01_TacticalStart_1920x1080.png`.

# Files changed
- `Assets/Game/Scenes/Game.unity`
- `Assets/Game/Scripts/Bootstrap/M01GameSceneStartupController.cs`
- `Assets/Game/Scripts/Bootstrap/M01GameSceneStartupController.cs.meta`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01GameSceneImplementationBuilder.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01GameSceneImplementationBuilder.cs.meta`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs`
- `Assets/Game/Scripts/Editor/WarlineCaptureM01RuntimeVisualMatchProofCapture.cs.meta`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png`
- `Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-14_gameplay_m01-01-game-scene-implementation-proof.md`

# Contracts touched
- Added `M01GameSceneStartupController`, a scene-level startup component that begins `saga.ch01.m01.first_contact`, calls `GameBootstrap.BeginGameplay()`, enables the parallel UI, and routes to `WarlineCaptureRoute.Match`.
- Added `WarlineCaptureM01GameSceneImplementationBuilder.Build` to wire the M01 startup controller into `Assets/Game/Scenes/Game.unity`.
- Updated `Game.unity` so `WarlineCaptureUiBootstrap` starts in parallel UI mode and routes to Match.
- Extended `WarlineCaptureM01RuntimeVisualMatchProofCapture` with an actual Game scene playmode capture path.
- No flattened mockup PNG was imported or used as runtime source.
- No M01-02 selected-state behavior was implemented.

# User-visible behavior
- Opening the actual Game scene now starts the M01 First Contact mission path and routes the Codex UI to Match.
- The M01 tactical binder applies the Chapter01 tactical map, and the Match HUD is visible in the runtime capture.
- The captured runtime result is not yet visually acceptable against the approved M01-01 target.

# Validation run
- Read `Design/AgentTasks/gameplay_heartbeat.md`.
- Read `Design/AgentTasks/gameplay_current.md`.
- Read `Design/AgentReports/2026-05-14_pm_gameplay-game-scene-full-implementation-required.md`.
- Read prior Gameplay proof and implementation reports.
- Read Designer spec and Art/Atlas imagegen sample reports.
- Applied scene wiring through CodexUnity because the main project is open in another Unity instance:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: Unity 6000.4.0f1 `-batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01GameSceneImplementationBuilder.Build -logFile /private/tmp/warlinecapture-m01-game-scene-implementation-builder.log`
  - Result marker: `WARLINECAPTURE_M01_GAME_SCENE_STARTUP_BUILT scene=Assets/Game/Scenes/Game.unity`
- First actual Game scene capture attempt:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: Unity 6000.4.0f1 `-batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameScenePlayMode -logFile /private/tmp/warlinecapture-m01-01-game-scene-playmode-capture.log`
  - Result: M01 startup path ran, but `-nographics` render texture output was blank gray.
- Second actual Game scene capture attempt:
  - Workspace: `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
  - Command: Unity 6000.4.0f1 `-batchmode -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity1 -executeMethod WarlineCaptureM01RuntimeVisualMatchProofCapture.CaptureGameScenePlayMode -logFile /private/tmp/warlinecapture-m01-01-game-scene-playmode-capture-gui.log`
  - Result marker: `WARLINECAPTURE_M01_GAME_SCENE_CAPTURED path=Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png`
  - Process required manual `kill` after capture because Unity remained alive during shutdown with Unity.Entities SubScene shutdown exceptions.
- Important runtime log markers:
  - `TACTICAL_MAP_RUNTIME_LOADER_LOADED mapId=iso.ch01.district_edge_01 children=2`
  - `CHAPTER01_TACTICAL_BINDER_APPLIED mission=saga.ch01.m01.first_contact mapId=iso.ch01.district_edge_01 grid=64x36`
  - `AIControlMode faction=1 mode=Auto controlledUnits=1 controlledBuildings=0`
  - `AIControlMode faction=0 mode=Manual controlledUnits=1 controlledBuildings=0`
- Comparison command:
  - `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLock/Gameplay/M01_StepByStepGameplayMockups/Images/M01-01_TacticalStart_1920x1080.png --capture Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_1920x1080.png --out Design/AgentReports/Captures/M01-01_GameSceneRuntimeCapture_vs_Target_Comparison.png --label "M01-01 Game scene visual proof"`
- Capture dimensions:
  - Runtime capture: 1920x1080
  - Comparison image: 5824x1170

# Validation result
- Scene startup implementation: partial pass.
- Actual Game scene capture: produced a nonblank 1920x1080 runtime image.
- Visual-match result: failed / needs fixes.
- Comparison MSE: `3177.33`.
- The capture proves the Game scene now reaches the M01 runtime path and Match HUD, but it does not prove the required M01-01 target match.

# Match / mismatch assessment
- All soldiers: failed. The runtime logs report one player-controlled and one enemy-controlled unit, but the capture does not show visible battlefield soldiers.
- ECS animation proof: failed. ECS runtime startup is evidenced by logs, but visible animated soldier frames are not present in the capture.
- Camera/framing: failed. Runtime camera shows an empty paved road/plaza area, not the target composition with player squad lower-left and enemy patrol upper-right.
- Objective/Star Goals: partial mismatch. Objective and Star Goals panels are visible, but text/content differs from target: runtime shows multiple objectives such as `Destroy the forward patrol 0/1`, not exactly `Destroy hostile patrol`.
- Command panel state: mismatch. Runtime command panel shows active-looking Move and other controls; target M01-01 requires neutral/disabled no-selection state.
- Allowed command set: mismatch. Runtime shows Stop, Hold, Move, Attack, Build; target command order is Select, Move, Attack, Stop, Hold, with Build secondary/unavailable.
- Squad cards: partial mismatch. Rifle Squad card is visible, but additional expected/target card presentation and no-selection state do not match the target.
- Threat feed: mismatch. Runtime threat feed shows live alert rows such as `Enemy Air Detected`; target allows mission-start row and should not show unrelated alerts.
- Minimap start viewport: partial mismatch. Minimap is visible, but its viewport/content does not match the approved M01-01 target composition.
- Assistant closed: mismatch. ARIA dock/entry is visible on the left; target M01-01 requires assistant closed.
- Build unavailable reason: partial mismatch. Build is present, but the capture does not visibly communicate `MissionDoesNotAllowBuild`.
- No selected rings: pass in visible capture. No player selection rings are visible.
- No selected status: pass in visible capture. No selected squad world status bar is visible.
- No world markers: pass in visible capture. No move/attack/objective/invalid markers are visible.

# Known gaps
- Visible ECS soldier presentation is still missing from the runtime capture.
- Camera/framing must be corrected to the M01-01 visual target and must include the player squad and enemy patrol.
- HUD state must be tightened to M01-01: neutral/disabled commands, allowed command order, Build unavailable reason, mission-start threat row, assistant closed, and exact objective copy.
- Unity.Entities SubScene shutdown throws during capture process exit; capture completes before the shutdown exception, but the Unity process required manual kill after writing the PNG.
- QA/HCI, PM/user approval, M01-02, and further gameplay slices remain held.

# Cross-lane impacts
- PM/user should not approve M01-01 yet.
- QA/HCI should remain blocked.
- This remains a Gameplay implementation issue; Art/Atlas and Designer do not need to change the approved target unless PM decides to narrow acceptance.

# Next recommended task
Gameplay should continue by fixing visible ECS soldier rendering/camera framing in the actual Game scene, then tighten the Match HUD to the M01-01 no-selection contract and rerun the same Game scene proof capture.
