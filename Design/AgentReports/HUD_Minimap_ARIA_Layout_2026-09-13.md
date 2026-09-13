# Independent minimap and adaptive ARIA

- The minimap is a sibling of ARIA in the HUD header, with a bottom-right anchor. It follows the actual Build button top with a 12-unit gap and a 15-unit right margin in the HUD reference space. During placement it clears the taller confirmation bar.
- The minimap uses a 320×220 frame with the existing cyan UI border. Its clipped map artwork cannot claim pointer hits outside that frame.
- ARIA measures wrapped title/body text at the current font size. Empty instructions remove the instruction rectangle. Short instructions contract the panel; longer instructions grow it.
- The portrait can contract when space is limited. Very long body text scrolls within the remaining space, keeping the 72-unit action buttons and minimap clear. Stable content reuses measured layout rather than measuring every frame.
- Existing references to ARIA, minimap input, narration, mission actions, and localization remain connected. The shared HUD builder and mission styling builder preserve the new structure.

## QA

Validation entry point: `HudRightColumnLayoutValidation.Run`, through the approved macOS wrapper in an isolated Editor project.

Checks cover 16:9 and 20:9 layouts, empty/short/long instructions, very long scrollable copy, and all 96 M3/M4 English/Farsi and normal/large-text lesson presentations. The live M3 build/drag/confirm probe captures the resulting HUD, including clearance above the placement bar.

Final result: **passed**, wrapper exit code 0. Log: `/private/tmp/warline-hud-right-column-verified.log`.

- Dock, empty/short/long content, very long scrolling, and margins passed at both aspect ratios.
- All 96 M3/M4 English/Farsi and normal/large-text presentations passed.
- Live M3 placement drag, camera stability, confirmation, and next-lesson progression passed with the new HUD.
- Visually reviewed `/private/tmp/warline-m03-editor-probe/tutorial-build-confirm.png`: ARIA, minimap, and placement controls have separate frames and clear margins.
- Copied the generated, tested HUD prefab back into the main project. Images remain under `/private/tmp`, not Design. Validation was Editor-only.
