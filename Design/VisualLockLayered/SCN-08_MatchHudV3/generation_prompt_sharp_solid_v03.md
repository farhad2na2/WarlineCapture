# Match HUD V3 Sharp Solid V03 Generation Prompt

```text
Revise the WarlineCapture Match HUD V3 concept.

Required layout changes:
- Remove the top-right menu/list/hamburger button.
- Header keeps match resources plus Settings and Pause only.
- Remove the standalone Objectives panel from the main HUD.
- Objectives belong in the ARIA assistant/popup flow, not in a separate left panel.
- Left side becomes one tall selected-unit panel starting near the top of the screen.

Tall selection panel content:
- RIFLE SQUAD
- Squad | Anti-Infantry
- Large portrait area
- Health bar and 120 / 120 text
- CURRENT ORDER
- MOVE ORDER or MOVING TO MARKER
- PLAYER CONTROL and Moving state
- Large action buttons: RETURN, DESTROY, BOARD, CAMERA
- Passenger state: PASSENGERS or NO PASSENGERS ONBOARD

Bottom controls:
- Bottom-left squad tray shows five large cards with number, portrait, health bar, and title text:
  Rifle Squad, APC, Tank, Helicopter, Transport.
- Bottom command rail contains only: SELECT, MOVE, ATTACK, HOLD, STOP, SCAN.
- Build and Support are not in the bottom command rail.
- Build and Support live on the right vertical quick rail only.

Right side:
- ARIA cyan assistant control.
- BUILD yellow/industrial control.
- SUPPORT teal control.
- Zoom controls.
- Minimap remains bottom right.

Feedback:
- Show invalid attack feedback above the command rail:
  ATTACK UNAVAILABLE - SELECT A UNIT OR VALID TARGET.
- Attack button uses warning red-orange disabled/unavailable state.

Style:
- Sharp solid mobile UI.
- Large touch targets.
- Solid color action blocks.
- Strong drop shadows.
- Clean readable typography.
- Avoid old thin black-and-gold HUD borders and ornate gold frames.
```
