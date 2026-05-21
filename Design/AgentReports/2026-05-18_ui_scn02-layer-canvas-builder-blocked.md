Status: Superseded by `Design/AgentReports/2026-05-18_ui_scn02-layer-canvas-builder-test-pass.md`.

Lane: UI

Task: SCN-02 Main Menu layer-to-canvas editor builder, first screen test.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Assets/Tests/Editor/WarlineCaptureScn02LayerCanvasBuilderTests.cs`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_layers_3840_request.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_target_lock_3840.md`
- `Design/ChatGPT/SCN02_MainMenu_SourceAssets_Batch01/*`

Contracts touched:
- New SCN-02 layer request contract uses a 3840x2160 top-left coordinate space and separate source asset IDs.
- New editor menu item: `WarlineCapture/Design/SCN-02/Build Layer Canvas Test`.
- New generated output targets:
  - `Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01`
  - `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_LayerCanvasTest.prefab`

User-visible behavior:
- No runtime behavior changed yet.
- Current `Screen_MainMenu.prefab` was intentionally not modified.
- The new builder is isolated and intended to create a standalone layer-canvas test prefab from the approved source asset batch.

Validation run:
- `python3 -m json.tool Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs Assets/Tests/Editor/WarlineCaptureScn02LayerCanvasBuilderTests.cs Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_layers_3840_request.md`
- Unity batch executeMethod in `/Users/farhad/Projects/WarlineCapture`
- Unity batch executeMethod in `/Users/farhad/Projects/WarlineCapture-CodexUnity1`
- `dotnet build Assembly-CSharp-Editor.csproj --no-restore -v:minimal`

Validation result:
- JSON validation passed.
- `git diff --check` passed.
- Unity did not reach the builder method. Main workspace first failed under sandbox Package Manager IPC, then outside sandbox hit Unity Licensing Client protocol failures.
- CodexUnity1 also hit Unity Licensing Client protocol failures before executing the builder.
- `dotnet build` did not isolate this editor script; it failed in package/dependency compilation (`Unity.RenderPipelines.Core.Runtime`) and missing dependent generated DLLs, not in the new SCN-02 files.
- Later follow-up used the documented UI workspace workaround in `/Users/farhad/Projects/WarlineCapture-CodexUnity2`; the builder and focused EditMode test passed there. This earlier blocker report is retained only as history.

Known gaps:
- `Screen_MainMenu_LayerCanvasTest.prefab` has not been generated yet because Unity is blocked before `WarlineCaptureScn02LayerCanvasBuilder.BuildLayerCanvasTest` executes.
- Sprite import metadata for `SourceAssetsBatch01` has not been produced yet for the same reason.
- The first builder pass covers approved Batch01 chrome/source pieces plus background fallback; missing card/profile/resource icon layers still need additional source asset batches.

Cross-lane impacts:
- Art/source lane can continue generating more individual SCN-02 layers using `layer_request_3840.json`.
- UI lane can continue once Unity licensing/headless execution is restored.
- No current prefab or gameplay route was overwritten.

Next recommended task:
- Fix or route around Unity 6000.4.0f1 headless licensing (`Unsupported protocol version '1.18.1'` / missing `com.unity.editor.headless`) so the new SCN-02 builder can execute and emit the test prefab.
