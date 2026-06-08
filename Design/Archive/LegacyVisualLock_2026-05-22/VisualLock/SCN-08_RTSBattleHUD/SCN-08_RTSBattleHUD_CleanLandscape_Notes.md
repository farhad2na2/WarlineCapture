# SCN-08 RTS Battle HUD Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-08_RTSBattleHUD/SCN-08_RTSBattleHUD_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-08_rts_battle_hud.jpg`.
- Recreate in Unity as battlefield camera content plus separate HUD prefabs, not as one baked overlay image.

## Implementation Notes

- Battlefield art should come from the game camera; this target defines HUD placement and style only.
- Objective panel, threat feed, resource bar, squad tray, command bar, build button, and minimap must be separate prefabs.
- Preserve mobile-safe margins and keep command controls large enough for touch.

## Layer Pack Workflow

- Layer manifest: `Design/VisualLock/SCN-08_RTSBattleHUD/LayerPack/manifest.json`.
- The flattened target mockup is a visual QA reference, not a UI asset source for frames, buttons, rails, or icons.
- Generated MatchHUD chrome, fills, command buttons, top buttons, squad cards, and icons are separate reusable layer sprites under `Assets/Game/Art/UI/Generated/MatchHUD`.
- Minimap and squad portrait content can temporarily reference the target while native map/portrait content layers are produced; those layers must stay separate from chrome and text.
- Every visual-lock pass must capture the Unity prefab at 1672x941 and 20:9, then focused-crop compare named problem areas before calling the screen accepted.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture RTS Battle HUD screen, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Active low-poly urban battlefield viewed from an angled top-down RTS camera, city roads, base structures, friendly squads, enemy units, explosions, helicopters, and tactical movement lines. UI overlays must stay readable and not be baked into the battlefield art.

UI layout:
- Full-screen battlefield with separate HUD frame elements, not a full opaque menu screen.
- Top-left compact panel: "OBJECTIVES" with three checklist rows and a small "STAR GOALS" row.
- Top-center/right resource bar: money, materials, supply, timer, and pause/settings icon, matching the Main Menu resource bar style.
- Left side vertical threat feed panel: title "THREAT FEED" with orange warning row.
- Bottom-left squad tray: four unit cards with low-poly thumbnails, health bars, status icons, and selected state.
- Bottom-center command bar: buttons "STOP", "HOLD", "MOVE", "ATTACK", "SPECIAL" with icon placeholders and pressed/selected visual language.
- Bottom-right minimap panel: small tactical map with cyan frame and objective markers.
- A build button near command bar labeled "BUILD".

Style requirements:
- Match accepted targets: dark graphite metal panels, cyan highlights, blue selected states, orange/yellow warnings and CTA accents, smooth shadows, crisp typography.
- HUD must be dense but readable for mobile landscape, with safe margins and no stretched UI.
- Objective panel, resource bar, squad cards, command buttons, minimap, threat feed, and build button must look like separate Unity UI prefabs.
- No bright white borders, no hard black block shadows, no watermark, no captions.
- Text must be legible and exactly as specified where quoted.
```
