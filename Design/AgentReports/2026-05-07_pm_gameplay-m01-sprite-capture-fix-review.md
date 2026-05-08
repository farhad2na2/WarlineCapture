Gate: Gameplay M01 sprite-renderer close tactical capture fix
Status: accepted
Reason:
- The updated close tactical capture fixes both previously rejected visual blockers: unit/decor sprites no longer render as map-fragment rectangles, and the hostile patrol is fully visible instead of clipped at the frame edge.
- The submitted PNG is acceptable as current review-art evidence for M01 unit grounding, scale, and capture composition. It is not final art approval.
Validation accepted:
- `Assets/Game/Art/Generated/2DISO/Chapter01/Validation/M01_SpriteRenderer_CloseCapture.png` was manually inspected by PM and shows `decor.command_point`, `unit.player.rifle_squad_01`, and `unit.enemy.patrol_01` together on the tactical ground.
- Gameplay reported `WarlineCaptureM01SpriteRendererCaptureBuilder.BuildAndCapture` completed and logged `WARLINECAPTURE_M01_SPRITE_RENDERER_CAPTURED`.
- Gameplay reported `Chapter01M01SpriteRendererTests`: passed 4/4.
- PM scan found no banned scene lookup calls in the touched gameplay runtime files: `MissionRuntimeComponents.cs`, `Chapter01M01SpriteAssetResolver.cs`, and `MissionRuntimeSpriteRendererSystem.cs`.
Validation still needed:
- QA/HCI still needs integrated M01 smoke/readability validation before Gate 4 can pass.
- Final art approval is still blocked on final atlas/config packaging, final hostile readability treatment, and `vfx.unit.destroyed.small`.
- Current AI-generated unit/building/tactical-map sprites remain `exists_needs_review`; do not mark asset-register rows complete from this capture alone.
Cross-lane notices:
- QA/HCI may now use `M01_SpriteRenderer_CloseCapture.png` as current close tactical visual evidence for grounding, scale, and readability checks.
- UI and Support/FTUE contracts are unchanged; their next work should wait for QA/HCI findings unless PM assigns a concrete blocker.
- Art/PM still owns final M01 asset approval, hostile non-color readability, and destroyed VFX approval.
Next gate/task:
- Move to QA/HCI M01 smoke/readability review using the accepted current review-art capture. Keep active balance QA blocked until the integrated route has a QA/HCI pass with no blocker findings.
