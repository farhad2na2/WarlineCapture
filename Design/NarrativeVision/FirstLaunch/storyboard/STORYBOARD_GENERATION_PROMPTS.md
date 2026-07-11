# First-Launch Storyboard Generation Prompts

Date: 2026-07-10

Status: Gate 4 production prompts active

## Output Contract

- Generate three sheets at `1672 x 941` or another exact 16:9 size.
- Sheets 1 and 2 use a strict 3-by-3 grid of nine equal 16:9 cells.
- Sheet 3 uses the same grid; its first six cells contain panels and its final three cells are plain dark placeholders.
- Read order is left-to-right, top-to-bottom.
- Thin, uniform dark gutters separate cells. Subjects never cross a gutter.
- Do not generate labels, panel IDs, captions, speech bubbles, UI, signs, words, numbers, logos, flags, real insignia, religious symbols, or medical-cross symbols.
- Storyboard sheets are composition evidence, not final art and not part of the `0/22` Gate 6 completion count.

The built-in generator accepts at most five reference paths. Use the consolidated `evidence/visual/continuity/STORYBOARD_CHARACTER_REFERENCE.png` board when multiple principal identities are needed.

## Shared Style Prefix

```text
Use case: illustration-story
Asset type: production storyboard thumbnail sheet for the WarlineCapture first-launch full-screen cinematic
Style/medium: rough but polished AI storyboard color thumbnails in the exact approved Match-aligned low-poly POLYGON Military visual language; visibly faceted geometry, blocky proportions, flat color materials, cinematic dawn and practical light.
Composition/framing: every cell is a self-contained landscape 16:9 composition with one clear phone-scale read; do not let subjects cross gutters.
Constraints: preserve all referenced identities and geography; no speech bubbles; no captions; no labels; no words; no numbers; no UI; no logos; no flags; no real insignia; no religious symbols; no medical cross; no gore; no photorealism; no painterly realism; no malformed or duplicated people.
```

## Sheet 1: `FL-P01` Through `FL-P07`

References:

1. `FL-P01_StyleLock_A_Master_16x9.png`
2. `WORLD-OLDMARKET-02_LivingMorning_CandidateB.png`
3. `WORLD-OLDMARKET-03_AttackBlackout_CandidateB.png`
4. `WORLD-RELAY-01_DamagedCommandPost.png`
5. `STORYBOARD_CHARACTER_REFERENCE.png`

Candidate B corrective body:

```text
Create exactly nine equal cells in a strict 3-column by 3-row grid. The first seven cells visualize FL-P01 through FL-P07 in order. Cells eight and nine are plain dark empty placeholders.

1. Elevated Old Market before dawn: functioning lived city, plain clinic supply delivery with no emblem, road crew, market activity, distant aircraft.
2. Exact same geography during two localized coordinated failures and partial blackout. Restrained smoke and dust, no broad city destruction, no casualties.
3. Damaged improvised local command post: dead radios, cracked dark terminals and terrain table.
4. One coherent wide crisis scene. Left foreground: unmistakable approved Dalia, tan JRC field uniform, sunglasses and headset, directing JRC rescue of convoy survivors. Right foreground: unmistakable approved Samira in mustard hijab reporting protected civilians behind a blocked plain clinic street. The clinic facade is completely blank and has no cross, emblem, sign, lettering, or colored icon.
5. Close rugged terminal, gloved faceless Commander hands, fragmented approved cyan-and-amber ARIA interrupted rings booting.
6. Faceless rear-three-quarter Commander at the damaged terminal while one abstract emergency authority record resolves. No portrait or readable UI.
7. Terrain table/district picture resolving around one connected Old Market road and clinic corridor.

Keep Dalia and Samira visually distinct and large enough for phone recognition. Commander stays faceless. Cells eight and nine must contain only a uniform dark neutral fill.
```

## Sheet 2: `FL-P08` Through `FL-P16`

References:

1. `FL-P01_StyleLock_A_Master_16x9.png`
2. `WORLD-RELAY-01_DamagedCommandPost.png`
3. `WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`
4. `STORYBOARD_CHARACTER_REFERENCE.png`
5. `FACTION-ASH-01_FirstContactPatrol_CandidateB.png`

Prompt body:

```text
Create exactly nine equal cells in a strict 3-column by 3-row grid. Visualize FL-P08 through FL-P16 in exact order.

1. Clean damaged roster-terminal background with crisis visible behind it; leave a central quiet surface for runtime Commander portrait choices, but generate no UI or portraits.
2. Same terminal family with approved stable cyan ARIA and Old Market route context, leaving a quiet runtime confirmation zone.
3. Abstract district terrain map as power, road and command links fail; no labels.
4. Approved Dalia in command pose beside two surviving JRC squad silhouettes and a distant separate abandoned-forward-post marker.
5. Approved Samira reporting beside protected civilians, a plain clinic route and municipal workers.
6. Approved ARIA interrupted-ring analysis over the district map, solid confirmed links and incomplete uncertain links.
7. Faceless Commander hands over the terrain table as Dalia, Samira and ARIA channels compose one response picture.
8. Correct low-escalation M01 Ash patrol approaches the blocked civilian route: Male 03 courier/raider, Female 01 rifle-cell commander and Female 02 sidearm/logistics operative. Do not use Male 05/Qassem and do not use a heavy gunner.
9. Same Old Market geography with clear spatial separation between the armed patrol, protected civilians and the first safe command route. No baked tactical UI.

Hostility reads only from confirmed weapons, formation and action. Civilians remain visibly unarmed and spatially protected.
```

