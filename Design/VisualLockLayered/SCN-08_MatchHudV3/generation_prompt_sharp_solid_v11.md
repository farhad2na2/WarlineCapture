# Match HUD V3 Sharp Solid V11 Generation Prompt

```text
Use case: ui-mockup
Asset type: WarlineCapture mobile tactical match HUD visual target, v11 ARIA FTUE plus bottom command consolidation
Input images: Image 1 is the preferred expanded-ARIA FTUE layout reference; Image 2 is the sharp-solid main-menu style reference; Image 3 is the ARIA portrait/personality reference.
Primary request: Keep the expanded ARIA FTUE section from Image 1, but make the layout more practical by moving BUILD, SUPPORT, and map access into the bottom command bar. Remove the current fixed minimap.

Preserve from Image 1:
- Tall selected-unit panel on the left.
- Five squad cards bottom-left with title text.
- Expanded top-right ARIA FTUE module with portrait, "TUTORIAL 1/3", longer text, and two large buttons "DO IT" and "SHOW ME".
- Tutorial highlights on Rifle Squad and MOVE.
- HOSTILE CELL SPOTTED warning panel.
- Dry Sahrin battlefield background, no water.
- Attack-unavailable feedback above command rail.

Change the right and bottom controls:
- Remove the standalone bottom-right minimap panel completely.
- Remove the right-side BUILD and SUPPORT vertical rail so ARIA no longer covers those controls.
- Remove the right-side + / - zoom block that belonged to the minimap.
- Extend the bottom command bar across the freed right-side space.
- Bottom command bar should contain these readable action tiles: SELECT, MOVE, ATTACK, HOLD, STOP, SCAN, BUILD, SUPPORT.
- Add one larger MAP tile at the far right of the bottom command bar. It should be wider than the normal command buttons and show a small embedded tactical city-map preview with pips and a viewport rectangle, like a minimap collapsed into a button.
- Label the larger tile exactly: MAP.
- Make BUILD gold/yellow with construction tools icon.
- Make SUPPORT cyan/blue with support/drop/parachute style icon.
- Make MAP teal/cyan with a tactical-grid mini-preview, not a blank icon.
- Keep the bottom command tiles sharp, large, colorful, mobile-readable, and aligned with the existing bottom rail.

ARIA FTUE behavior:
- ARIA can stay as the taller expanded panel from the preferred previous concept because BUILD/SUPPORT/minimap no longer occupy that right-side area.
- Keep ARIA visually connected to tutorial pointer lines.
- Keep `DO IT` green/teal and `SHOW ME` bright blue/cyan.
- Do not let ARIA cover the new bottom command bar.

Tutorial indicators:
- Keep cyan/teal pulse outlines and corner brackets on the first Rifle Squad card and MOVE command.
- Keep amber step badges: "1" on Rifle Squad and "2" on MOVE.
- Keep callouts readable but avoid covering the new BUILD, SUPPORT, and MAP tiles.

Style: high-end colorful mobile RTS HUD, sharp rectangles, solid action colors, strong drop shadows, tactical sci-fi military polish, ARIA cyan holographic accents, brighter than old black/gold.
Avoid: old ornate gold borders, sea/water/naval scenery, diamonds/gems/supply resources, standalone minimap panel, vertical Build/Support rail, tiny unreadable buttons, full-screen modal, extra fake resources.
```
