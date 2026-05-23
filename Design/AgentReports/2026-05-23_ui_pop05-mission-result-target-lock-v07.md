Lane: UI

Task: Iterate POP-05 Mission Result target-lock conversion, prioritizing the Consequences panel.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCapturePop05MissionResultSceneBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_TargetLock.prefab
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_TargetLock.unity
- Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V07_2400x1080.png
- Design/AgentReports/2026-05-23_ui_pop05-mission-result-target-lock-v07.md

Contracts touched:
- No production runtime contracts changed.
- POP-05 remains isolated to the design-target scene/prefab path.
- Continued using the shared layered UI builder utility and Unity batch capture workflow.

User-visible behavior:
- Consequences panel now uses a cleaner target-style list instead of exposing the generated frame asset's boxed row chrome.
- Consequence rows now use direct right-aligned values, no extra middle marker column, tighter target values, and clearer icon/text/value alignment.
- The latest capture is Design/AgentReports/Captures/POP05_MissionResult_TargetLock_V07_2400x1080.png.

Validation run:
- Unity 6000.4.0f1 batchmode in WarlineCapture-CodexUnity2:
  /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCapturePop05MissionResultSceneBuilder.CaptureScene -logFile /private/tmp/warlinecapture-pop05-result-v07.log

Validation result:
- Passed. Unity built and captured V07 successfully.

Known gaps:
- The Consequences panel is cleaner, but the generated panel frame still contains baked internal row styling underneath; the builder masks most of it, but a perfect target match would require a clean consequences panel frame asset without baked row boxes.
- Other POP-05 areas were not retuned in this pass except where needed by the capture rebuild.

Cross-lane impacts:
- None. This remains design-target-only.

Next recommended task:
- If further matching is needed, request or generate a clean POP-05 consequences panel frame with only outer chrome and no baked row containers, then remove the builder-side masking workaround.
