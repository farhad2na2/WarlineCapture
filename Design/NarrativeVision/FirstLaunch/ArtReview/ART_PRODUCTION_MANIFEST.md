# First-Launch Art Production Manifest

Date: 2026-07-10

Status: Gate 6 passed; 22 approved revisions exported and verified

## Production Rule

This folder owns review-only art. Candidates, source masters, continuity sheets, storyboards, animatic frames, and final review composites remain outside `Assets/` until the user explicitly approves every final panel at Gate 6.

After Gate 6, the approved revisions may be exported into runtime art folders and implementation may continue autonomously without further blocking approval requests.

## Art Direction

- AI-generated low-poly cinematic stills that remain visibly part of the current Match scene's POLYGON Military world.
- Faceted geometry, simplified proportions, flat color-block materials, strong silhouettes, and phone-scale readability.
- Cinematic composition, lighting, atmosphere, and storytelling may improve presentation, but may not replace the low-poly asset language with realistic or painterly rendering.
- A lived fictional Middle Eastern regional city: markets, clinic activity, apartments, workshops, road repairs, vegetation, stone, dirt, concrete, and contemporary infrastructure.
- Natural dawn, practical light, dust, smoke, and blackout contrast without a uniform orange desert filter.
- Urgency and restraint, not exaggerated hero poses or celebratory destruction.
- No baked subtitles, UI, logos, flags, real insignia, scripture, news graphics, or generated regional writing.

## Generation Policy

- All final narrative images are AI-generated 2D runtime assets.
- Existing Match screenshots and Unity character renders are strict visual references, not final art.
- No manual paintover, Photoshop retouching, hand masking, hand compositing, 3D scene remodeling, or model/material fine-tuning is required.
- Quality control uses selection, rejection, and AI regeneration.
- Automated resize, crop, chroma-key removal, metadata generation, and format validation are allowed because they do not alter the authored visual content by hand.
- Final presentation uses flat panels with restrained pan, zoom, crossfade, light, and optional generated overlay motion. Fully generative video is excluded from the first slice because geometry and identity drift cannot be reliably corrected without manual work.

## Canonical Source Anchors

| Subject | Source anchor |
|---|---|
| Major Dalia Rahim | `Prefab_UnitGrid_Chr_Soldier_Female_02_Alt_02_Config` and its Unity/generated portrait references |
| Engineer Samira Haddad | `Prefab_UnitGrid_Chr_Civilian_Female_01_Config` and its Unity/generated portrait references |
| Commander | Player-defined identity; use faceless framing before selection and six diverse free portraits plus a neutral fallback in the identity state |
| ARIA | New dedicated identity; do not reuse a human soldier/civilian portrait |
| JRC | Project regular-force roster and weapons |
| Ash Line | Project insurgent roster; distinguish by armed action, equipment, and context rather than ethnicity |
| Old Market/M01 | Current M01 operation-map visual target plus verified scene/location captures when available |
| Command base | Existing bright command-table and restored-headquarters visual direction |

## Reusable Continuity Deliverables

| ID | Deliverable | Review requirement |
|---|---|---|
| `CHAR-ARIA-01` | ARIA master identity with neutral, booting, warning, damaged, and stable states | One unmistakable non-human assistant identity across all states |
| `CHAR-DALIA-01` | Dalia face, wardrobe, equipment, expression, and pose sheet | Matches the canonical model and approved realistic portrait anchor |
| `CHAR-SAMIRA-01` | Samira face, clothing, expression, engineer/reporting pose sheet | Matches the canonical model; remains visibly civilian/infrastructure-focused |
| `CHAR-COMMANDER-01` | Six diverse portraits plus neutral fallback and faceless pre-identity framing | No portrait becomes the canonical fixed Commander |
| `FACTION-JRC-01` | Regular-force silhouette/equipment sheet | Consistent weapons, colors, and restrained fictional markings |
| `FACTION-ASH-01` | Ash Line first-contact patrol sheet | Armed-cell readability without real symbols or civilian interchangeability |
| `CIVILIANS-01` | Old Market families, workers, clinic staff, and responders | Dignified, varied, and appropriate to role/location |
| `WORLD-OLDMARKET-01` | Old Market geography, materials, roads, clinic route, and handoff landmarks | Agrees with the playable operation-map approach |
| `WORLD-RELAY-01` | Damaged JRC terminal and Civic Relay/ARIA visual language | Fictional, readable, reusable, and free of generated text |

## Final Panel Inventory

Each row receives a full-resolution final composite, 20:9 and 16:9 previews, provenance, and a user disposition.

