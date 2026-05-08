Lane: Gameplay
Task: P0 M01 sprite-renderer close tactical capture fix
Files changed:
- Assets/Game/Scripts/Editor/WarlineCaptureM01SpriteRendererCaptureBuilder.cs
- Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png
- Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_SpriteRendererCapture.unity
- Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md
Contracts touched:
- Capture evidence builder only; runtime `MissionRuntimeSpritePresenter`, presenter state mapping, `MissionRuntimeSpriteRendererSystem`, legacy `Model` suppression, and `vfx.unit.destroyed.small` destroyed-feedback contract were preserved.
- Close tactical evidence now frames `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point` together using bounds derived from their tactical anchors and sprite extents.
- Capture path uses explicit transparent texture-backed quads/materials for evidence rendering so transient editor-created sprite serialization cannot reintroduce map-fragment rectangles.
User-visible behavior:
- `M01_SpriteRenderer_CloseCapture.png` now shows the command/decor proxy, player rifle squad, and hostile patrol fully visible on the M01 tactical ground.
- The hostile patrol is no longer clipped at the right edge.
- Current AI-generated unit/building/tactical-map sprites remain review assets, not final approved atlas art.
Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -executeMethod WarlineCaptureM01SpriteRendererCaptureBuilder.BuildAndCapture -logFile /private/tmp/warlinecapture-m01-sprite-renderer-capture-fix.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity2 -runTests -testPlatform EditMode -testFilter Chapter01M01SpriteRendererTests -testResults /private/tmp/warlinecapture-m01-sprite-renderer-capture-fix-tests.xml -logFile /private/tmp/warlinecapture-m01-sprite-renderer-capture-fix-tests.log`
- Manual inspection of `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`.
- Runtime lookup scan: `rg -n "FindObject|FindObjects|FindFirstObjectByType|FindAnyObjectByType|GameObject.Find|Transform.Find|GetComponentInChildren" Assets/Game/Scripts/Components/MissionRuntimeComponents.cs Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs`
Validation result:
- Capture builder completed and logged `WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURED`.
- Capture builder logged intended texture-backed quads: player `infantry_squad` 299x255 scale 0.070, enemy `infantry_squad` 299x255 scale 0.070, and command `command_building` 401x376 scale 0.140.
- Manual PNG inspection passed: no map-fragment rectangles; hostile patrol is fully visible; player squad and command/decor proxy are visible and inspectable.
- `Chapter01M01SpriteRendererTests`: Passed 4/4.
- No banned runtime scene lookup calls were found in the touched gameplay runtime files.
- Remaining capture log warnings are Unity/tooling environment noise: access-token warning, XcodeApplications Info.plist warning, and usbmuxd shutdown warning. No compile errors or capture exceptions.
Known gaps:
- Final destroyed VFX asset `vfx.unit.destroyed.small` is still planned/missing, so final destroyed feedback remains blocked on art.
- Current unit/building sprites are still `exists_needs_review`; this pass fixes the evidence capture, not final atlas approval.
- Capture evidence uses explicit quads for review reliability; production runtime should still move to the final packed atlas/config or Addressables path when final art is approved.
Cross-lane impacts:
- QA/HCI can use `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` for close tactical visual review of grounding/readability/scale with current review art.
- UI and Support/FTUE contracts are unchanged.
- Art still owns final hostile variant, final atlas packaging, and `vfx.unit.destroyed.small`.
Next recommended task:
- PM/QA review the updated close capture, then either accept the renderer/capture slice for Gate 1 follow-up or assign final atlas packaging and missing destroyed VFX as the next gameplay/art integration task.
