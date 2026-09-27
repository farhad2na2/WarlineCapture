# Future mission comics

Chapter 3 Mission 5 and Chapters 4–5 are story previews. Their 11 mission nodes
open the existing narrative canvas from the campaign screen. They are absent
from the playable mission catalog and progression masks; selecting them must
never deploy a scenario. The campaign card says **STORY PREVIEW · GAMEPLAY
COMING LATER**.

## Content contract

- [catalog.json](../../../Assets/Game/Resources/FutureMissionComics/catalog.json)
  carries stable mission IDs, English and Persian card text, and ordered brief,
  comms, and debrief captions. These stage tags are handoff points for gameplay.
- Each mission has three full-width images: briefing key art, action/comms,
  and debrief. Captions in the same stage share that stage image. The final
  mission has an extra debrief line. Expand individual stages into more panels
  and add voice assets when gameplay is authored.
- Each PNG is exactly **1672 × 941**, with a Unity sprite import meta file.
  Run `python3 Design/NarrativeVision/FutureMissionComics/check_future_comics.py`
  after any change.
- Mission beats and sequence IDs come from
  `Design/Campaign_Narrative_Sequence_And_Comic_Catalog.md`. Existing visual
  rules are in `Design/NarrativeVision/M3StyleAlignment/visual_direction.md`.

## Visual continuity

SARIN is a dry sandstone desert city with sparse palms, dusty streets and
purple-brown **arid** mountains. Do not add lakes, rivers, coastal skylines,
forests, or green mountains. Match the restrained faceted painterly rendering,
moderate contrast, natural skin tones, warm sandstone light, and limited cyan
interface glow of the corrected Chapter 3 panels.

Use established character art as identity reference. ARIA has warm olive skin,
dark brown hair in a low bun, dark wraparound glasses and a black over-ear
headset. Laila's airfield reference is
`Assets/Game/Art/Narrative/M04Airlift/Final/M04-B01.png`; Samira's is
`Assets/Game/Art/Narrative/CH03M03FalseFront/CommsSamiraCorrection.png`.
Keep the scene's principal character prominent; ARIA should not replace Laila,
Samira or another speaker.

## Gameplay handoff

When a mission scenario is ready, split its brief, comms, and debrief captions
into the narrative sequence IDs in the campaign comic catalog. Bind those
configs through `MenuBootstrapView` and register the playable mission only
after its map, win/loss flow, and input have been validated. Remove the preview
reader for that node at the same time so the click has one clear path.
Chapter 4 and 5 also need progression beyond the current 16-bit availability
mask before they can become playable.
