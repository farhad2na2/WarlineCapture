Lane: UI

Task: Build SCN-01 Splash / Loading screen from the layered target lock.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureScn01SplashLoadingSceneBuilder.cs
- Assets/Game/Scripts/Editor/WarlineCaptureScn01SplashLoadingSceneBuilder.cs.meta
- Assets/Game/Art/UI/Generated/SplashLoading/TargetLockV01/
- Assets/Game/Prefabs/UI/Screens/Screen_SCN01_SplashLoading_TargetLock.prefab
- Assets/Game/Prefabs/UI/Screens/Screen_SCN01_SplashLoading_TargetLock.prefab.meta
- Assets/Game/Scenes/DesignTargets/SCN01_SplashLoading_TargetLock.unity
- Assets/Game/Scenes/DesignTargets/SCN01_SplashLoading_TargetLock.unity.meta
- Design/AgentReports/Captures/SCN01_SplashLoading_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/SCN01_SplashLoading_TargetLock_V02_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn01-splash-loading-logo-blocker.md

Contracts touched:
- Added a design-target-only builder/prefab/scene path for SCN-01 SplashLoading.
- Did not modify production splash runtime flow.

User-visible behavior:
- A first-pass splash/loading canvas exists with layered background, outer frame, loading panel, progress bar, live status/percent/tip text, and bottom loading status chip.
- The brand/logo block is not target-matched because the active layer pack does not contain the approved isolated SCN-01 brand lockup.

Validation run:
- Unity 6000.4.0f1 batchmode in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn01SplashLoadingSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn01-splash-v02.log

Validation result:
- Passed for build/capture.
- Visual result is blocked for approval because the logo is a temporary live TMP/shield substitute, not the target mockup logo.

Known gaps:
- Exact blocker: missing approved isolated SCN-01 brand lockup PNG matching the target logo.
- Missing file/report/command: no approved `scn01_brand_logo_lockup.png` or equivalent exists under Design/VisualLockLayered/SCN-01_SplashLoading/layers.
- The SCN-01 README explicitly says the brand/logo layer is pending fresh isolated approval and must not be cropped from target mockups or reused from SCN-02.

Cross-lane impacts:
- Design/Art lane must provide or generate the approved isolated SCN-01 logo layer.
- UI lane can continue layout implementation after the logo asset exists.

Next recommended task:
- Generate/approve a transparent-background SCN-01 Warline Capture brand lockup layer matching the splash target, then replace the temporary TMP/shield brand block in WarlineCaptureScn01SplashLoadingSceneBuilder.
