Lane: UI

Task: GameUI Step 7 - wire isolated scene flow with content prefabs and smoke driver.

Files changed:
- Assets/Game/Scenes/GameUI.unity

Contracts touched:
- GameUI scene now binds the shell content presenter to the runtime shell.
- GameUI scene now binds Step 6 content prefabs for loading, main menu, match HUD, and mission result popup.
- GameUI scene now includes the scene-only smoke driver for the loading-to-menu-to-match-to-popup-to-menu flow.

User-visible behavior:
- Opening GameUI now has content prefab references wired into the shell instead of empty region roots.
- Play mode can run the scene smoke sequence automatically through loading, main menu, match HUD, mission result popup, and return to menu.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep7 -logFile /private/tmp/warlinecapture-gameui-step7-unity2-guidfix.log
- rg -n "WarlineCaptureGameUiSmokeDriverView|WarlineCaptureShellContentPresenterView|InputSystemUIInputModule|StandaloneInputModule|GameUICamera" Assets/Game/Scenes/GameUI.unity

Validation result:
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP7_VALIDATED scene=Assets/Game/Scenes/GameUI.unity
- PASS: Step 7 validation exercises presenter installs for loading, menu regions, match HUD regions, and mission result popup.
- PASS: GameUI scene contains GameUICamera and InputSystemUIInputModule.
- PASS: GameUI scene does not contain StandaloneInputModule.

Known gaps:
- Smoke driver is wired and structurally validated; no screenshot/video capture was produced in this step.
- Step 6 prefabs are still structural placeholder content, not final target-matched art prefabs.

Cross-lane impacts:
- None expected. Game scene and legacy UI are not modified.

Next recommended task:
- Step 8: add hard layout guards for region bounds, content fit, panel overlap, centered popup scaling, and stable header behavior.
