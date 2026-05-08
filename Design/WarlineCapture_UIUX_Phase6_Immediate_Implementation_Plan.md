# WarlineCapture UI/UX Phase 6 Immediate Implementation Plan

Date: 2026-05-05

## Goal

Continue the tactical HUD vertical slice around `Screen_MatchOverlay`, `SCN-08 RTS Battle HUD`, `SCN-09 Build Drawer / Production`, and `SCN-10 Unit Command` while staying aligned with the updated premium 2D isometric gameplay direction.

This phase keeps the accepted HUD chrome and Canvas layering work, but treats old target-cropped gameplay content as temporary unless it is replaced by 2D isometric production assets or explicit placeholder states.

## Updated Design Alignment

The updated game design changes the content-art source for gameplay-facing UI:

- UI chrome, buttons, tabs, panels, frames, sliders, toggles, dropdowns, and layout targets still come from `Design/VisualLock` and `Design/VisualLockLayered`.
- Gameplay-facing content must follow `Design/WarlineCapture_2D_Isometric_Production_Direction.md` and `Design/WarlineCapture_2D_Isometric_Art_Bible.md`.
- Squad thumbnails, minimap content, map previews, mission art, battlefield capture backgrounds, unit thumbnails, building thumbnails, and tactical overlay art should come from `Design/VisualReferences/2DIsometricProduction` or `Assets/Game/Art/Generated/2DISO`.
- Do not generate or expand old Synty/desert/low-poly tactical content for new UI work.
- Do not bake unit art, minimap content, icons, labels, health bars, or markers into UI chrome.

## Current Tactical HUD Status

Implemented or partially implemented:

- `Screen_MatchOverlay.prefab` has the main tactical HUD hierarchy.
- Resource bar, objective panel, threat feed, squad tray, command bar, minimap, pause/settings buttons, build button, and build drawer exist as Canvas objects.
- MatchOverlay chrome has been rebuilt into separate fill/frame/button/icon/text layers with focused validation.
- `BuildDrawerPanelController` toggles the build drawer from the HUD build button.
- Focused MatchOverlay tests validate hierarchy, sprite paths, sliced frames, transparent frame centers/corners, selected command states, and drawer wiring.
- `Design/VisualLockLayered/SCN-08_RTSBattleHUD_M01_TacticalFeedback` now exists as the first refreshed tactical state layer pack.
- The M01 tactical feedback target was promoted from the clean visual candidate and should be treated as the official state reference. Do not use the rejected rough target for visual matching.
- `Screen_MatchOverlay.prefab` now contains hidden-by-default scaffolds for `BattleHud.SelectedEntityPanel`, `BattleHud.CommandModeBanner`, `BattleHud.WorldCommandMarkerLayer`, and `BattleHud.InvalidCommandToast`, plus an active `BattleHud.MinimapCameraBridge` viewport indicator.
- `BattleHudTacticalFeedbackController` owns the new feedback references and provides initial show/hide methods for selected entity, command mode, invalid command, and world marker visibility.
- M01 tactical-feedback layers have been cleaned so frames do not contain baked text/icons and runtime markers do not contain map pixels. `TacticalMapQaPreview` exists only as an inactive QA preview layer for capture comparison.

Known content-art adjustment:

- Current squad portraits, minimap content, and some build drawer thumbnails still use target crops or generated HUD placeholder content. These are acceptable only as temporary UI wiring placeholders.
- The next UI pass should replace those gameplay-facing images with 2D isometric asset references where assets exist.

## Immediate Work Order

0. Apply the tactical missing-UI work order.
   - Use `Design/WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md` as the current handoff for close-up tactical-map gameplay.
   - Use `Design/WarlineCapture_M01_FirstContact_Production_Contract.md` as the first concrete validation target for the UI agent.
   - Use `Design/WarlineCapture_UIUX_Tactical_Strategic_Target_Update_Audit.md` as the current target audit for stale/missing tactical state targets.
   - Use `Design/VisualLock/SCN-08_RTSBattleHUD_M01_TacticalFeedback/SCN-08_RTSBattleHUD_M01_TacticalFeedback_Landscape_Target.png` as the refreshed M01 HUD state target after its matching `VisualLockLayered` pack exists.
   - Use `WarlineCaptureUiPhase1PrefabBuilder.CaptureMatchOverlayTacticalFeedbackVisual` for M01 state QA; the default MatchOverlay capture intentionally hides tactical feedback by default.
   - Add `BattleHud.SelectedEntityPanel`, `BattleHud.CommandModeBanner`, `BattleHud.WorldCommandMarkerLayer`, `BattleHud.InvalidCommandToast`, and `BattleHud.MinimapCameraBridge`.
   - Treat strategic/zoomed-out map art as preview/minimap only. `SCN-08` gameplay validation must happen over the approved close-up tactical ground plate with separate runtime sprites.

