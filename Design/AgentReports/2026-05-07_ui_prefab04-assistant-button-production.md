Lane:
UI

Task:
P0 PREFAB-04 assistant button production assets and reusable animated prefab.

Files changed:
- Assets/Game/Art/UI/Generated/Assistant/Icons/aria_waveform_icon.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_set.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_idle.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_recommendation.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_critical.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_takeover.png
- Assets/Game/Art/UI/Generated/Assistant/Buttons/aria_button_state_muted.png
- Assets/Game/Art/UI/Generated/Assistant/Atlases/Assistant_UI.spriteatlas
- Assets/Game/Prefabs/UI/Components/PREFAB-04_AssistantButton.prefab
- Assets/Game/Prefabs/UI/Screens/Screen_MatchOverlay.prefab
- Assets/Game/Scripts/UI/Components/AssistantButtonView.cs
- Assets/Game/Scripts/Editor/WarlineCaptureUiPhase1PrefabBuilder.cs
- Assets/Tests/Editor/WarlineCaptureUiAssistantButtonTests.cs
- Assets/Tests/Editor/WarlineCaptureUiMatchOverlayTests.cs

Contracts touched:
- Design/WarlineCapture_AssistantPanel_M01_Implementation_Contract.md: preserved typed-intent boundary; no gameplay execution logic added to UI.
- Design/WarlineCapture_AssistantRuntime_M01_Wiring_Plan.md: prefab now exposes visual states for future runtime binding.
- Design/WarlineCapture_Art_Asset_Requirements_Register.csv: referenced `aria.icon.waveform` and `aria.button.state_set`; rows intentionally left `missing` / `not_reviewed` / `not_started` pending PM approval.

User-visible behavior:
The match HUD ARIA entry now uses `PREFAB-04_AssistantButton` instead of the old inline text-only chrome button. The reusable prefab has transparent separated art, live TMP labels, animated button transition support, and five visual states: idle, recommendation, critical, takeover/control, and muted.

Validation run:
- Generated/inspected assistant PNG dimensions, alpha, and pixel variance with Pillow.
- Unity builder: `WarlineCaptureUiPhase1PrefabBuilder.BuildAssistantButtonPrefab`
- Unity builder: `WarlineCaptureUiPhase1PrefabBuilder.BuildMatchOverlayScreen`
- EditMode: `WarlineCaptureUiAssistantButtonTests`
- EditMode: `WarlineCaptureUiMatchOverlayTests`
- Graphics capture: `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayAssistantValidationSet`
- `git diff --check`

Validation result:
Passed. Asset checks confirm `aria_waveform_icon.png` is 256x256 with alpha and pixel variance, and `aria_button_state_set.png` is 960x128 with alpha and pixel variance. `WarlineCaptureUiAssistantButtonTests` passed 3/3. `WarlineCaptureUiMatchOverlayTests` passed 18/18. Captures were produced at `/private/tmp/warlinecapture-screen-matchoverlay-capture.png`, `/private/tmp/warlinecapture-screen-matchoverlay-capture-20x9.png`, `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture.png`, and `/private/tmp/warlinecapture-screen-matchoverlay-assistant-open-capture-20x9.png`. `git diff --check` passed.

Known gaps:
The art-register rows are not advanced because PM approval is still required for separated production assets and runtime wiring. Runtime recommendation data does not yet drive the assistant button state; the prefab exposes state support for the next wiring pass.

Cross-lane impacts:
Support/FTUE can bind recommendation, critical, takeover/control, and muted states through `AssistantButtonView.SetState(...)` without executing gameplay from UI. Gameplay contracts remain unchanged.

Next recommended task:
PM review of `PREFAB-04_AssistantButton` production assets and prefab, then Support/FTUE can wire runtime assistant recommendation state into the button via typed assistant presentation data.
