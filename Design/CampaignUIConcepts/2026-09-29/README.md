# Campaign UI visual directions — 2026-09-29

Reference-based ImageGen review mockups for SCN-05 Campaign Operations. These are **visual concepts**, not native Unity screens, exact map captures, or player-readiness evidence. The generated progress, currency, objective and reward values are illustrative; bind all text and status to runtime data during implementation.

## Candidates

| Concept | File | Navigation | Assessment |
|---|---|---|---|
| A — District Atlas | [candidate-a-district-atlas.png](candidate-a-district-atlas.png) | Five chapter scene cards; selected chapter has five location cards positioned over a district overview. | **Approved by the owner.** Production uses separate chapter overview art and mission-specific cards, without route lines that imply false geographic continuity. |
| B — Playable District Lens | [candidate-b-playable-district.png](candidate-b-playable-district.png) | Five chapter tabs; five mission strips; one environment center. | Rejected in visual review by the owner. Retained only as exploration history. |
| C — Operations Board | [candidate-c-operations-board.png](candidate-c-operations-board.png) | Five environment bands; expand a chapter. | Rejected in visual review by the owner. Retained only as exploration history. |
| D — District Panorama | [candidate-d-district-panorama.png](candidate-d-district-panorama.png) | A quieter chapter panorama and a single row of five scene cards. | Exploration only; A was selected. |
| E — Mission Gallery | [candidate-e-mission-gallery.png](candidate-e-mission-gallery.png) | Five large scene cards, with selected mission purpose below. | Exploration only; A was selected. |

## Chapter and mission visual binding

The production UI should show a chapter-specific scene and a separate preview of the **selected mission's actual operation map**. A chapter panorama may be authored as a collage, but it must not masquerade as one continuous playable map. Future missions remain concept art until their physical sources and views are authored and qualified.

| Chapter | Chapter identity | Mission-specific preview anchors |
|---|---|---|
| I — First Response | Old Market / district edge, JRC forward post and civic corridor | M01 First Contact — civilian block and patrol road; M02 Establish the Base — buildable forward post; M03 Radar Warning — convoy approach and guard tower; M04 Airlift — landing zone and side streets; M05 Breach Assault — fortified communications node. |
| II — Broken Grid | Hospital road, industrial/refinery belt, market and utilities | M01 Gridlock — blocked hospital corridor; M02 Supply Line — Oil/refinery/Fuel chain; M03 Market Lifeline — Old Market exchange yard; M04 Power Relay — substation and shelter route; M05 Route Reopened — logistics hub. |
| III — Hidden Network | Inhabited city edge, radio/service equipment and evidence route | M01 Signal Trace — competing signal sites; M02 Safehouse Sweep — occupied homes and verified weapons node; M03 False Front — evacuation street; M04 Evidence Chain — archive/witness extraction route; M05 Network Break — bunker/relay site. |
| IV — Air and Armor | City edge airfield and military approaches | M01 Air Corridor — air-defense/flight corridor; M02 Steel Push — armored approach; M03 Split Front — forward base and verified launcher battery; M04 Grounded Signal — planned runway, relay and extraction apron; M05 Armor Break — planned refinery-to-airfield military sector. |
| V — Citywide Command | Civic districts and the Civic Relay, with distinct port and logistics sectors | M01 Citywide Alert — two civic fronts; M02 Trust Under Fire — populated quay and evacuation routes; M03 Network Collapse — communications/evidence district; M04 Last Corridor — bounded logistics-to-city route; M05 Command Node — new Civic Relay complex. |

Sources: `Design/Campaign_Mission_High_Level_Design_Catalog.md`, `Design/SagaChapters/Saga_Chapter01_First_Response.md` through `Saga_Chapter05_Citywide_Command.md`, and `Design/MapVariants/FUTURE_CONTENT_MAP_PLAN.md`. Visual style references: current native `en-campaign.png` from CH04-M03 evidence and SCN-05 V3 chapter/mission select targets.

## Recommended production direction after visual review

1. Use the visual direction selected by the owner after review, retaining the existing typography, portrait system and colored command controls. Use native UI text, icons and progress state, never text baked into generated images.
2. Capture each coded mission's operation map from its own planning camera, with a predictable crop and a versioned mission-to-preview binding. Keep dynamic units, targets, markers and order indicators in runtime UI rather than background art.
3. Give uncoded CH04-M04/M05 and CH05 missions labeled concept previews until their authored physical layouts are qualified. Do not present a concept as a gameplay capture.
4. Review the visual direction before implementing this substantial Campaign UI redesign. Then validate native layouts, input, selection, briefing, complete normal-input mission, ARIA, result and return separately from these mockups.

## Implemented A direction

The SCN-05 Campaign prefab now uses five distinct ImageGen district plates, mission-specific artwork cards, the existing Warline typography and command colors, live chapter progress, the selected mission briefing and operation preview, and a four-control footer. The district plate continues behind the header under a transparent dark shade at both 16:9 and 20:9. [Native layout captures](NativeLayoutQA/README.md) cover each chapter at both widths. These layout captures use fixed selected-mission fixture text; live mission content is verified separately through the normal-input CH04-M03 journey.

The runtime does not imply that a chapter illustration is a single playable district. Coded missions keep their operation-specific preview binding. Planned missions retain comic/story preview imagery until their operation maps are authored.

## ImageGen prompt set

Built-in ImageGen, `ui-mockup`, 16:9 landscape. A–C supplied the current native Campaign capture and SCN-05 V3 visual target as reference images. D–E supplied A and the current native capture as references. Every prompt requested the existing logo/typography, low-poly desert city style, dark frames, colorful command controls, legible mobile touch targets, exact chapter and mission names, and no generic map or developer labels. A requested a district atlas with CH02 mission location cards; B requested a selected CH01 forward-post scene with five mission strips and chapter tabs; C requested five chapter bands with expanded CH03 mission cards. D requested a restrained CH02 district panorama with one row of cards and no node dots or connector lines. E requested a CH04 five-card mission gallery with the selected mission purpose below. All requested a visible mission purpose and `START BRIEFING` control.
