# First-Launch Cinematic Storyboard

Date: 2026-07-10

Status: Gate 4 production draft

## Format Contract

The sequence is a full-screen cinematic motion comic, not a comic-book page. Each frame fills the screen. Story text is supplied by runtime subtitles and speaker labels; speech bubbles, baked captions, decorative panel borders, generated writing, and permanent UI are prohibited in the image.

The 22 final frames are cinematic art states. The logo, Commander identity controls, guidance choice, review-only gameplay placeholder, and reviewer controls are separate runtime states and do not increase the final-frame count.

## Composition Rules

- Author every frame for a centered 16:9 story-safe area inside the 20:9 runtime canvas.
- Keep faces, weapons, route arrows, evidence, and other critical information out of the outer 10 percent on each horizontal side.
- Reserve the lower 24 percent for runtime subtitles without covering a face, weapon muzzle, civilian action, or route decision.
- Reserve the top-right 12 percent for the visible Skip control.
- Use one dominant story read per frame at phone scale.
- Pre-identity Commander views remain faceless: hands, shoulder, silhouette, or rear three-quarter framing only.
- Civilians are never visually interchangeable with hostiles. Ash Line readability comes from confirmed weapons, posture, and mission context, not ethnicity.
- Normal motion is restrained to a 3-6 percent pan/zoom, shallow layer drift, light change, dust/smoke, or ARIA signal motion. Reduced motion uses a static hold with the same duration and subtitles.

## Sequence Board

| Panel | Time | Canonical beat | Composition and continuity | Motion / transition | Subtitle intent |
|---|---:|---|---|---|---|
| `FL-P01` | 5.0s | Sahrin before dawn | Elevated Old Market wide; clinic delivery, road crew, civilians, market activity, aircraft; use living Candidate B geography and approved style anchor | Slow push; dawn ambience; hard audio interruption into P02 | Establish Sahrin as lived-in and functioning before conflict |
| `FL-P02` | 4.0s | Coordinated attack and blackout | Same camera family and landmarks; separate localized failures, restrained smoke, no visible casualties | Brief light pulse, blackout wipe, shallow shake; cut to P03 | Multiple systems fail together; avoid naming an unseen culprit |
| `FL-P03` | 4.0s | Command channels collapse | Damaged Relay command room, unanswered radios, cracked dark screens and contradictory abstract lights | Slow lateral drift; radio fragments; dissolve to P04 | JRC command is unavailable and reports conflict |
| `FL-P04` | 5.0s | Human crisis on two fronts | One cinematic crisis montage: Dalia/JRC rescue at damaged convoy in foreground; Samira at blocked civilian street/clinic route in a separated background plane | Two-plane parallax; dust; radio crossfade | Dalia reports survivors; Samira reports civilians cut off |
| `FL-P05` | 4.0s | ARIA boots from the Relay | Approved close terminal treatment; gloved Commander hands; cyan interrupted rings breaking through amber faults | Ring fragments assemble; terminal glow; dissolve | ARIA identifies damaged continuity and limited confidence |
| `FL-P06` | 4.0s | Emergency candidate found | Rear shoulder/faceless Commander at terminal; one anonymous continuity record resolves, with no readable generated UI | Slow rack/push toward silhouette; UI added at runtime only | One valid emergency authority remains |
| `FL-P07` | 4.0s | Old Market becomes the first operation | Terrain table/district map resolves from fractured links to the Old Market road and clinic corridor | Signal lines converge; camera lowers toward map; match dissolve to P08 | ARIA narrows the immediate decision to Old Market |
| `FL-P08` | interactive | Commander identity | Damaged roster surface over Relay-room background; portrait choices are runtime UI using approved identity sheet, never baked into background | Static background with subtle terminal flicker | Player selects or accepts a default Commander identity |
| `FL-P09` | 3.0s | ARIA confirms the Commander | Same terminal family; approved selected portrait appears only in runtime UI; Old Market route remains behind the confirmation | ARIA pulse; brief push; transition to guidance UI then P10 | ARIA confirms the name and requests a bounded first order |
| `FL-P10` | 3.0s | District picture breaks apart | District terrain/map view showing power, road, and command links failing in sequence | Three restrained state changes; cut | The crisis is coordinated across systems, not one isolated blast |
| `FL-P11` | 3.0s | Dalia reports surviving forces | Dalia continuity-locked portrait/action layer beside surviving JRC squads and distant abandoned-post marker; no claim that current command room is that post | Subtle portrait drift and map pulse | Dalia identifies available squads and the next threatened military position |
| `FL-P12` | 3.0s | Samira reports civic stakes | Samira continuity-locked layer; clinic route, municipal crew, responders and protected civilians clearly separated from patrol lanes | Route highlight and gentle background parallax | Samira states who is trapped and which access must remain open |
| `FL-P13` | 3.0s | ARIA detects a pattern with uncertainty | ARIA cyan analysis over map; confirmed links are solid, uncertain links visually incomplete; no omniscient villain reveal | Interrupted rings and partial connection lines | ARIA reports coordination as a probability, not proof |
| `FL-P14` | 3.0s | Commander assumes field authority | Faceless Commander hands over terrain table with Dalia, Samira and ARIA channels composing one response picture | Slow push; channels stabilize | The player accepts responsibility for a bounded rescue corridor |
| `FL-P15` | 4.0s | Confirmed armed patrol approaches | Old Market ground-level/elevated approach; correct M01 Ash patrol silhouettes and weapons; civilians remain behind cover on a separate route | Patrol advance parallax; dust; cut | Dalia confirms an armed patrol moving on the blocked route |
| `FL-P16` | 4.0s | ARIA separates targets and protected space | Same geography; runtime tactical overlay distinguishes confirmed hostiles, protected civilians, and first legal command actions without baked UI | Three sequential highlights; reduced motion uses static final state | ARIA explains the first command constraints and uncertainty |
| `FL-P17` | 4.0s | Dalia hands over tactical control | Dalia in continuity-locked command pose with Old Market route behind; Commander remains represented by camera point of view | Dalia glance/hand gesture implied through layer drift; match cut | Dalia: field units are ready for the Commander's order |
| `FL-P18` | 4.0s | Illustrated-to-3D handoff | Candidate B M01 approach refined into final binding frame: player anchor foreground, move point center, patrol route background, connected road and light cover | Controlled push to exact gameplay camera; fade only after anchor alignment | ARIA gives one concise first-action prompt; gameplay starts by 90s |
| `FL-P19` | 4.0s | Corridor secured | Same Old Market damage and geography; responders and civilians move through reopened route, no total reset | Slow lateral move following responders | Immediate result: the route is open and people can move |
| `FL-P20` | 4.0s | Fragmentary evidence recovered | Close evidence composition: weathered orders/timing marks and an abstract revoked-credential trace; all readable text is runtime UI or absent | Focus drift from order pattern to ARIA fragment | Evidence suggests coordination; the credential trace is incomplete, not proof |
| `FL-P21` | 4.0s | Next threat identified | Dalia over secured-corridor/map composition; another cell moves toward the abandoned forward post | Route pulse toward distance; cut | Dalia identifies the next threat without resolving the conspiracy |
| `FL-P22` | 5.0s | Earned command-base destination | Same Relay room geometry stabilized; damage remains; restored power and approved cyan ARIA identity; this is not the M02 abandoned post | Slow pullback; stable practical lights; end hold | The district coordination post becomes the player's immediate command destination |

