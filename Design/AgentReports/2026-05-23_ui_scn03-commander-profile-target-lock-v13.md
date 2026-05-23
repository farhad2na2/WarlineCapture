Lane: UI

Task: SCN-03 Commander Profile target-lock canvas V13 cleanup pass

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V13_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn03-commander-profile-target-lock-v13.md

Contracts touched:
- CommanderProfile route target-lock screen prefab.
- SCN-03 DesignTargets scene.
- SCN-03 hit zones: Back, OpenArmory, EditId, Badges.

User-visible behavior:
- Rebuilt SCN-03 V13 with the overview title separated from the tab strip.
- Simplified the right-column Armory rows by pulling content inward and reducing row fill weight.
- Removed the dense framed reward chip in Profile Rewards and replaced it with simpler icon/text content.
- Tightened Account Snapshot content blocks so labels are less cramped against panel edges.
- Preserved V11/V12 major panel gutter improvements.

Validation run:
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v13.log
- Capture dimension check:
  sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V13_2400x1080.png

Validation result:
- Passed. Unity produced the V13 capture.
- Capture size is 2400x1080.
- Unity batchmode exited successfully.

Known gaps:
- Right column still does not match target quality; Armory rows remain visually weaker than the target.
- Account Snapshot is cleaner but still lacks the polished framed-cell structure of the mockup.
- Overview tabs are separated from the title now, but their chrome still does not feel exact.
- Recent History still has cramped row/action composition.
- Some generated frame sprites have bevel geometry that differs from the target, so code-only layout passes cannot fully fix the look.

Cross-lane impacts:
- None. This pass only touched SCN-03 UI assembly, scene/prefab output, capture, and report.

Next recommended task:
- If continuing SCN-03, focus V14 on replacing weak right-column row/cell treatment with better matched generated panel sprites rather than more coordinate-only layout edits.