Candidate B corrections:

```text
Preserve the successful Candidate A compositions for P10-P13 and P15-P16, but make these two corrections mandatory:

P09 must show the approved ARIA interrupted-ring identity large and unmistakable on the active terminal. It cannot be another blank terminal. Use stable cyan rings with one restrained amber damaged fragment, with Old Market visible beyond. Leave only a small quiet zone for later runtime confirmation UI.

P14 must not physically place Dalia, Samira, any JRC soldier, any Ash Line member, or any other person in the Relay room. Only the faceless Commander is physically present at the terrain table. Show Dalia and Samira as two clearly separated remote field-scene planes or light projections at the left and right edges, with the approved ARIA ring above the map at center. No armed insurgent appears in this panel. The composition communicates three remote channels joining one response picture.

P16 may show subtle cyan/amber storyboard route-light guides for composition planning, but no HUD, icons, labels, arrows, or permanent baked tactical interface.
```

## Sheet 3: `FL-P17` Through `FL-P22`

References:

1. `FL-P01_StyleLock_A_Master_16x9.png`
2. `WORLD-OLDMARKET-04_M01Handoff_CandidateB.png`
3. `WORLD-OLDMARKET-05_DebriefCorridor_CandidateB.png`
4. `WORLD-RELAY-03_StabilizedCommandPost.png`
5. `STORYBOARD_CHARACTER_REFERENCE.png`

Prompt body:

```text
Create exactly nine equal cells in a strict 3-column by 3-row grid. The first six cells visualize FL-P17 through FL-P22 in order. The final three cells are plain dark empty placeholders.

1. Approved Dalia in restrained handover pose with the connected Old Market route behind her; the Commander is represented only by camera point of view.
2. Exact M01 approach composition: player anchor foreground, move point center, armed patrol route background, connected road and light cover. No HUD or markers.
3. Same Old Market geography after the route is secured: persistent damage, thinning smoke, responders and civilians moving through the reopened corridor.
4. Close evidence surface with weathered blank orders, abstract timing shapes, recovered radio and one incomplete amber revoked-ARIA trace. No readable writing or complete credential.
5. Approved Dalia reports over the secured corridor/map as a second small armed cell moves toward a distant abandoned forward post.
6. Same improvised Relay room geometry stabilized: damage and scars remain, practical power returns, approved cyan ARIA identity is stable. This is the district coordination post, not the abandoned forward post.

Keep all story information inside phone-safe center framing. Final three cells contain only a uniform dark neutral fill.
```

Candidate B corrections:

```text
Preserve the successful P17, P19, P21, and P22 compositions, but correct P18 and P20.

P18 must read as three distinct depth anchors at phone scale without HUD: foreground left/bottom contains two tan JRC player soldiers seen from behind beside light cover; center contains a clear empty connected-road decision area; far background contains the correct three-person Ash patrol as visibly separate smaller silhouettes approaching on the road. Do not render every armed person with the same uniform. Preserve civilian frontage at the side and keep all anchors inside the cyan story-safe area.

P20 must move the incomplete amber revoked-ARIA trace and the key abstract timing pattern into the central upper half of the evidence surface. The lower 24 percent of the cell is quiet dark crate/desk surface with no critical evidence so runtime subtitles cannot cover the trace. Keep the order sheet blank and abstract: no letters, text-like bars, numbers, stamps, dates, or complete credential symbol.
```

## Review And Rejection Log

