Lane
UI

Task
SCN-01 Splash/Loading target-lock canvas: replace the incorrect temporary/generated splash logo with the approved Warline Capture logo source using the green-screen chroma workflow.

Files changed
- Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV01/scn01_brand_logo_lockup.png
- Assets/Game/Prefabs/UI/Screens/Screen_SCN01_SplashLoading_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN01_SplashLoading_TargetLock.unity
- Assets/Game/Scripts/Editor/WarlineCaptureScn01SplashLoadingSceneBuilder.cs
- Design/AgentReports/Captures/SCN01_SplashLoading_TargetLock_V04_2400x1080.png
- Design/VisualLockLayered/SCN-01_SplashLoading/layers/scn01_brand_logo_lockup.png

Contracts touched
- SCN-01 Splash/Loading VisualLockLayered target-lock canvas.
- Splash route prefab remains bound to WarlineCaptureRoute.Splash.
- Layer asset workflow uses green-source chroma removal, not mockup cropping and not imagegen alpha regeneration.

User-visible behavior
- Splash/loading screen now uses the approved Warline Capture logo lockup with the correct emblem, silver WARLINE text, gold CAPTURE text, and gold mark.
- The incorrect temporary constructed TMP logo and rejected old emblem are no longer used by the SCN-01 builder.

Validation run
- Chroma validation: scanned the SCN-01 logo layer and Unity asset copy for opaque #00ff00-style pixels.
- Unity batchmode: /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn01SplashLoadingSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn01-splash-v04.log

Validation result
- PASS: logo PNG has alpha, size 1983x793.
- PASS: opaque chroma-green pixel scan returned 0 for both source layer and Unity asset copy.
- PASS: Unity batchmode built the prefab and scene and captured SCN01_SplashLoading_TargetLock_V04_2400x1080.png successfully.

Known gaps
- V04 focuses on correcting the logo source/workflow. Further visual matching may still tune logo scale/position, command-system chip spacing, and loading-panel typography.

Cross-lane impacts
- None expected. This is UI-layer-only and does not change gameplay/runtime contracts outside the splash screen prefab/scene.

Next recommended task
- Review the V04 capture against SCN-01 target, then tune only layout/scale issues if needed. Keep the approved chroma-keyed logo asset as the source of truth.
