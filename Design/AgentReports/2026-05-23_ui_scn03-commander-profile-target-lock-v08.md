Lane: UI

Task: SCN-03 Commander Profile target-lock canvas V08 label containment pass

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureScn03CommanderProfileSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_SCN03_CommanderProfile_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/SCN03_CommanderProfile_TargetLock.unity
- Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V08_2400x1080.png
- Design/AgentReports/2026-05-23_ui_scn03-commander-profile-target-lock-v08.md

Contracts touched:
- CommanderProfile route target-lock screen prefab.
- SCN-03 DesignTargets scene.
- SCN-03 hit zones: Back, OpenArmory, EditId, Badges.

User-visible behavior:
- Rebuilt SCN-03 V08 with a containment pass for labels and framed controls.
- Moved overview tab labels into the visible tab interiors.
- Tightened stat, roster, history, account snapshot, route strip, and CTA text rectangles so labels no longer ride panel borders.
- Reduced a few font sizes where needed to keep labels inside their frames.
- Preserved the layered Unity UI structure with live text and hit zones.

Validation run:
- Unity batchmode capture in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn03CommanderProfileSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-scn03-profile-v08.log
- Capture dimension check:
  sips -g pixelWidth -g pixelHeight Design/AgentReports/Captures/SCN03_CommanderProfile_TargetLock_V08_2400x1080.png

Validation result:
- Passed. Unity produced the V08 capture.
- Capture size is 2400x1080.
- Unity batchmode exited successfully.

Known gaps:
- V08 fixes the most visible label containment problems, but the screen is still not a perfect target match.
- Some generated panel frame geometry still differs from the target mockup.
- Profile Rewards still has dense reward-copy text; it is contained but visually small.
- Further polish should focus on exact panel sprite selection and reducing right-column density.

Cross-lane impacts:
- None. This pass only touched SCN-03 UI screen assembly, scene/prefab output, capture, and report.

Next recommended task:
- If continuing SCN-03, do V09 focused on exact panel geometry and right-column density, not generic label positioning.