| Panel ID | Sequence | Final visual beat |
|---|---|---|
| `FL-P01` | `seq.prologue.command_lost` | Sahrin before dawn: Old Market opening, clinic supplies, road crew, and aircraft in a functioning lived city |
| `FL-P02` | `seq.prologue.command_lost` | Coordinated blasts and blackout sever separate city systems without graphic casualties |
| `FL-P03` | `seq.prologue.command_lost` | Damaged command post and fragmented JRC channels with unanswered calls and contradictory status lights |
| `FL-P04` | `seq.prologue.command_lost` | Dalia's unit rescues convoy survivors while Samira reports civilians isolated beyond a blocked street |
| `FL-P05` | `seq.prologue.command_lost` | Dormant Civic Relay terminal powers on and fragmented ARIA appears |
| `FL-P06` | `seq.prologue.command_lost` | One emergency continuity candidate is found; Commander remains faceless |
| `FL-P07` | `seq.prologue.command_lost` | Tactical picture resolves around Old Market and points toward the first operation |
| `FL-P08` | `seq.prologue.commander_identity` | Clean damaged-command-post background; portrait choices, name entry, buttons, and Skip remain separate live Unity UI |
| `FL-P09` | `seq.prologue.commander_identity` | ARIA confirms the selected/default Commander and requests a bounded first order |
| `FL-P10` | `seq.ch01.open.first_response` | District map loses power, roads, and command links in quick succession |
| `FL-P11` | `seq.ch01.open.first_response` | Dalia identifies surviving JRC squads and the abandoned forward post |
| `FL-P12` | `seq.ch01.open.first_response` | Samira identifies civilians, clinic access, and municipal crews beyond the attacks |
| `FL-P13` | `seq.ch01.open.first_response` | ARIA visualizes a coordinated strike pattern while clearly marking uncertainty |
| `FL-P14` | `seq.ch01.open.first_response` | Faceless Commander assumes field authority over the fractured response picture |
| `FL-P15` | `seq.ch01.m01.brief` | Confirmed armed Ash Line patrol approaches the blocked civilian route |
| `FL-P16` | `seq.ch01.m01.brief` | Clean tactical geography with confirmed hostiles and protected civilians; ARIA routes/highlights remain separate runtime effects |
| `FL-P17` | `seq.ch01.m01.brief` | Dalia hands tactical control to the Commander |
| `FL-P18` | `first_launch.m01_handoff` | Illustrated Old Market approach that becomes the binding target for the later playable 3D camera |
| `FL-P19` | `seq.ch01.m01.debrief` | Corridor secured; responders and civilians begin moving through the route |
| `FL-P20` | `seq.ch01.m01.debrief` | Recovered patrol orders reveal coordinated timing and a fragmentary trace of a revoked ARIA credential; the full proof remains a later Chapter 1 reveal |
| `FL-P21` | `seq.ch01.m01.debrief` | Dalia reports another cell moving toward the abandoned forward post |
| `FL-P22` | `first_launch.command_base_reveal` | Restored command post becomes the player's earned headquarters and next destination |

## Review Ledger

Allowed final dispositions are `Approved`, `Changes Required`, and `Rejected`. Only the user assigns `Approved`.

| Deliverable | Revision | Status | User notes |
|---|---:|---|---|
| Complete continuity package | 1 | Internally Locked For Gate 6 | Principal approvals plus supporting internal validation complete; final package approval remains Gate 6. |
| Complete world-reference package | 1 | Internally Locked For Gate 6 | Old Market persistent states, Relay states, props, effects, and handoff comparison passed Gate 3. |
| Complete storyboard package | 1 | Internally Locked For Gate 6 | Twenty-two ordered composition frames, manifest, correction evidence, and 16:9/20:9 safe-area review passed Gate 4. |
| Complete animatic package | 2 | Internally Locked For Gate 6 | Revised `176.5s` references add audible ambience, approved comic dialogue, real icons/portraits, separate interactive UI, and clarity-first pacing. MP4 files remain review-only. |
| Complete 22-panel package | 1 | Approved 2026-07-11 | `22/22` exact revisions approved by the user, exported, and hash verified. |
| Integrated art contact sheet | 1 | Approved 2026-07-11 | Ordered, safe-area, storyboard-comparison, reference-summary, motion-proof, and approved-runtime evidence assembled. |

## Gate 6 Package

The final review contains:

1. Reusable continuity sheets.
2. Style and world-direction evidence.
3. A numbered contact sheet in playback order.
4. Every panel at full resolution.
5. 20:9 and 16:9 phone previews with subtitle and Skip safe areas.
6. Storyboard/reference/final comparisons.
7. Motion proofs for layered panels.
8. Provenance and revision ledger.

No Unity narrative-player implementation begins until every final panel is approved.

## Style Direction Decisions

| Direction | Status | Reason |
|---|---|---|
| Direction A: realistic/painterly concept set | Rejected | Useful only as composition exploration; does not match the low-poly Match scene and is not acceptable production art. |
| Direction B: Match-aligned AI low-poly render | Approved 2026-07-10 | Candidate A is the primary `FL-P01` composition anchor; B and C are supporting geometry, palette, and camera references. |

## Principal Identity Decisions

| Identity | Revision | Status | Locked use |
|---|---:|---|---|
| Dalia | `CHAR-DALIA-01_CandidateA.png` | Approved 2026-07-10 | Strict face, hair, sunglasses, headset, tan uniform/equipment, proportions, and low-poly treatment reference |
| Samira | `CHAR-SAMIRA-01_CandidateA.png` | Approved 2026-07-10 | Strict face, mustard hijab, civilian wardrobe, proportions, and low-poly treatment reference |
| ARIA | `CHAR-ARIA-01_CandidateA.png` | Approved 2026-07-10 | Strict non-human radial identity, interrupted-ring geometry, cyan stable state, and amber warning-state reference |

The approval locks identity and visual language. It does not pre-approve the final narrative panels that use these identities.
