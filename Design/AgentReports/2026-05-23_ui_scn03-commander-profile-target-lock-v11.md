Lane: UI

Task: SCN-03 Commander Profile target-lock canvas V11 panel spacing cleanup

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V11_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn03-commander-profile-target-lock-v11.md

Contracts touched:
- CommanderProfile route target-lock screen prefab.
- SCN-03 DesignTargets scene.
- SCN-03 hit zones: Back, OpenArmory, EditId, Badges.

User-visible behavior:
- Rebuilt SCN-03 V11 with larger gutters between major panels.
- Moved the left identity, center stack, and right stack apart so their chrome no longer reads as touching.
- Reduced center and right panel widths slightly to create clean spacing.
- Shrunk and padded overview stat cells, armory cells, history rows, and account snapshot cells.
- Removed heavy internal subframe strokes from repeated cells and replaced them with subtle neutral backing.

Validation run:
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v11.log
- Capture dimension check:
  sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V11_2400x1080.png

Validation result:
- Passed. Unity produced the V11 capture.
- Capture size is 2400x1080.
- Unity batchmode exited successfully.

Known gaps:
- V11 is cleaner, but still not a pixel-perfect match to the target mockup.
- Armory/Profile Rewards/Account Snapshot still need exact frame geometry polish if this screen is pushed further.
- Some generated frame sprites still have bevel shapes that differ from the target.

Cross-lane impacts:
- None. This pass only touched SCN-03 UI assembly, scene/prefab output, capture, and report.

Next recommended task:
- If continuing SCN-03, use V11 as the current baseline and focus V12 on right-column exact frame selection and inner content alignment.
