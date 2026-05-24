Lane: UI

Task: Fix GameUI scene visibility and Input System runtime error.

Files changed:
- Assets/Game/Scenes/GameUI.unity
- Assets/Game/Scripts/Editor/WarlineCaptureGameUiSceneBuilder.cs

Contracts touched:
- GameUI isolated shell scene contract now includes one dedicated GameUICamera under GameUIRoot.
- GameUI event input contract now requires InputSystemUIInputModule and rejects StandaloneInputModule.
- GameUI canvas contract now renders as ScreenSpaceCamera through GameUICamera.

User-visible behavior:
- Opening GameUI now shows a scene camera named GameUICamera for viewing/capturing the UI shell.
- The EventSystem no longer reads UnityEngine.Input through StandaloneInputModule, so the Input System package Player Settings error is removed for this scene.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureGameUiSceneBuilder.BuildStep5 -logFile /private/tmp/warlinecapture-gameui-camera-input-fix-unity2.log
- rg -n "GameUICamera|InputSystemUIInputModule|StandaloneInputModule|m_RenderMode" Assets/Game/Scenes/GameUI.unity

Validation result:
- PASS: WARLINECAPTURE_GAMEUI_SCENE_STEP5_VALIDATED scene=Assets/Game/Scenes/GameUI.unity
- PASS: GameUI.unity contains GameUICamera.
- PASS: GameUI.unity contains InputSystemUIInputModule.
- PASS: GameUI.unity does not contain StandaloneInputModule.
- PASS: GameUICanvas serializes as ScreenSpaceCamera.

Known gaps:
- Step 6 content prefabs remain structural placeholders; this fix only addresses scene camera and input module setup.

Cross-lane impacts:
- None expected. The change is scoped to the new GameUI scene builder and regenerated GameUI scene. Legacy Game scene and legacy UI builders were not modified.

Next recommended task:
- Continue GameUI step 7: instantiate/bind the Step 6 shell content prefabs into the runtime shell regions and add a small scene smoke path for loading-to-menu presentation.
