# Board 6 · Selection and target markers

The owner approved the [Board 6 mockup](../../selection-target-markers.png) as the visual direction on November 20, 2026. The production art uses signed-distance shapes on ground quads. The shader draws a 3.5 px stroke and 1.5 px dark keyline at 1080p when there is enough space between strokes.

| Approved mockup | Native gameplay |
| --- | --- |
| ![Board 6 selection, move and attack study](../../selection-target-markers.png) | ![Split Front, native tank selection at 2400×1080](./en-2400x1080-vehicle-selected.png) |

## Native evidence

The EN captures passed the eight-frame gate at both widths after the lab’s previous `Temp` directory was moved aside. The fixture sends Move while the tank is selected, before the launcher lesson. A revised destination on open sand west of the tank keeps the marker clear of refinery geometry and the lesson callout. The 2400×1080 Attack retry captures acceptance before the comms cinematic starts. [Follow-up 4 results](../../../../Orchestration/windows_dev_results/2026-11-20-ui-c-selection-markers.md#board-6-follow-up-4)

| State | 1920×1080 | 2400×1080 |
| --- | --- | --- |
| Infantry selected | [Frame](./en-1920x1080-infantry-selected.png) | [Frame](./en-2400x1080-infantry-selected.png) |
| Vehicle selected | [Frame](./en-1920x1080-vehicle-selected.png) | [Frame](./en-2400x1080-vehicle-selected.png) |
| Launcher selected | [Frame](./en-1920x1080-large-unit-selected.png) | [Frame](./en-2400x1080-large-unit-selected.png) |
| Zoom in and out | [In](./en-1920x1080-zoom-in.png), [out](./en-1920x1080-zoom-out.png) | [In](./en-2400x1080-zoom-in.png), [out](./en-2400x1080-zoom-out.png) |
| Attack preview and accepted | [Preview](./en-1920x1080-attack-preview.png), [accepted](./en-1920x1080-attack-accepted.png) | [Preview](./en-2400x1080-attack-preview.png), [accepted](./en-2400x1080-attack-accepted.png) |
| Move accepted | [Frame](./en-1920x1080-move-accepted.png) | [Frame](./en-2400x1080-move-accepted.png) |

The infantry frames show an open cyan centre at nominal zoom. A normal touch at the displayed world target selects the launcher. The fixture uses production camera requests for the zoom frames because the HUD offered no reachable zoom button. The close frames leave the launcher offscreen at 1920×1080 and partly behind the ARIA panel at 2400×1080. Refinery stacks partly obscure it in the far frames. The Attack preview shows the cyan guide around the battery. The accepted frame shows red-orange target brackets over the battery and the separate missile impact-area circle.

## Intentional differences

- The cyan ring is continuous and uses four fixed ticks. Its proportions follow the selected unit's footprint.
- Move uses an open diamond and two chevrons at the accepted destination. The existing order path and its rendering determine any route decoration.
- Attack uses four brackets and a centre pip in both preview and accepted feedback. Preview retains its lower opacity. The projection cap keeps the complete Attack quad within 20% of viewport height, measured along its longest projected span.
- The missile impact-area circle is a gameplay indicator outside Board 6 scope. [`SplitFrontLauncherHudView.cs:31`](../../../../../Assets/Game/Scripts/UI/Screens/SplitFrontLauncherHudView.cs#L31) draws it at the authored launcher damage radius.
- The native captures use separate command states with the production HUD and mission camera. The mockup combines the three glyphs for comparison.
- Live markers come from world presentation. The HUD marker preview remains hidden during gameplay.

## Art assets

- `Assets/Game/Rendering/Shaders/TacticalMarker.shader`
- `Assets/Game/Rendering/Meshes/Selection/Tactical_InfantryRing.asset`
- `Assets/Game/Rendering/Materials/Selection/TacticalMarkerSelection.mat`
- `Assets/Game/Rendering/Materials/Selection/TacticalMarkerMove.mat`
- `Assets/Game/Rendering/Materials/Selection/TacticalMarkerAttack.mat`
- `Assets/Game/Resources/TacticalMarkerAttackTarget.mat`

Real-player and device acceptance are pending.
