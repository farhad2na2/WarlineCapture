Lane: UI

Task: GameUI runtime shell implementation handoff, Steps 1-10.

Files changed:
- Assets/Game/Scenes/GameUI.unity
- Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs
- Assets/Game/Scripts/Editor/WarlineCaptureGameUiContentPrefabBuilder.cs
- Assets/Game/Scripts/UI/Shell/UIShellRegionView.cs
- Assets/Game/Scripts/UI/Shell/UIMotionHostView.cs
- Assets/Game/Scripts/UI/Shell/UIShellView.cs
- Assets/Game/Scripts/UI/Shell/WarlineCaptureShellEcsBridgeView.cs
- Assets/Game/Scripts/UI/Shell/WarlineCaptureShellContentPresenterView.cs
- Assets/Game/Scripts/UI/Shell/UIGameUiSmokeDriverView.cs
- Assets/Game/Scripts/UI/Shell/Ecs/UiShellComponents.cs
- Assets/Game/Scripts/UI/Shell/Ecs/UiShellBoundarySystem.cs
- Assets/Game/Scripts/UI/Shell/Ecs/UiShellFlowSystem.cs
- Assets/Game/Prefabs/UI/Shell/Content/SCN01_LoadingContent.prefab
- Assets/Game/Prefabs/UI/Shell/Content/SCN02_MainMenuContent.prefab
- Assets/Game/Prefabs/UI/Shell/Content/SCN08_MatchHudContent.prefab
- Assets/Game/Prefabs/UI/Shell/Popups/POP05_MissionResultPopup.prefab
- Design/AgentReports/2026-05-24_ui_gameui-scene-step7.md
- Design/AgentReports/2026-05-24_ui_gameui-scene-step8.md
- Design/AgentReports/2026-05-24_ui_gameui-scene-step9.md
- Design/AgentReports/Captures/GameUI/GameUI_Loading_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_MainMenu_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_MatchHud_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_ResultPopup_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_ReturnedMainMenu_Stable.png
- Design/AgentReports/2026-05-24_ui_gameui-runtime-shell-step10-handoff.md

Contracts touched:
- New isolated GameUI scene contract: GameUI is self-contained and does not modify Game.unity, legacy UI scenes, legacy router, legacy screen controller, or legacy modal controller.
- Runtime shell region contract: LoadingLayer, HeaderRegion, LeftRegion, MiddleRegion, RightRegion, FooterRegion, and PopupLayer own only view placement and animation targets.
- ECS boundary contract: ECS shell data owns route requests, transition state, popup requests, loading progress, and presentation commands.
- Unity view contract: WarlineCaptureShellView and motion/presenter views execute animations and content placement only; they do not decide gameplay routing.
- Smoke-driver contract: WarlineCaptureGameUiSmokeDriverView is scene-only/debug validation flow and must not become gameplay policy.

User-visible behavior:
- Opening Assets/Game/Scenes/GameUI.unity now shows an isolated UI shell validation scene with its own camera, Screen Space Camera canvas, InputSystemUIInputModule, shell regions, content presenter, and smoke driver.
- The scene validates the intended first runtime flow: Loading -> Main Menu -> Loading -> Match HUD -> Result Popup -> Loading -> Main Menu.
- Stable screenshot captures were generated for loading, main menu, match HUD, result popup, and returned main menu states.
- Legacy Game.unity and legacy UI/router behavior are not changed by this slice.

Validation run:
- Unity project: /Users/farhad/Projects/WarlineCapture-CodexUnity2
- Command: /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep9 -logFile /private/tmp/warlinecapture-gameui-step9-unity2.log
- Focused validations included Step 7 flow wiring, Step 8 layout guards, and Step 9 stable-state capture generation.

Validation result:
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP7_VALIDATED scene=Assets/Game/Scenes/GameUI.unity
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP8_VALIDATED scene=Assets/Game/Scenes/GameUI.unity
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP9_VALIDATED captures=5 folder=Design/AgentReports/Captures/GameUI
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP9_BUILT scene=Assets/Game/Scenes/GameUI.unity
- Capture files verified as PNG image data, 2400 x 1080, RGBA:
  - Design/AgentReports/Captures/GameUI/GameUI_Loading_Stable.png
  - Design/AgentReports/Captures/GameUI/GameUI_MainMenu_Stable.png
  - Design/AgentReports/Captures/GameUI/GameUI_MatchHud_Stable.png
  - Design/AgentReports/Captures/GameUI/GameUI_ResultPopup_Stable.png
  - Design/AgentReports/Captures/GameUI/GameUI_ReturnedMainMenu_Stable.png

Known gaps:
- Captured visuals are structural placeholder shell content, not final target-matched screen art.
- Latest approved target-lock region assets still need to replace the Step 6 placeholder prefabs.
- Step 9 captures stable states only; transition sample captures for header entry, side-region entry, middle scale, and popup scale are not generated yet.
- Step 8 layout guards validate region containment and popup centering, but do not yet validate pixel-perfect art match, text fit against final fonts, or alpha-visible icon centering for final assets.
- The smoke driver proves the shell sequence in GameUI only; gameplay scene integration is not implemented.

Cross-lane impacts:
- Gameplay lane can continue independently because no gameplay scene or runtime gameplay policy was changed.
- UI/design lane can now replace placeholder region prefabs with approved screen-specific assets inside the existing GameUI shell contract.
- QA/PM can review the five stable captures for shell structure, but should not treat them as final visual target-match approval.

Next recommended task:
- Replace the placeholder content prefabs with approved target-matched region prefabs for loading, main menu, match HUD, and result popup, then rerun Step 8 layout guards and Step 9 capture generation with transition sample captures enabled.
