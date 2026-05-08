# SCN-05 Saga Map Landscape Target

- Canvas: 1672 x 941.
- Direction changed on request: this is a new landscape-optimized AAA target in the same spirit as `MainMenu_Landscape_Visual_Target`, not an exact extraction of the portrait SCN-05 source.
- The source SCN-05 mockup remains the content reference for labels, chapter controls, campaign nodes, rewards, selected/locked node, and route concept.
- Use this target for the landscape Saga Map visual direction: premium military HUD frame, harbor campaign map, curved dotted route, soft-shadow level nodes, gold selected node, and over-map controls.
- Unity implementation should split this into real canvas elements: generated/replaced map background, separate route segments, node prefabs, selected node prefab, dropdown prefabs, top bar, and reward/difficulty controls.

Canonical target: `Design/VisualLock/SCN-05_SagaMap/SCN-05_SagaMap_Landscape_Target.png`

## Generation Prompt

```text
Use case: ui-mockup
Asset type: landscape mobile game UI visual target mockup, 1672x941.

Primary request: Create a AAA-quality landscape mobile game UI mockup for the WarlineCapture Saga Campaign map screen, in the same premium military strategy HUD style as the existing main menu target. This is a new optimized landscape target, not an exact crop of the portrait reference.

Scene/backdrop: Isometric modern military harbor city map with water channels, roads, bridges, buildings, docks, hills in the distance, cinematic tactical lighting. The map should fill the central play area and look detailed, polished, and game-ready.

UI layout:
- Full-screen futuristic military HUD frame with dark graphite/black panels, cyan edge highlights, orange/gold accents, subtle bevels, soft shadows, and premium AAA mobile strategy game polish.
- Top header bar integrated over the map: left back button with the original-style military back arrow icon, title text "SAGA CAMPAIGN", right reward/star counter "24 / 30" with gold star icon.
- Under header, two dropdown controls: "Chapter 03" and "Shattered Harbor", dark glassy panels with cyan chevrons.
- Central campaign route over the map: six level nodes labeled 3-1, 3-2, 3-3, 3-4, 3-5, 3-6. The nodes should follow a clean curved dotted route similar to a campaign progression path, not straight angular lines.
- Level nodes: black/dark beveled card below a hexagonal portrait/mission badge, white text, three gold stars, soft drop shadow. Content must have comfortable spacing; stars must not touch labels or borders.
- Node 3-6 is selected/locked: larger orange/gold hexagonal frame with dark inner plate, lock badge at top, gold "3-6" text, three gold stars, strong but smooth orange glow and soft shadow.
- Bottom left difficulty dropdown "NORMAL" over the map.
- Bottom right button "CHAPTER REWARDS" with "18 / 21" and gold star icon, over the map.

Style requirements:
- Match the MainMenu_Landscape_Visual_Target style: premium military RTS UI, dark beveled panels, cyan highlights, gold accents, realistic rendered background, crisp readable typography, strong hierarchy.
- No generic sci-fi neon redesign, no flat vector look, no bright white borders, no hard solid black drop-shadow blocks.
- Shadows must be soft and smooth, like real AAA UI compositing.
- Buttons and panels should be interactive-looking and sharp, not baked blurry text.
- Text must be legible and exactly as specified.
- Landscape mobile composition with safe margins and no stretched UI.
- No spec-sheet footer labels, no captions, no explanatory text, no watermark.
```
