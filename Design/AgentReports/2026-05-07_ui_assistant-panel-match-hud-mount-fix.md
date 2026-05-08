Lane:
UI

Task:
Fix and resubmit the assistant HUD mount visual capture/readability validation.

Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Game/Prefabs/UI/Components/PREFAB-05_AssistantPanel.prefab
- Assets/Tests/Editor/WarlineCaptureUiAssistantPanelTests.cs
- Design/AgentTasks/ui_current.md
- Design/AgentReports/2026-05-07_ui_assistant-panel-match-hud-mount-fix.md

Contracts touched:
- `PREFAB-05_AssistantPanel`
- `Screen_MatchOverlay`
- `AssistantPanelController`
- `Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md`
- `Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md`

User-visible behavior:
The match HUD capture path now produces visible artifacts when run with a real graphics device. The assistant panel prefab root now uses a fixed centered 660x620 rect, so the panel stays inside `AssistantPanelDock` when opened from the ARIA entry button instead of expanding across core HUD surfaces.

Validation run:
- Diagnosed previous capture logs and confirmed the failed artifacts were generated under `-nographics` with `NullGfxDevice`.
- Added a capture guard that throws if visible UI capture is attempted while Unity is running with `GraphicsDeviceType.Null`.
- Unity batch prefab regeneration: `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantPanelPrefab`
- Unity capture without `-nographics`: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayAssistantValidationSet`
- Inspected capture artifacts with pixel variance checks and visual image review.
- Unity EditMode: `WarlineCaptureUiAssistantPanelTests`
- Unity EditMode: `WarlineCaptureUiAssistantPanelControllerTests`
- Unity EditMode: `WarlineCaptureUiMatchOverlayTests`
- `git diff --check` on the edited script, test, and regenerated assistant panel prefab.

Validation result:
Passed. `WarlineCaptureUiAssistantPanelTests` passed 7/7, including the new fixed-root regression test. `WarlineCaptureUiAssistantPanelControllerTests` passed 4/4. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Visible capture artifacts were produced at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`, `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`, `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`, and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`. The new artifacts are no longer flat gray; sampled pixel variance was non-zero across all four captures.

Known gaps:
- The capture commands must be run without `-nographics`; the helper now fails loudly under `NullGfxDevice` instead of producing misleading gray files.
- Open-panel captures prove the docked placeholder shell, but the assistant content is still placeholder data rather than live `WarlineCaptureAssistantService` recommendations.
- Show Me / Do It / Stop remain callback seams only and still do not execute gameplay typed intents, highlights, takeover ownership, or gameplay commands.
- No Android touch/device validation was run for the ARIA entry point in this pass.

Cross-lane impacts:
PM and QA/HCI can now review visible match HUD artifacts instead of blank gray captures. Support/FTUE can continue wiring `WarlineCaptureAssistantService` data into `AssistantPanelController`; this fix does not change the assistant runtime contract. Gameplay typed command ownership remains unchanged.

Next recommended task:
Implement or connect the first `WarlineCaptureAssistantService` / `M01AssistantRecommendationProvider` slice so the mounted assistant panel receives live M01 recommendation data instead of placeholder presentation data.
