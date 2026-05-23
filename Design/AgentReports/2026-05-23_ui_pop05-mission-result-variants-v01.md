Lane: UI

Task: POP-05 mission result target-lock variants: victory, partial success, defeat/lost, withdrawn, and simulation resolved.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCapturePop05MissionResultSceneBuilder.cs
- Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/pop05_defeat_*.png(.meta)
- Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/pop05_partial_*.png(.meta)
- Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/pop05_withdrawn_*.png(.meta)
- Assets/Game/Art/UI/Generated/MissionResult/TargetLockV01/pop05_variant_*.png(.meta)
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_Victory_TargetLock.prefab(.meta)
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_Partial_TargetLock.prefab(.meta)
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_Defeat_TargetLock.prefab(.meta)
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_Withdrawn_TargetLock.prefab(.meta)
- Assets/Game/Prefabs/UI/Screens/Screen_POP05_MissionResult_Resolved_TargetLock.prefab(.meta)
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_Victory_TargetLock.unity(.meta)
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_Partial_TargetLock.unity(.meta)
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_Defeat_TargetLock.unity(.meta)
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_Withdrawn_TargetLock.unity(.meta)
- Assets/Game/Scenes/DesignTargets/POP05_MissionResult_Resolved_TargetLock.unity(.meta)
- Design/AgentReports/Captures/POP05_MissionResult_Victory_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/POP05_MissionResult_Partial_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/POP05_MissionResult_Defeat_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/POP05_MissionResult_Withdrawn_TargetLock_V01_2400x1080.png
- Design/AgentReports/Captures/POP05_MissionResult_Resolved_TargetLock_V01_2400x1080.png
- Design/AgentReports/2026-05-23_ui_pop05-mission-result-variants-v01.md

Contracts touched:
- POP-05 target-lock variant contract from Design/VisualLockLayered/POP-05_MissionResult/target_lock_variants_manifest.json.
- Reusable result-shell rule: shared layout with variant data for background, snapshot, title, star state, objectives, rewards, consequences, route label, and CTA.
- Existing UI route remains WarlineCaptureRoute.Match for design target screens.

User-visible behavior:
- Added separate target-lock scenes and prefabs for Victory, Partial Success, Defeat/Lost, Withdrawn, and Simulation Resolved.
- Defeat/Lost now uses failed-state text, red state coloring, failed objectives, retry operation CTA, reduced rewards, and negative consequences.
- Partial/Withdrawn/Resolved variants reuse the approved POP-05 shell while changing state-specific art, copy, stars, objective status, route note, and CTA.
- The long labels that clipped in the first capture pass were tightened: defeat CTA, adjust-loadout action, unresolved status, route notes, partial summary title, and long consequence values.

Validation run:
- /Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCapturePop05MissionResultSceneBuilder.CaptureAllVariants -logFile /private/tmp/warlinecapture-pop05-result-variants-v03.log
- Visual review of all five 2400x1080 captures.

Validation result:
- PASS: Unity2 batchmode completed successfully.
- PASS: All five variant captures generated.
- PASS: No obvious clipped CTA/action/route/objective/consequence text remains in the reviewed captures.

Known gaps:
- These are static target-lock scenes/prefabs. Runtime MissionResultPopupController still needs binding work if product wants the gameplay result popup to switch among Partial, Withdrawn, and Resolved at runtime.
- SimulationResolved intentionally reuses the partial-success shell because the target-lock manifest marks it as reusable until a dedicated target is needed.
- Some faint green/gold edge coloration is inherited from the supplied POP-05 chrome sprites.

Cross-lane impacts:
- Gameplay/data lane can now map result data to five visual states, but runtime popup state model may need extension beyond the current victory/defeat boolean.
- QA can compare the five captures against POP-05 references and flag only state-specific visual mismatches.

Next recommended task:
- Extend runtime result data/controller mapping so mission completion can select Victory, Partial Success, Defeat, Withdrawn, or Simulation Resolved and populate live values into the reusable POP-05 result shell.
