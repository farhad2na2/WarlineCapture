Lane: UI

Task: SCN-03 Commander Profile target-lock canvas V07 cleanup pass

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/*.png
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V07_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn03-commander-profile-target-lock-v07.md

Contracts touched:
- CommanderProfile route target-lock screen prefab.
- SCN-03 DesignTargets scene.
- SCN-03 hit zones: Back, OpenArmory, EditId, Badges.

User-visible behavior:
- Rebuilt the SCN-03 Commander Profile screen with cleaner chrome and less green edge contamination.
- Enlarged the header brand and tightened the header title/subtitle placement.
- Strengthened panel interior fills so text reads more like the target mockup.
- Reduced added internal stroke weight to avoid the previous heavy, busy panel look.
- Kept the screen as layered Unity UI with live text and hit zones, not a baked full-screen image.

Validation run:
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v07.log
- Capture dimension check:
  sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V07_2400x1080.png
- Strong green artifact scan across Assets/Game/Art/UI/Generated/CommanderProfile/TargetLockV01/*.png.

Validation result:
- Passed. Unity produced the V07 capture.
- Capture size is 2400x1080.
- Strong green artifact scan result: 0 files.

Known gaps:
- V07 is cleaner than V04, but still not a perfect pixel match to the target.
- Some generated panel sprites still have different bevel geometry and corner detail than the target mockup.
- The profile rewards and account snapshot areas can still be tightened if exact target match is required.
- The overview tabs and reward-track node sizing are acceptable but still not identical to the reference.

Cross-lane impacts:
- None. This pass only touched SCN-03 UI screen assembly, SCN-03 generated UI assets, scene/prefab output, capture, and report.

Next recommended task:
- If continuing SCN-03, do a focused V08 on the right-column lower panels and central overview tab/reward sizing. If moving on, use V07 as the current clean baseline.
