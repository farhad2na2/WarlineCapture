# ARIA demonstration mockup provenance

Status: design proposal; static image, not an implemented animation.

- Generator: built-in `image_gen.imagegen`.
- Reviewed output: [aria-watch-play-concept-v01.png](aria-watch-play-concept-v01.png).
- Project reference: [English delivered squad screenshot](../../../AgentReports/SkirmishRecruitmentDelivery/english-delivered-squad.png).
- Original generation: `/Users/farhad/.codex/generated_images/01a08250-179d-7261-9476-7492adcb4aa2/exec-f68a6ce6-538c-4c67-9938-f6b3304a18ed.png`.
- Selected revision: `/Users/farhad/.codex/generated_images/01a08250-179d-7261-9476-7492adcb4aa2/exec-669d02a1-4c31-4c5f-b69b-fb6033479233.png`.
- The board illustrates controls and holographic gesture treatment. Generated battlefield art and decorative timings are not implementation requirements.
- Revision corrects the selection panel ownership label from PLAYER CONTROL to ARIA CONTROL.

## Initial generation prompt

```text
Use case: ui-mockup.
Create a polished product-design concept board for a mobile landscape military strategy game called Warline Capture. The attached image is a STYLE AND EXISTING UI LAYOUT REFERENCE, not a demand for an exact screenshot edit. Preserve the game's sand-colored low-poly desert military world, dark charcoal rectangular panels, thin cyan borders, cyan holographic ARIA woman portrait at upper right, resource strip at top, large squad cards at lower left and seven command buttons across the bottom. This is a proposed new "Watch ARIA play" feature; no giant robot, no decorative sci-fi dashboard redesign.

Compose a high-resolution landscape concept board, approximately 3:2 aspect ratio. Upper two thirds: one large legible in-game landscape view with soldiers on a road and ARIA performing a real screen touch on the MOVE button. Lower third: three large neatly spaced interface detail cards for start confirmation, touch gestures, and instant takeover. Use generous margins and readable crisp English text; keep labels short.

In the gameplay view, the upper-right ARIA panel shows her cyan portrait, the exact title "ARIA IS PLAYING", objective "Protect your base.", short explanation "I'll move this squad to cover the road.", and a single large full-width red button labeled "STOP ARIA". Beneath the panel place a compact timer/status line "Next: choose cover". Preserve a small bottom-right minimap above the Build button. Avoid a second Stop command in the normal command bar. Main game buttons read SELECT, MOVE, ATTACK, HOLD, SCAN, SUPPORT, BUILD. Show two selected squad cards.

Hero visual: a beautiful small translucent cyan holographic human hand with one index finger extended, clean polygonal crystal/wireframe details matching ARIA's portrait. The fingertip lands PRECISELY at the center of the MOVE button. The palm is offset upward to avoid hiding the label. A thin bright cyan contact ring and a short soft trail explain the touch. The hand is clearly a screen-overlay teaching pointer, not a giant hand in the 3D world. Restrained glow, no strobe, no huge opaque cursor. Show a small caption above the gesture "Tap Move". No yellow guide arrow and no fake cursor detached from its touch ring.

Bottom card 1 header "START WITH PERMISSION". Show a compact dark confirmation panel: "Let ARIA play this match?" and "Watch her touches. Take over anytime." Two clear buttons "CANCEL" and "START".
Bottom card 2 header "EVERY GESTURE IS VISIBLE". Three small cyan holographic finger illustrations labeled "Tap", "Hold", "Drag"; hold has a circular progress ring, drag has a short trail and a rectangular selection outline. This is a gesture legend, not multiple simultaneous hands in the gameplay view.
Bottom card 3 header "INSTANT TAKEOVER". Show a red "STOP ARIA" button and the simple handover state "YOU ARE IN CONTROL". Small text "No confirmation to stop."

Aim for a realistic, implementable mobile game UI proposal with precise alignment, full-width ARIA action button, restrained holographic treatment and uncluttered battlefield. This is a still mockup of animation states. Do not add performance claims, win guarantees, payment UI, platform logos, or watermarks.
```

## Selected revision prompt

```text
Use case: text-localization. Edit this existing Warline ARIA concept board with exactly one correction. In the left selection panel, the small green-outlined ownership strip currently says 'PLAYER CONTROL' while ARIA is visibly playing. Replace that exact strip text with 'ARIA CONTROL'. Preserve its alignment, icon, size, and styling. Keep absolutely every other element, text, holographic hand, composition, battlefield, lower concept cards and colors unchanged. This is a precise UI state-label correction, not a redesign.
```
