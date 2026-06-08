Lane:
UI

Task:
Replace `PREFAB-04_AssistantButton_Landscape_Target.png` with an AAA-quality target-lock mockup for the ARIA assistant button.

Files changed:
- Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png
- Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Style_ContactSheet.png
- Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_CleanLandscape_Notes.md
- Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Target_State_Manifest.json
- Design/AgentTasks/ui_current.md
- Design/AgentReports/2026-05-07_ui_prefab04-assistant-button-target-lock.md

Contracts touched:
- `PREFAB-04_AssistantButton`
- `Design/AssistantPanel_M01_Implementation_Contract.md`
- `Design/AssistantRuntime_M01_Wiring_Plan.md`
- `Design/Art_Asset_Requirements_Register.csv`

User-visible behavior:
The assistant button visual target now presents the ARIA entry point in an actual WarlineCapture match HUD/gameplay context instead of a flat state-board panel. The target shows a premium chrome ARIA button cluster with idle, recommendation, critical, takeover/control, and muted states, with waveform/radio identity marks and non-color-only state cues.

Validation run:
- Generated a new 1672x941 target-lock mockup using the built-in image generation path.
- Copied the selected generated image into `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Landscape_Target.png`.
- Opened and visually inspected the replacement target image.
- Opened and visually inspected accepted reference targets for style comparison: `SCN-08_RTSBattleHUD`, `PREFAB-05_AssistantPanel`, `POP-10_AssistantTakeover`, and `SCN-03_CommanderProfile`.
- Produced and inspected a side-by-side style contact sheet at `Design/VisualLock/PREFAB-04_AssistantButton/PREFAB-04_AssistantButton_Style_ContactSheet.png`.
- Confirmed image dimensions and pixel variance with Pillow.
- Validated `PREFAB-04_AssistantButton_Target_State_Manifest.json` with `python3 -m json.tool`.
- Checked `aria.icon.waveform` and `aria.button.state_set` rows in `Design/Art_Asset_Requirements_Register.csv`.

Validation result:
Passed. The replacement target is 1672x941, uses a visibly WarlineCapture match HUD/gameplay background, is not a wireframe or deterministic flat UI sheet, and keeps the assistant button as the first-read subject. The style contact sheet shows alignment with the accepted WarlineCapture family: dark military RTS chrome, cyan edge lighting, restrained amber accents, Oxanium-style typography, dense tactical HUD language, and compatible bevel/frame proportions. The five states are labeled and differentiated by shape/iconography as well as color. The asset-register rows remain `missing` / `not_reviewed` / `not_started`.

Known gaps:
- This is a visual target lock only; it is not a separated production layer pack.
- `aria.icon.waveform` and `aria.button.state_set` are still missing production assets.
- The current target includes state labels inside the mockup for review clarity; production Unity UI should use live TMP labels and separated reusable button/icon sprites.
- No Unity prefab implementation or runtime validation was part of this pass.

Cross-lane impacts:
PM and Art can now review a more realistic ARIA assistant button target before approving production asset extraction. UI should not mark the assistant button prefab or art-register rows complete until separated icon/state assets exist and are reviewed. Support/FTUE and Gameplay contracts are unchanged.

Next recommended task:
Generate or author the separated `aria_waveform_icon.png` and `aria_button_state_set.png` production assets from the approved target direction, then build `PREFAB-04_AssistantButton` as a reusable animated Unity prefab.
