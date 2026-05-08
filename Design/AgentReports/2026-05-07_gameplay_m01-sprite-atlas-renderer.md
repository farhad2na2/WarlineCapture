Lane: Gameplay
Task: P1 M01 sprite-atlas renderer hookup and close tactical capture
Files changed:
- Assets/Game/Scripts/Components/MissionRuntimeComponents.cs
- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs
- Assets/Game/Scripts/Campaign/Chapter01M01SpriteAssetResolver.cs.meta
- Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs
- Assets/Game/Scripts/Systems/MissionRuntimeSpriteRendererSystem.cs.meta
- Assets/Game/Scripts/Editor/WarlineCaptureM01SpriteRendererCaptureBuilder.cs
- Assets/Game/Scripts/Editor/WarlineCaptureM01SpriteRendererCaptureBuilder.cs.meta
- Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs
- Assets/Tests/Editor/Chapter01M01SpriteRendererTests.cs.meta
- Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png
- Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png.meta
- Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_SpriteRendererCapture.unity
- Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_SpriteRendererCapture.unity.meta
- Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer.md
Contracts touched:
- `MissionRuntimeSpritePresenter` is now consumed by `MissionRuntimeSpriteRendererSystem` for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`.
- `MissionRuntimeSpriteRendererRuntime` managed ECS component stores the spawned sprite GameObject, SpriteRenderer, and current sprite id.
- Runtime renderer resolves presenter sprite ids through `Chapter01M01SpriteAssetResolver` using `chapter01_tactical_asset_manifest.asset` and `chapter01_tactical_scale_contract.asset`.
- Covered M01 entities with `MissionRuntimeSpritePresenterSuppressesLegacyModelTag` now receive recursive `DisableRendering` on legacy model instance entities.
- Damaged/destroyed presenter contract remains atlas/VFX-id based: `vfx.unit.destroyed.small`; no separate `Destroyed` child is used.
- Fixed-direction baked/contact shadow requirement remains asserted through presenter/manifest tests.
User-visible behavior:
- M01 covered production entities now have a presenter-driven sprite renderer path instead of relying on visible legacy 3D `Model` children.
- Enemy patrol gets a temporary hostile tint while sharing the current infantry sprite until a hostile variant is approved.
- A close-capture scene and PNG were generated for review at `Assets/Game/Scenes/DesignTargets/Chapter01/Chapter01_M01_SpriteRendererCapture.unity` and `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png`.
- The capture now shows the current command building and infantry PNG assets on the M01 tactical ground using explicit transparent texture-backed quads for evidence rendering.
Validation run:
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01M01SpriteRendererTests -testResults /private/tmp/warlinecapture-m01-sprite-renderer-results.xml -logFile /private/tmp/warlinecapture-m01-sprite-renderer.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01M01PlayableRuntimeTests -testResults /private/tmp/warlinecapture-m01-playable-results.xml -logFile /private/tmp/warlinecapture-m01-playable.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01TacticalRuntimeBindingTests -testResults /private/tmp/warlinecapture-chapter01-runtime-binding-results.xml -logFile /private/tmp/warlinecapture-chapter01-runtime-binding.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform EditMode -testFilter Chapter01LegacyRuntimeGuardrailTests -testResults /private/tmp/warlinecapture-m01-legacy-guardrails-results.xml -logFile /private/tmp/warlinecapture-m01-legacy-guardrails.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -nographics -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -runTests -testPlatform PlayMode -testFilter Chapter01M01PlayModeValidationTests -testResults /private/tmp/warlinecapture-m01-sprite-renderer-playmode-results.xml -logFile /private/tmp/warlinecapture-m01-sprite-renderer-playmode.log`
- `/Applications/Unity/Hub/Editor/6000.4.0f1/Unity.app/Contents/MacOS/Unity -batchmode -quit -projectPath /Users/farhad/Projects/WarlineCapture-CodexUnity -executeMethod WarlineCaptureM01SpriteRendererCaptureBuilder.BuildAndCapture -logFile /private/tmp/warlinecapture-m01-sprite-renderer-capture.log`
Validation result:
- `Chapter01M01SpriteRendererTests`: Passed 4/4. Includes sprite resolve, presenter state selection, source texture dimensions/alpha, legacy model suppression, no separate destroyed child, and fixed shadow requirement assertions.
- `Chapter01M01PlayableRuntimeTests`: Passed 8/8.
- `Chapter01TacticalRuntimeBindingTests`: Passed 6/6.
- `Chapter01LegacyRuntimeGuardrailTests`: Passed 3/3 after Codex tool/sandbox approval for the Unity command; initial sandbox attempt hit `attempt to write a readonly database`.
- `Chapter01M01PlayModeValidationTests`: Passed 3/3 after Codex tool/sandbox approval. Remaining warnings in log are Unity shutdown/tooling warnings: preview scene leak notice, persistent allocation leak notice, thread prematurely finalized messages, and usbmuxd/debugger-agent shutdown noise. No PlayMode test failures.
- Capture builder: completed and logged `WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURED`. It also logged `WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURE_QUAD` for `unit.player.rifle_squad_01`, `unit.enemy.patrol_01`, and `decor.command_point`, with texture sizes `299x255`, `299x255`, and `401x376`. Remaining warnings are Unity/tooling environment noise: license handshake/access-token warnings, XcodeApplications Info.plist warning, and usbmuxd shutdown warning. No compile exception in the capture pass.
- Scene-search check: no banned runtime scene lookup calls were found in the touched gameplay runtime files (`MissionRuntimeComponents.cs`, `Chapter01M01SpriteAssetResolver.cs`, `MissionRuntimeSpriteRendererSystem.cs`).
Known gaps:
- Final destroyed VFX asset `vfx.unit.destroyed.small` is still planned/missing; renderer contract avoids separate `Destroyed` children but cannot visually validate final destroyed feedback yet.
- Current resolver is editor/file-backed for validation of current manifest PNGs; production should move to a serialized runtime atlas/config or Addressables once final atlas art exists.
- Current unit/building sprite art is still `exists_needs_review`, not final approved atlas art.
- The capture evidence path uses explicit transparent quads to avoid editor transient-sprite serialization issues; production runtime still needs final packed atlas/config replacement before this becomes final art infrastructure.
Cross-lane impacts:
- UI and Support/FTUE ids are unchanged.
- QA/HCI can use `M01_SpriteRenderer_CloseCapture.png` as close tactical evidence for current sprite scale/style review, with the caveat that current art is still not final-approved atlas art.
- Art needs final hostile variant and `vfx.unit.destroyed.small` asset before the atlas renderer can be marked visually complete.
- PM can review the renderer hookup and close tactical capture as a completed implementation slice, while keeping final art approval and atlas packaging as follow-up work.
Next recommended task:
- Move from current `exists_needs_review` unit/decor PNGs to final packed atlas/config, add the missing `vfx.unit.destroyed.small`, then rerun the close tactical capture and QA/HCI smoke on the final art package.