## Auxiliary Runtime States

| State | Placement | Contract | Skip destination |
|---|---|---|---|
| `first_launch.logo` | Before `FL-P01` | 2.5 second maximum, minimal title and audio cue, no forced button wall | `first_launch.m01_handoff` |
| `first_launch.commander_identity` | During `FL-P08` | Six portraits plus neutral fallback, editable name, Continue enabled with valid default | `first_launch.m01_handoff` after committing default/selected identity |
| `first_launch.guidance_choice` | Between `FL-P09` and `FL-P10` | Full Guidance, Tactical Hints, Veteran; default Full Guidance; changeable later | `first_launch.m01_handoff` after committing default/selected guidance |
| `first_launch.gameplay_placeholder` | After `FL-P18` in review builds only | Clearly labeled non-gameplay handoff proof with Jump To Debrief | `seq.ch01.m01.debrief` |
| `first_launch.reviewer_controls` | Entire review build | Play, pause, restart, previous, next, scrub, reduced motion, capture, skip to game, jump to debrief | State-dependent |

## Background Reuse Families

- `BG-OM-WIDE`: `FL-P01`, `FL-P02`.
- `BG-RELAY-DAMAGED`: `FL-P03`, `FL-P05`, `FL-P06`, `FL-P08`, `FL-P09`.
- `BG-DISTRICT-MAP`: `FL-P07`, `FL-P10`-`FL-P14`, `FL-P16`, `FL-P20`, `FL-P21`.
- `BG-OM-M01`: `FL-P15`-`FL-P19`, `FL-P21`.
- `BG-RELAY-STABLE`: `FL-P22`.
- `FL-P04` is a dedicated crisis composition and may not be assembled by moving unrelated building interiors or loose props independently.

Reuse means a shared geography authority or generated background family. It does not permit copying a final flattened frame when lighting, action, character scale, or camera perspective would become false.

## Timing Result

The fixed visual holds before the gameplay handoff total 64 seconds. The logo adds up to 2.5 seconds. Identity and guidance are interactive and are budgeted to keep normal first command at or before 90 seconds; both have valid defaults so they cannot block continuation. Debrief and command-base review add 17 seconds after the review-only gameplay placeholder.

## Gate 4 Remaining Evidence

- Generate storyboard thumbnails from these locked compositions.
- Build the numbered contact sheet.
- Produce 16:9 and 20:9 safe-area review boards.
- Verify the complete state graph, subtitle zones, face/weapon visibility, skip destinations, and final timing before marking Gate 4 passed.
