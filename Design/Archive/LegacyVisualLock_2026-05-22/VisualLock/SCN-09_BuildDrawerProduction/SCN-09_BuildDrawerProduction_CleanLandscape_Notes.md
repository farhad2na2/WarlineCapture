# SCN-09 Build Drawer / Production Visual Target

- Canvas: 1672 x 941.
- Canonical target: `Design/VisualLock/SCN-09_BuildDrawerProduction/SCN-09_BuildDrawerProduction_Landscape_Target.png`.
- Direction: generated AAA landscape target using the accepted WarlineCapture visual-lock style.
- Source reference: `Design/UIUX_Codex_Package/uiux_spec_assets/SCN-09_build_drawer_production.jpg`.
- Recreate in Unity as a real drawer overlay with separate category tabs, item cards, queue rows, capacity bar, and action buttons.

## Implementation Notes

- Drawer should overlay the live battle HUD without replacing the battlefield.
- Build item cards and production queue rows should be reusable pooled prefabs.
- Tabs need selected/inactive states matching Settings and Loadout.

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Build Drawer / Production overlay, matching the premium military RTS HUD style of the accepted WarlineCapture visual targets visible in this conversation. This is a new optimized landscape target.

Scene/backdrop: Active low-poly RTS battlefield dimmed slightly behind a bottom/side production drawer overlay. The battlefield should remain visible but secondary.

UI layout:
- Full-screen battle HUD context with a large slide-up production drawer occupying the lower half to lower two-thirds, designed for mobile landscape touch controls.
- Drawer header: title "BUILD / PRODUCTION", close X icon button on the right.
- Category tab row: "INFANTRY" selected in filled blue, "VEHICLES", "AIR", "DEFENSE", "SUPPORT" inactive.
- Left/center scrollable build list: item cards for "Rifle Squad", "Grenadier Team", "Medic Team", "Barracks", each with thumbnail, cost icons, timer, and build button.
- Right production queue panel: title "PRODUCTION QUEUE", three queue rows with progress bars and cancel X icons.
- Bottom capacity strip: "Build Capacity" with cyan progress bar and text "18 / 30".
- Bottom-right secondary button "RUSH ALL" with yellow/orange accent.

Style requirements:
- Match accepted targets: dark graphite 9-sliced panels, cyan bevels, blue selected tabs, orange/yellow CTA accents, smooth shadows, crisp readable text.
- Drawer, tabs, cards, queue rows, capacity bar, close button, and rush button must visually read as separate Unity UI prefabs.
- Do not bake buttons or text into a background concept. No bright white borders, no hard shadows, no cramped controls, no stretched UI, no watermark.
- Text must be legible and exactly as specified where quoted.
```
