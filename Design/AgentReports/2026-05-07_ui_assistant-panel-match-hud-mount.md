Lane:
UI

Task:
Mount `AssistantPanelController` behind the accepted match HUD/app-shell assistant entry point.

Files changed:
- Assets/Game/Scripts/UI/Screens/AssistantPanelController.cs
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
- Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs
- Design/AgentTasks/ui_current.md
- Design/AgentReports/2026-05-07_ui_assistant-panel-match-hud-mount.md

Contracts touched:
- `PREFAB-05_AssistantPanel`
- `AssistantPanelController`
- `Screen_MatchOverlay`
- `Design/AssistantPanel_M01_Implementation_Contract.md`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`

User-visible behavior:
The match HUD now has a live `ARIA` assistant entry button on the left rail between the objective tracker and threat feed. Tapping it toggles a placeholder-bound assistant panel through `AssistantPanelController` and `AssistantPanelView`. Show Me, Do It, and Stop remain UI callback seams only; this pass does not execute gameplay typed intents.

Validation run:
- Unity batch prefab regeneration: `WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen`
- Unity EditMode: `WarlineCaptureUiAssistantPanelTests`
- Unity EditMode: `WarlineCaptureUiAssistantPanelControllerTests`
- Unity EditMode: `WarlineCaptureUiMatchOverlayTests`
- Unity capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayVisual`
- Unity capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayVisual20x9`

Validation result:
Passed. `WarlineCaptureUiAssistantPanelTests` passed 6/6. `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Captures were produced at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png` and `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`.

Known gaps:
- `PREFAB-04_AssistantButton` is still not a reusable Unity prefab; this pass uses a live ARIA match-HUD button as the accepted temporary entry point.
- The placeholder recommendation is contract-safe but not produced by `WarlineCaptureAssistantService`.
- Show Me / Do It / Stop do not execute typed intents, highlights, takeover ownership, or gameplay commands yet.
- No PlayMode or Android touch validation was run for the mounted assistant entry point.

Cross-lane impacts:
Support/FTUE can now mount future `WarlineCaptureAssistantService` data into the real match HUD through `AssistantPanelController`. Gameplay remains the owner of typed selection, move, attack, and command execution. QA/HCI can see the entry-point placement in the generated match overlay captures, but should wait for runtime assistant service wiring before judging tutorial behavior.

Next recommended task:
Implement or connect the first `WarlineCaptureAssistantService` / `M01AssistantRecommendationProvider` slice so the mounted panel receives live M01 recommendation data instead of the placeholder.
