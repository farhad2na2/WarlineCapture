Lane
UI

Task
Build SCN-03 Commander Profile as a separate target-lock Unity canvas scene using the SCN-03 VisualLockLayered pack and reusable layered UI builder workflow.

Files changed
- Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V02_2400x1080.png

Contracts touched
- SCN-03 Commander Profile VisualLockLayered target-lock canvas.
- WarlineCaptureRoute.CommanderProfile route binding on the target-lock screen controller.
- SCN-03 route affordances: Back, Edit ID, Badges, Open Armory hit zones.

User-visible behavior
- Added a standalone Commander Profile target-lock screen with header resources, commander identity/portrait, overview tabs/stats, reward track, recent history, armory summary CTA, profile rewards, account snapshot, and route strip.
- Text, counters, labels, reward states, and route labels are live TMP elements, not baked into the target reference.
- Uses SCN-03 background/chrome/portrait/icon layers plus the corrected approved shared Warline Capture logo asset.

Validation run
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v02.log
- Chroma scan over Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/*.png.
- Capture dimension check with sips.

Validation result
- PASS: Unity batchmode built prefab and scene and captured SCN03_CommanderProfile_TargetLock_V02_2400x1080.png.
- PASS: capture dimensions are 2400x1080.
- PASS: opaque pure chroma-green scan returned 0 files.

Known gaps
- V02 is a first implementation pass, not pixel-perfect final target match.
- Remaining likely polish: header/brand scale, some generated chrome green-tinted edge accents, profile reward chip density, account snapshot frame strength, and exact panel-to-target spacing.

Cross-lane impacts
- None expected. This is isolated to UI target-lock scene/prefab/assets and does not change gameplay systems.

Next recommended task
- Review SCN-03 V02 capture against the target, then do a focused V03 layout pass on header scale, panel density, and right-column readability if needed.
