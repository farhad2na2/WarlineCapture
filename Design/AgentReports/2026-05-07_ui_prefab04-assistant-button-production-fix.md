Lane:
UI

Task:
PM fix pass for P0 PREFAB-04 assistant button production readability and AAA HUD lock.

Files changed:
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_set.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_idle.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_recommendation.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_critical.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_takeover.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_muted.png
- Assets/Game/Prefabs/UI/Components/PREFAB-04_AssistantButton.prefab
- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantButtonTests.cs
- Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs
- Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-production-fix.md

Contracts touched:
- Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md: preserved typed-intent boundary; no gameplay execution logic added to UI.
- Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md: retained state-ready assistant entry for later runtime binding.
- Design/WarlineCapture_Art_Asset_Requirements_Register.csv: no status advancement; PM review remains required before approving generated assistant assets.

User-visible behavior:
The closed ARIA HUD entry is now wider and more readable in 16:9 and 20:9. The waveform, ARIA label, live state text, and right-side cue have separated visual zones so they no longer compete or crowd each other. The open assistant panel behavior remains unchanged.

Validation run:
- Regenerated assistant state art with larger 240x128 state cells and separated text/cue lanes.
- Unity builder: `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantButtonPrefab`
- Unity builder: `WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen`
- EditMode: `WarlineCaptureUiAssistantButtonTests`
- EditMode: `WarlineCaptureUiMatchOverlayTests`
- Graphics capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayAssistantValidationSet`
- `git diff --check`

Validation result:
Passed. `WarlineCaptureUiAssistantButtonTests` passed 3/3. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Captures were regenerated at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`, `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`, `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`, and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`. `git diff --check` passed after removing Unity-generated trailing prefab whitespace.

Known gaps:
PM acceptance is still required for the revised assistant button visual lock. Art-register rows remain unapproved. Runtime recommendation state is still not wired into `AssistantButtonView.SetState(...)`; this pass only fixes the reusable prefab, assets, and HUD mount.

Cross-lane impacts:
Support/FTUE can still bind idle, recommendation, critical, takeover/control, and muted states through the existing `AssistantButtonView` API after PM accepts the visual. Gameplay behavior is unchanged.

Next recommended task:
PM review of the fixed 16:9 and 20:9 closed/open captures, then Support/FTUE runtime binding of assistant recommendation state through typed presentation data.
