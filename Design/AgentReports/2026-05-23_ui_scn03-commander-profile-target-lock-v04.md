Lane: UI

Task: SCN-03 Commander Profile target-lock canvas V04 next pass

Files changed:
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V04_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn03-commander-profile-target-lock-v04.md

Contracts touched:
- CommanderProfile route target-lock screen prefab.
- SCN-03 DesignTargets scene.
- SCN-03 UI hit zones: Back, OpenArmory, EditId, Badges.

User-visible behavior:
- Built the V04 Commander Profile screen pass with the corrected shared Warline Capture logo source.
- Enlarged and tightened the header brand placement.
- Improved overview tab alignment and reduced panel background bleed.
- Added stronger dark backing to the main content panels so live text and generated frames read more cleanly.
- Preserved interactive canvas structure instead of baking the full mockup as one background.

Validation run:
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v04.log
- Capture dimension check:
  sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V04_2400x1080.png
- Chroma hygiene scan across Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/*.png.

Validation result:
- Passed. Unity produced the V04 capture.
- Capture size is 2400x1080.
- Opaque chroma-green asset remnants found: 0.

Known gaps:
- V04 is cleaner, but not a final pixel match to the SCN-03 target.
- Some generated frame assets still contain green-tinted accent styling that differs from the target.
- The right-side account/profile stack and overview stat panels can still be tuned for closer section centering and target spacing.
- Header can still be target-tuned if exact logo and resource-section proportions are required.

Cross-lane impacts:
- None. This pass only touched UI screen assembly, generated UI assets, capture output, and report files.

Next recommended task:
- If SCN-03 needs further polish, do a focused V05 pass on right-column section centering, overview stat tile spacing, and header proportions using the same V04 asset set.
