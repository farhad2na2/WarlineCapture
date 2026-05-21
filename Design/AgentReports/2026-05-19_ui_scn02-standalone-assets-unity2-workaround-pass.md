Lane:
UI

Task:
SCN-02 main menu standalone asset import, canvas build, and Unity2 licensing-workaround validation pass.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Tools/UI/prepare_scn02_standalone_assets.py`
- `Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/`
- `Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01/`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_LayerCanvasTest.prefab`
- `Design/AgentReports/Captures/SCN-02_MainMenu_LayerCanvasTest_3840x2160.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_LayerCanvasTest_1672x941.png`
- `Design/AgentReports/Captures/SCN-02_MainMenu_StandaloneCanvas_vs_Target_Comparison.png`
- `Design/AgentReports/2026-05-19_ui_scn02-standalone-assets-unity2-workaround-pass.md`

Contracts touched:
- SCN-02 Main Menu visual-lock implementation path only.
- Unity validation used the documented UI lane workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- Runtime UI assets now resolve only from `Design/VisualLockLayered/SCN-02_MainMenu/imagegen_standalone_20260519/assets`; old layer sheets, target-reference crops, comparison images, contact sheets, and previous generated folders are not fallback sources in the builder.

User-visible behavior:
- The layer-canvas test prefab renders SCN-02 using the newly generated standalone sprites for the header, settings, logo, profile, nav rows, cards, operation rows, deploy button, icons, and background.
- The generated capture is materially cleaner than the previous mixed-asset pass, with no old generated header source selected.

Validation run:
- Synced the SCN-02 standalone asset folder and builder into `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- Removed stale `Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01` output in the UI workspace before rebuilding.
- Ran:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-standalone-canvas-unity2.log`
- Ran a second placement-compensation pass:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-standalone-canvas-unity2-pass2.log`
- Ran additional focused geometry passes:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-standalone-canvas-unity2-pass3.log`
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-standalone-canvas-unity2-pass4.log`
- Ran logo backing and top-resource centering correction:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-standalone-canvas-unity2-pass5.log`
- Ran background/world-map placement correction:
  `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.CaptureLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-background-pass2.log`
- Ran:
  `python3 Tools/UI/compare_ui_capture_to_target.py --target Design/VisualLockLayered/SCN-02_MainMenu/reference/SCN-02_MainMenu_Landscape_Target.png --capture Design/AgentReports/Captures/SCN-02_MainMenu_LayerCanvasTest_3840x2160.png --out Design/AgentReports/Captures/SCN-02_MainMenu_StandaloneCanvas_vs_Target_Comparison.png --label "SCN-02 standalone canvas vs target"`
- Ran `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs Tools/UI/prepare_scn02_standalone_assets.py`.
- Ran scene-wide lookup warning scan on `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`.

Validation result:
- Unity2 workaround run passed. The final log still contains initial Unity licensing startup noise, but the versioned `LicenseClient-farhad-6000.4.0` connection succeeded, the prefab built, the 3840x2160 capture was written, and Unity exited successfully.
- Capture: `Design/AgentReports/Captures/SCN-02_MainMenu_LayerCanvasTest_3840x2160.png`
- Review size capture: `Design/AgentReports/Captures/SCN-02_MainMenu_LayerCanvasTest_1672x941.png`
- Comparison: `Design/AgentReports/Captures/SCN-02_MainMenu_StandaloneCanvas_vs_Target_Comparison.png`
- Comparison MSE: `575.95`.
- `git diff --check` passed.
- Scene-wide lookup warning scan returned no matches.

Known gaps:
- Not yet a perfect target lock. The logo backing now uses the correct standalone asymmetric panel and top-resource icon/text groups are better centered. The background/world-map is now rendered through a full-screen mask with the map image shifted downward and lightly scaled so continents sit more visibly under the panels. Remaining mismatches are mostly UI-owned geometry and typography: mode-card spacing is closer but still not exact, operation warning row styling is heavier than target, and deploy CTA frame/chevrons still differ from the target artwork even after placement correction.
- Art-owned risk remains only if PM requires exact frame silhouettes beyond what the current standalone generated sprites provide; current pass proves the sprites are usable, but layout still needs refinement.

Cross-lane impacts:
- No Gameplay, QA/HCI, Support/FTUE, or Art/Atlas source docs were modified.
- PM/QA can review the current capture, but UI should not claim SCN-02 target-lock complete yet.

Next recommended task:
Do one focused UI geometry pass against the 1672x941 target: widen and reposition the three mode cards, retune top/header/logo placement, align operation warning rows/meters, and tighten deploy CTA to the reference before asking PM/QA for target-lock acceptance.