| Sheet | Candidate | Status | Findings |
|---|---|---|---|
| P01-P07 | Candidate A | Rejected | P04 generated a green clinic cross; Dalia was not identity-readable; P02 destruction was broader than the restrained localized-failure contract. Candidate is retained as `candidates/STORYBOARD-SHEET-01_P01-P07_CandidateA_REJECTED.png`. |
| P01-P07 | Candidate B | Accepted For Gate 4 | `1672 x 941`; blank clinic facade; Dalia and Samira identity/action read correctly; attack scale is localized; seven ordered panels plus two clean placeholders. Stored as `candidates/STORYBOARD-SHEET-01_P01-P07_CandidateB_ACCEPTED.png`. |
| P08-P16 | Candidate A | Rejected | P09 omitted ARIA and remained a blank terminal. P14 incorrectly placed Dalia, Samira, and an armed insurgent physically inside the Relay room instead of remote channels. Stored as `candidates/STORYBOARD-SHEET-02_P08-P16_CandidateA_REJECTED.png`. |
| P08-P16 | Candidate B | Accepted For Gate 4 | `1672 x 941`; P09 restores approved ARIA; P14 keeps the Commander physically alone and separates Dalia/Samira as remote channels; district, civic, analysis, and correct three-person patrol panels remain coherent. Stored as `candidates/STORYBOARD-SHEET-02_P08-P16_CandidateB_ACCEPTED.png`. |
| P17-P22 | Candidate A | Rejected | P18 failed to distinguish player, move, and patrol depth anchors at phone scale. P20 placed the critical amber trace inside the subtitle reserve. Stored as `candidates/STORYBOARD-SHEET-03_P17-P22_CandidateA_REJECTED_SAFEAREA.png`. |
| P17-P22 | Candidate B | Partial, Not Accepted | P18 now passes three-depth handoff readability, but P20 again places the amber trace inside the lower subtitle reserve. Stored as `candidates/STORYBOARD-SHEET-03_P17-P22_CandidateB_PARTIAL.png`. |
| P20 standalone correction | Candidate C | Accepted For Gate 4 | Full `1672 x 941` AI frame; fragmentary amber trace and abstract route pattern remain above the subtitle reserve; lower 30 percent is empty. Stored as `candidates/STORYBOARD-FRAME-P20_CandidateC_ACCEPTED.png`. |
| P17-P22 | Candidate C composite | Accepted For Gate 4 | Candidate B with exactly one deterministic whole-cell P20 replacement. Pixel-difference bounds are exactly the P20 cell rectangle `546x303+8+319`. Stored as `candidates/STORYBOARD-SHEET-03_P17-P22_CandidateC_ACCEPTED.png`. |

## Acceptance Checks

- Grid geometry and output aspect are valid.
- Every required panel occupies exactly one cell and follows narrative order.
- Dalia, Samira and ARIA match their approved continuity sheets.
- Commander remains faceless except for dynamic runtime portrait surfaces.
- M01 patrol uses the approved three-character composition and reserves Male 05 for Qassem.
- Old Market road and landmark continuity remain connected and coherent.
- P19 retains P18 damage; P22 retains P03 room damage/scars.
- No generated text, symbols, cross, flag, emblem, UI or speech bubble appears.
- Each cell retains a quiet lower subtitle band and top-right Skip zone.

Correction evidence: `evidence/visual/storyboard/STORYBOARD-SHEET-01_P04_CORRECTION_COMPARE.png` shows rejected Candidate A on the left and accepted Candidate B on the right.

Sheet 2 rejection evidence: `evidence/visual/storyboard/STORYBOARD-SHEET-02_CandidateA_REJECTION_COMPARE.png` shows the blank P09 terminal on the left and the spatially invalid P14 composition on the right.

Sheet 2 correction evidence: `evidence/visual/storyboard/STORYBOARD-SHEET-02_CORRECTION_COMPARE.png` shows Candidate A on the left and accepted Candidate B on the right for both corrected panels.

Sheet 3 rejection evidence: `evidence/visual/storyboard/STORYBOARD-SHEET-03_CandidateA_SAFEAREA_REJECTION.png` shows P18 anchor ambiguity on the left and P20 subtitle collision on the right. The rejected full contact sheet and both safe-area sheets are preserved under `evidence/visual/storyboard/CONTACT-CANDIDATE-A_REJECTED*`.

Sheet 3 Candidate B evidence: `evidence/visual/storyboard/STORYBOARD-SHEET-03_CandidateB_SAFEAREA_REVIEW.png` confirms corrected P18 on the left and the repeated P20 subtitle collision on the right.

## Standalone P20 Candidate C

References:

1. `STORYBOARD-SHEET-03_P17-P22_CandidateB_PARTIAL.png`
2. `WORLD-PROPS-01_CivicAndCommandProps.png`
3. `WORLD-FX-01_ReusableEffectsSheet.png`
4. `CHAR-ARIA-01_CandidateA.png`
5. `FL-P01_StyleLock_A_Master_16x9.png`

Prompt body:

```text
Create one standalone landscape 16:9 storyboard frame for FL-P20, not a grid. Use an elevated close view of a dark plain evidence table. Place the recovered radio at upper-right. Place a blank weathered order sheet across upper-left and upper-center, using only irregular abstract map polygons and route shapes with no writing, bars, letters, numbers, stamps, or dates. Place one visibly incomplete amber ARIA interrupted-ring trace on top of the paper in the upper-center, clearly above 60 percent image height. The ring is fragmentary and not a complete credential. Keep the entire bottom 30 percent of the image as plain, dark, empty tabletop with no paper, radio, ring, hand, weapon, debris, or critical object so runtime subtitles cannot cover evidence. Keep the top-right corner quiet for Skip. Match the approved faceted low-poly POLYGON Military style. No UI, labels, symbols, cross, logo, or readable text.
```

The generated frame remains entirely AI-authored. The only assembly operation is deterministic replacement of one complete storyboard cell; there is no masking, paintover, or partial-image retouching.
