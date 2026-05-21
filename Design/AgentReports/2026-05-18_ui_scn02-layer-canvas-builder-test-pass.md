Lane: UI

Task: SCN-02 Main Menu layer-to-canvas editor builder, first isolated screen test.

Files changed:
- `Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs`
- `Assets/Tests/Editor/WarlineCaptureScn02LayerCanvasBuilderTests.cs`
- `Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01/*`
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_LayerCanvasTest.prefab`
- `Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`
- `Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_layers_3840_request.md`
- `Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_target_lock_3840.md`
- `Design/ChatGPT/SCN02_MainMenu_SourceAssets_Batch01/*`
- `Design/ChatGPT/SCN02_MainMenu_Layers_3840/*`
- `Design/AgentReports/2026-05-18_ui_scn02-layer-canvas-builder-blocked.md`
- `Design/AgentReports/2026-05-18_ui_scn02-layer-canvas-builder-test-pass.md`

Contracts touched:
- Added SCN-02 layer request contract at `Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`, using 3840x2160 top-left coordinate space and explicit source asset IDs.
- Added editor menu item `WarlineCapture/Design/SCN-02/Build Layer Canvas Test`.
- Added generated output target `Assets/Game/Art/UI/Generated/MainMenu/SourceAssetsBatch01`.
- Added isolated prefab target `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu_LayerCanvasTest.prefab`.

User-visible behavior:
- No shipped runtime screen changed.
- `Assets/Game/Prefabs/UI/Screens/Screen_MainMenu.prefab` was intentionally not modified.
- UI now has a tested editor-builder path that converts approved SCN-02 source layer assets into alpha sprites and assembles a standalone layer-canvas test prefab.

Validation run:
- `python3 -m json.tool Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json`
- `git diff --check -- Assets/Game/Scripts/Editor/WarlineCaptureScn02LayerCanvasBuilder.cs Assets/Tests/Editor/WarlineCaptureScn02LayerCanvasBuilderTests.cs Design/VisualLockLayered/SCN-02_MainMenu/layer_request_3840.json Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_layers_3840_request.md Design/VisualLockLayered/SCN-02_MainMenu/prompts/scn02_main_menu_target_lock_3840.md`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureScn02LayerCanvasBuilder.BuildLayerCanvasTest -logFile /private/tmp/warlinecapture-scn02-layer-builder-codexunity2.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter WarlineCaptureScn02LayerCanvasBuilderTests -testResults /private/tmp/warlinecapture-scn02-layer-builder-tests.xml -logFile /private/tmp/warlinecapture-scn02-layer-builder-tests.log`

Validation result:
- JSON validation passed.
- `git diff --check` passed.
- Unity builder passed in the documented UI validation workspace `/Users/farhad/Projects/WarlineCapture-CodexUnity2`.
- Focused EditMode validation passed: `WarlineCaptureScn02LayerCanvasBuilderTests.BuildLayerCanvasTest_CreatesPrefabAndAlphaSprites`, 1/1 passed.
- Earlier Unity licensing blocker report is superseded by this pass; the documented workaround was to use the UI workspace and rerun batchmode there after clearing stale licensing clients.

Known gaps:
- This is an isolated converter/test-prefab pass, not a target-lock completion pass.
- The current runtime `Screen_MainMenu.prefab` still needs the accepted v2 placement pass and proof captures before PM/QA visual review.
- Batch01 does not provide every final SCN-02 layer. Profile/card art, several icons, and fine-grained production panel parts still need either accepted manifest layers or additional source asset batches before the editor tool can build the full production canvas.
- The builder currently covers top bar, shell/background, left-nav row, deploy button, settings gear, and live text placement. It does not yet ingest a full layer manifest with every card/profile/resource sublayer.

Cross-lane impacts:
- PM can treat the previous UI licensing blocker as cleared for this task path.
- Art/Atlas can continue delivering cleaner SCN-02 source layers against the 3840x2160 request contract.
- UI can continue by expanding the builder from Batch01 into the full accepted manifest/layer set, then comparing captures region by region.
- No gameplay route, data binding, or shipped main menu prefab was overwritten.

Next recommended task:
- Extend `WarlineCaptureScn02LayerCanvasBuilder` to ingest the full accepted SCN-02 v2 manifest/layer set, generate the production `Screen_MainMenu.prefab` only after the isolated test prefab matches the target structure, then run fresh 16:9 and 20:9 captures with comparison scores.
