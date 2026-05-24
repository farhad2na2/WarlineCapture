Lane: UI

Task: GameUI Step 9 - capture stable runtime shell states.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs
- Assets/Game/Scenes/GameUI.unity
- Design/AgentReports/Captures/GameUI/GameUI_Loading_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_MainMenu_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_MatchHud_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_ResultPopup_Stable.png
- Design/AgentReports/Captures/GameUI/GameUI_ReturnedMainMenu_Stable.png

Contracts touched:
- GameUI Step 9 now captures the five stable shell states required by the implementation plan.
- Capture output is fixed at 2400x1080 PNG under Design/AgentReports/Captures/GameUI.
- Step 9 validation requires all five captures to exist and be non-trivial PNG files.

User-visible behavior:
- No runtime behavior change beyond previous steps.
- The project now has reviewable visual artifacts for loading, main menu, match HUD, result popup, and returned main menu states.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep9 -logFile /private/tmp/warlinecapture-gameui-step9-unity2.log
- file Design/AgentReports/Captures/GameUI/*.png

Validation result:
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP9_VALIDATED captures=5 folder=Design/AgentReports/Captures/GameUI
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP9_BUILT scene=Assets/Game/Scenes/GameUI.unity
- PASS: all five captures are 2400x1080 PNG files.

Known gaps:
- Captures show structural placeholder content, not final target-matched art.
- Main menu and match HUD still have large empty black areas because final screen assets are not wired into these Step 6 placeholder prefabs.
- Transition sample captures were not added; this pass captures stable states only.

Cross-lane impacts:
- None expected. Game scene and legacy UI remain untouched.

Next recommended task:
- Step 10: write final GameUI shell handoff, then decide whether the next lane task is replacing Step 6 placeholder prefabs with target-matched region assets or adding transition sample captures.