1. Freeze the accepted MatchOverlay chrome patterns.
   - Keep thin neutral pause/settings chrome.
   - Keep thin squad card frames with transparent centers and separate card fill/content.
   - Reuse selected button/tab animator states for command buttons and squad cards.

2. Add 2D isometric content binding points.
   - Source `Squad_Rifle` thumbnail from `Assets/Game/Art/Generated/2DISO/GoldenAssets/GA-08_RifleSquad.png`.
   - Source APC thumbnail from `GA-09_APC.png`.
   - Source Tank thumbnail from `GA-10_Tank.png`.
   - Use cropped UI thumbnail derivatives when needed so the accepted 2DISO assets read clearly inside mobile HUD slots.
   - Use explicit placeholder or designed-unavailable content for Helicopter/Medic until accepted 2D isometric assets exist.
   - Source HQ/building rows from accepted 2D isometric building assets such as `GA-04_ForwardCommandHQ.png` when appropriate.

3. Replace old minimap placeholder content.
   - Add a `MinimapArtId` or temporary `IsoMapPreviewArtId` binding point.
   - Use 2D isometric map/minimap art when available.
   - Until then, keep the minimap frame valid but mark the content as placeholder in tests/notes.

4. Capture HUD over isometric context.
   - Continue transparent and flat-background captures for alpha QA.
   - Add a non-black 2D isometric background capture check using the current runtime prototype output when available.
   - Confirm opaque corner artifacts do not appear over the 2D isometric battlefield.

5. Preserve gameplay contracts.
   - Every visible HUD, drawer, command, squad, minimap, and resource element must remain covered by `WarlineCapture_UIUX_Gameplay_Element_Alignment.md`.
   - Any unavailable command/build/unit content must show `DesignedUnavailable`, `Locked`, `ReadOnly`, or `DevOnly` feedback instead of acting inert.

## Validation

Run after each tactical HUD pass:

- `WarlineCaptureUiMatchOverlayTests`
- MatchOverlay 16:9 capture
- MatchOverlay 20:9 capture
- MatchOverlay transparent capture when frame/corner alpha changes
- Focused target-vs-capture crops for any panel or control being changed

Additional design validation:

- Compare HUD over `Design/VisualReferences/2DIsometricProduction/RuntimePrototype` captures once the HUD is meant to sit above 2D isometric gameplay.
- Do not wire the isolated 2D isometric spike scenes into Jenkins; they remain manual design validation assets.

## Not In This Phase

- Do not replace the actual gameplay scene with the 2D isometric runtime prototype.
- Do not convert old 3D gameplay systems to 2D here.
- Do not create new mission/objective/reward systems before the tactical HUD slice is stable.
- Do not expand gameplay art generation inside the UI builder; content art belongs to the 2D isometric production pipeline.

## Exit Criteria

Phase 6 is ready to move on when:

- The new M01 tactical state target has a matching layer pack, target-to-canvas mapping, and capture comparison. The base `SCN-08_RTSBattleHUD` target alone is no longer enough.
- `Screen_MatchOverlay` and its build drawer use stable layered Canvas hierarchy.
- HUD chrome is reusable and atlas-ready.
- Gameplay-facing content either references accepted 2D isometric assets or has an explicit temporary/placeholder state.
- M01 First Contact can be captured with selected rifle squad, move/attack markers, invalid command toast, objective jump, minimap viewport, and disabled Build feedback as described in `WarlineCapture_M01_FirstContact_Production_Contract.md`.
- Selected entity, command mode, world command markers, invalid command feedback, and minimap camera-jump hooks exist as named UI elements from `WarlineCapture_Tactical_UI_Missing_Parts_Work_Order.md`.
- Existing select/move/attack/build/minimap flows are not broken by HUD input.
- Captures pass at 16:9 and 20:9.
- Focused tests pass.
