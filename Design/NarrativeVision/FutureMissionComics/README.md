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
- [bookends.json](../../../Assets/Game/Resources/FutureMissionComics/bookends.json)
  carries the three remaining chapter openings, all five Protocol Fragment
  closes, the canonical epilogue, three consequence emphasis records, and the
  recovery postscript. The two prologue records and Chapter 1/2 openings
  already have authored first-launch/Chapter 2 content. The preview reader
  plays the Chapter 3 opening on its first chapter selection, Chapter 4/5
  openings before M01, and Chapter 3/4/5 closes after M05;
  Chapter 5 M05 continues through the epilogue and postscript. Chapter 1/2
  closes play after the playable M05 result and debrief, before Continue returns
  to Campaign. The three consequence sequences carry both high and low copy;
  they remain separate from the Chapter 5 preview until gameplay supplies
  Trust, Evidence, and Infrastructure state.

## Visual continuity

SARIN is a dry sandstone desert city with sparse palms, dusty streets and
purple-brown **arid** mountains. Do not add lakes, rivers, coastal skylines,
forests, or green mountains. Match the restrained faceted painterly rendering,
moderate contrast, natural skin tones, warm sandstone light, and limited cyan
interface glow of the corrected Chapter 3 panels.

Use established character art as identity reference. **ARIA is the cyan
hologram** in `Assets/Game/Art/Narrative/M03RadarWarning/Final/M03-B01.png`:
asymmetric side-swept short hair, angular oval face, almond eyes, straight
nose and restrained lips. **Dalia** is the tan-uniformed field commander with
dark tied hair, wraparound glasses and a headset in `M03-B02.png`. Never
substitute Dalia's face or uniform for ARIA's hologram. Laila's airfield reference is
`Assets/Game/Art/Narrative/M04Airlift/Final/M04-B01.png`; Samira's is
`Assets/Game/Art/Narrative/CH03M03FalseFront/CommsSamiraCorrection.png`.
Keep the scene's principal character prominent; ARIA should not replace Laila,
Samira or another speaker.

For any new frame with a named character, supply that character's approved
panel as the image-generation source, preserve the exact face and costume,
and compare the result at face scale before importing. Reject unreferenced
generations even when their city and lighting look attractive. The accepted
Chapter 1/3/4/5 closes and epilogue derive from approved panels; the Chapter 2
close uses an existing approved panel. `Bookends/CH04_Close.png` was normalized
by one pixel to the standard canvas after generation.
The Chapter 3 opening is edited from approved `M03-B01.png` ARIA art; do not
replace it with a mission briefing UI capture or a newly invented ARIA face.

The Chapter 5 M05 briefing, comms, and debrief originally showed an unrelated
bare-haired woman in Laila's role. All three were corrected using the approved
`M04-B01.png` Laila reference while preserving Samira and Dalia. The Chapter 5
close and epilogue were then regenerated from that corrected debrief; do not
use the earlier bare-haired candidates. Verify Samira's unheadseted scarf,
Dalia's dark glasses and headset, Laila's scarf and aviation headset, and
ARIA's separate cyan hologram at every future handoff.

## Gameplay handoff

When a mission scenario is ready, split its brief, comms, and debrief captions
into the narrative sequence IDs in the campaign comic catalog. Bind those
configs through `MenuBootstrapView` and register the playable mission only
after its map, win/loss flow, and input have been validated. Remove the preview
reader for that node at the same time so the click has one clear path.
Chapter 4 and 5 also need progression beyond the current 16-bit availability
mask before they can become playable.
