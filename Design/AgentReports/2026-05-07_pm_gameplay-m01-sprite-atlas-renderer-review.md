Gate: Gameplay M01 sprite-atlas renderer hookup
Status: needs fixes
Reason:
- The code/test portion of the handoff is useful and can be preserved: `MissionRuntimeSpritePresenter` is now consumed for the player squad, hostile patrol, and command/decor proxy, legacy visible model rendering is suppressed for covered M01 entities, and focused EditMode/PlayMode validation passed.
- The visual gate is not accepted because the submitted close tactical capture is explicitly reported as rendering map-fragment rectangles for unit/decor sprites. That capture cannot be used as AAA visual evidence, QA/HCI evidence, or art approval evidence.
Validation accepted:
- `Chapter01M01SpriteRendererTests`: passed 4/4.
- `Chapter01M01PlayableRuntimeTests`: passed 8/8.
- `Chapter01TacticalRuntimeBindingTests`: passed 6/6.
- `Chapter01LegacyRuntimeGuardrailTests`: passed 3/3 after Codex tool/sandbox approval.
- `Chapter01M01PlayModeValidationTests`: passed 3/3 after Codex tool/sandbox approval.
- Capture builder completed without compile/runtime failure, but the resulting image is visually blocked.
Validation still needed:
- Produce a clean close tactical capture where player squad, hostile patrol, command/decor proxy, grounding, and baked/contact-shadow direction render from the intended sprite assets or final atlas, not map fragments.
- Re-run the focused sprite-renderer tests and capture builder after the capture fix.
- Confirm no obsolete scene lookup warnings or banned runtime scene searches were introduced in touched gameplay files.
- Keep final art status as `exists_needs_review` until the user approves final AI-generated atlas/map/unit PNGs.
Cross-lane notices:
- QA/HCI must not use `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` as final visual approval evidence.
- UI and Support/FTUE remain waiting; their accepted assistant binding work is not blocked by this code slice, but integrated QA is blocked by the visual evidence failure.
- Art still needs a hostile variant and `vfx.unit.destroyed.small`; destroyed state should remain atlas/VFX-driven with no separate `Destroyed` child dependency.
Next gate/task:
- Gameplay should immediately fix the close tactical evidence path while preserving the accepted presenter/renderer code. Recommended route: render current unit/decor PNGs through explicit texture-backed quads/materials or the final packed atlas, rerun capture, and submit `Design/AgentReports/2026-05-07_gameplay_m01-sprite-atlas-renderer-capture-fix.md`.
