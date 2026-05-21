# SCN-02 Main Menu Latest UI Workflow

Latest accepted direction: frame-first canvas conversion.

## Resume Point
- Use `Tools/UI/build_scn02_component_plates.py` as the current source of truth for SCN-02 layout generation.
- Use `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_menu_layout.json` as the generated layout manifest.
- Use `Design/VisualLockLayered/SCN-02_MainMenu/scn02_component_slot_report.json` as the generated safe-rect/slot report.
- Use `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs` to build/capture the Unity prefab from the manifest.

## Workflow
1. Keep generated frame assets in:
   - `Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets`
   - `Design/VisualLockLayered/SCN-02_MainMenu/component_plates_20260519/assets`
2. Build the frame-first layout:
   - `python3 Tools/UI/build_scn02_component_plates.py`
3. Validate the generated layout before Unity:
   - `python3 Tools/UI/validate_scn02_component_layout.py`
4. Sync layout/assets to the Unity workaround project:
   - `/Users/farhad/Projects/WarlineCapture-CodexUnity3`
5. Capture through Unity:
   - `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity3 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-framefirst-final-unity3.log`
6. Copy the capture/prefab/generated Unity sprites back to the main workspace.

## Current Capture
- Review image: `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_1672x941.png`
- Full capture: `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_3840x2160.png`
- Comparison image: `Design/AgentReports/Captures/SCN-02_MainMenu_ComponentCanvas_vs_Target_Comparison.png`

## Rules Learned
- Do not use baked composite `*_plate.png` panels as the final layout source.
- Use frames as backgrounds, then place icons, locks, meters, badges, art, and text as separate child layout entries.
- Every functional panel must declare a `safeRect`.
- Child images and live text must fit inside the panel `safeRect`.
- Active same-panel children must not overlap.
- Do not solve collisions by shrinking icons/text below the UI scale; adjust panel safe zones and child lanes.
- Diagnostic overlays are engineering-only and should not be shown as product captures.

## Known Gaps
- The frame-first approach improves editability and layout cleanliness, but source sprite style/proportions are still not pixel-perfect against the target mockup.
- Exact target match still requires better target-derived or regenerated frame assets.
