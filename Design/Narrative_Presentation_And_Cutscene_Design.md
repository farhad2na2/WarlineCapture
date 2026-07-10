# WarlineCapture Narrative Presentation And Cutscene Design

Date: 2026-07-10

Status: Active high-level narrative presentation authority

Scope: Presentation format, sequence tiers, visual language, character continuity, audio/subtitle behavior, AI-assisted asset policy, and narrative UX. This is not an implementation or asset-production tracker.

Upstream authorities: `AAA_Mobile_Game_Design_Document_v0_2.md`, `Campaign_Narrative_Bible.md`, and `First_Player_Experience_And_Story_Onboarding_Design.md`.

Direct consumers: `Level_And_Mission_Content_Plan.md`, `SagaChapters/README.md`, Story Archive design, character/story-art production, audio/localization planning, and later sequence-player implementation plans.

## Recommendation

Use a grounded illustrated tactical motion-comic format for the majority of Campaign storytelling. It can deliver character continuity, mission-specific locations, military hardware, and chapter drama at a realistic mobile production cost without requiring fully animated character cinematics.

The format should feel like an operational graphic novel made from the same world as the 3D game:

- Approved character reference sheets based on actual project models.
- Painted or stylized 3D environment references.
- Limited parallax, camera moves, light, smoke, dust, radio overlays, and map graphics.
- Short voiced or subtitle-led exchanges.
- Immediate transitions into the playable 3D operation map.

AI image generation can accelerate source art, but the shipped product uses reviewed, edited, versioned assets generated before runtime. No story sequence depends on live generative AI.

## Experience Goals

- Give every mission a human reason and consequence.
- Make the Commander, ARIA, Dalia, Samira, and Qassem recognizable across the campaign.
- Carry the `Shattered Relay` mystery without long exposition.
- Enter and exit scenes quickly enough for mobile sessions.
- Preserve player control and allow replay/skip.
- Reuse one presentation grammar across cold opens, briefings, debriefs, recaps, and chapter transitions.

## Visual Style

### Target Style

`Grounded stylized military graphic novel`:

- Realistic proportion and equipment structure translated into painterly, slightly simplified shapes.
- Strong silhouettes and environmental detail that remain readable on a phone.
- Natural sun, dust, smoke, practical light, and regionally appropriate material color.
- Restrained panel borders and tactical overlays; avoid comic-book comedy effects.
- Faces communicate urgency and restraint rather than exaggerated hero poses.
- The city remains visible as a lived place: shops, clinics, workers, apartments, traffic remnants, irrigation, trees, walls, and repair activity.

### Prohibited Shortcuts

- Generic stock-like soldiers unrelated to project models.
- Random uniforms, weapons, insignia, vehicles, or faces between scenes.
- One-note yellow/orange "desert filter" over every location.
- Real terrorist symbols, flags, political leaders, news logos, or identifiable current conflicts.
- Unreviewed generated Arabic or other regional text.
- Graphic civilian injury, victim spectacle, or celebratory destruction.
- Depicting civilians and insurgents as visually interchangeable for a twist.

## Sequence Tiers

| Tier | Use | Target length | Typical content |
|---|---|---:|---|
| A: Campaign bookend | First-launch prologue, Chapter 5 ending. | 30-75 sec | 5-10 images/panels, multiple characters, location progression, major revelation. |
| B: Chapter interlude | Chapter opening and finale transition. | 25-60 sec | 4-8 images/panels, Protocol Fragment, new threat family, relationship change. |
| C: Mission brief/debrief | Immediate objective and consequence. | 10-20 sec each | 2-4 panels, operation map, one character exchange, threat/clue. |
| D: In-mission comm beat | Urgent update without taking control. | 3-8 sec | Portrait, subtitle, radio waveform, optional camera/objective focus. |
| E: Recap/archive card | Returning-player recap and evidence review. | Player-paced | Still image, short text, character and clue metadata. |

Not every mission needs both a full brief and debrief cinematic. A strong interactive briefing or result card can carry Tier C when the story beat is small. Chapter finales and the first session deserve the most authored presentation.

## Campaign Sequence Map

| Campaign moment | Minimum presentation |
|---|---|
| First launch | Tier A `Command Lost` cold open, ARIA boot, identity transition. |
| M01 start | Seamless final prologue panel into operation-map camera. |
| Each standard mission | Tier C brief or interactive story briefing; Tier D updates; concise result beat. |
| Each chapter opening | Tier B interlude establishing changed city state and new feature pressure. |
| Each chapter finale | Tier B reveal of one Protocol Fragment. |
| Chapter 4 opening | Clear visual escalation from insurgent cells to organized proxy armor and aircraft. |
| Final mission | Tier A pre-assault and ending sequence, followed by epilogue panels. |
| Return after absence | Optional Tier E recap, never forced long playback. |

## Scene Grammar

Every narrative sequence should answer no more than four questions:

1. What changed?
2. What must the Commander do now?
3. Who is affected?
4. What new question or answer remains?

A typical mission briefing follows:

```text
Location and human context
-> Confirmed threat action
-> Operational objective
-> Risk or constraint
-> Transition to command
```

A typical debrief follows:

```text
Immediate outcome
-> Civilian/infrastructure consequence
-> Character response
-> Evidence or mystery beat
-> Next-operation hook
```

## Character Continuity

### Reference Package Per Principal Character

Each recurring character eventually requires an approved reference package containing:

- Stable character id and config/model anchor.
- Front, three-quarter, profile, and expression references.
- Uniform/clothing and equipment callouts.
- Color and material reference.
- Height and proportion relationship.
- Allowed chapter-specific damage or wardrobe states.
- Prohibited insignia and visual errors.
- Voice, subtitle name, and portrait crop rules.

The high-level casting anchors are defined in `Campaign_Narrative_Bible.md`.

### Continuity Rules

- A named character keeps the same face, age range, body type, equipment family, and faction identity.
- ARIA has one stable avatar language and is never represented by the Commander portrait.
- The player's selected Commander portrait appears in dialogue and archive surfaces; a fixed battlefield proxy must not be presented as the player's literal face unless avatar support exists.
- Insurgent specialist models keep consistent threat roles across panels and gameplay.
- Civilian models remain civilian across all media.
- Damage continuity follows mission order and resets only when time or treatment justifies it.

## Location Continuity

Story art should use the actual operation-map design as its spatial authority.

- Roads, gates, major buildings, airfields, bases, ridgelines, markets, and Relay structures must agree with gameplay.
- Briefing art may simplify distance but cannot promise a path, entrance, elevation, or cover route absent from the map.
- The last pre-mission image should establish the same approach angle, landmark, weather, and time of day that the player sees at mission start where practical.
- Debrief art reflects authored destruction and survival states without inventing outcomes.

## Middle Eastern Setting Direction

The presentation should draw from a broad fictional regional blend while remaining specific within its own world.

Required qualities:

- Dense and sparse districts, not only desert compounds.
- Historic and contemporary construction.
- Markets, apartments, workshops, clinics, schools, industrial yards, farms, oil infrastructure, airfields, and civic buildings.
- Vegetation, irrigation, grass, dirt, stone, bushes, road wear, and seasonal variation.
- Clothing and interiors appropriate to work, climate, class, and location, not a single costume stereotype.
- Signs and speech reviewed by fluent humans.
- Music and vocal performance reviewed for authenticity and political neutrality.

## AI-Assisted Image Production Policy

### Appropriate Uses

- Style exploration before lock.
- Storyboards and composition thumbnails.
- Background and environmental concept candidates.
- Character scene candidates using approved reference images.
- Lighting, weather, and chapter palette studies.
- Clean-up bases for paintover and compositing.

### Inappropriate Uses

- Runtime generation.
- Unreviewed final faces, hands, weapons, vehicles, uniforms, insignia, or text.
- Direct imitation of a living artist or copyrighted franchise style.
- Recreation of real victims, current attacks, political leaders, or news imagery.
- Automatic variation of a principal character without continuity review.
- Any generated asset whose provenance or commercial-use status cannot be recorded.

### Human Review Gate

Every candidate image must be checked for:

- Character identity and faction continuity.
- Correct project model, weapon, vehicle, and environment references.
- Hands, faces, anatomy, equipment attachment, and geometry.
- Accidental real symbols, flags, writing, logos, and political references.
- Cultural plausibility and stereotype risk.
- Civilian dignity and age depiction.
- Terrain, road, building, and camera continuity with the playable map.
- Mobile crop, subtitle-safe space, contrast, and compression artifacts.
- Provenance record and source-reference rights.

## Audio And Dialogue

### Voice Strategy

| Stage | Direction |
|---|---|
| High-level prototype | Subtitles, temp timing, radio filters, and existing audio-event support. |
| Vertical slice | Final or near-final voices for ARIA, Dalia, Samira, Qassem, and the Commander-neutral response set. |
| Campaign production | Localized performance and pronunciation review; mission specialists added by chapter need. |

AI-generated temporary voice may support internal timing only if legally permitted and clearly marked. Final voice policy requires consent, commercial rights, localization quality, and actor/voice-clone safeguards.

### Dialogue Rules

- ARIA: concise observation, reason, recommendation, uncertainty when appropriate.
- Dalia: tactical reality, troop state, timing, and executable options.
- Samira: civilian needs, infrastructure facts, and legitimacy consequences.
- Qassem: controlled argument and manipulation; no theatrical ranting or religious rhetoric.
- Commander choices: short professional intent such as `Protect the corridor`, `Confirm the target`, or `Preserve the evidence`; do not invent a fixed personality.
- Avoid exposition characters telling each other facts they already know.
- Use regional terms only when culturally reviewed and understandable in context.

## Subtitles And Accessibility

- Subtitles on by default.
- Speaker name and role available.
- Adjustable size, opacity/background, and display duration.
- Critical lines persist in a message log or Story Archive.
- No critical information only in ambient speech.
- Caption important non-speech events when they carry story meaning.
- Do not place subtitles over objectives, touch controls, faces, or critical battlefield action.
- Cinematics are skippable and replayable.
- Reduced-motion mode removes aggressive parallax, shake, and rapid zoom.
- Auto-advance can be disabled for player-paced reading.

## Interaction And Control

- Standard scenes expose skip/pause and subtitle controls without opening a complex menu.
- In-mission Tier D beats do not lock camera or input unless the player explicitly opens a story detail.
- An urgent gameplay warning overrides non-critical dialogue.
- A narrative camera focus must return safely to the player's previous command context.
- ARIA `Show Me` remains a gameplay preview, not a cutscene disguised as control.
- Story scenes never issue hidden gameplay orders.

## Story Archive

The Story Archive organizes:

- Prologue and chapter interludes.
- Mission brief/debrief sequences.
- Protocol Fragments.
- Character profiles unlocked through the main story.
- Supporting evidence from optional objectives.
- A concise chapter recap.

The archive marks unseen content but never makes optional evidence look mandatory. It must distinguish verified evidence, ARIA inference, and antagonist propaganda.

## Technology Direction At High Level

- Use one reusable data-driven sequence presentation for most image, text, audio, timing, and transition needs.
- Use Unity Timeline only for exceptional sequences requiring complex synchronization with 3D animation, cameras, audio, or gameplay objects.
- Store story state separately from tactical Match state but connect it through stable mission and evidence ids.
- Preload only the assets needed for the next short sequence on memory-constrained devices.
- Provide a static-panel fallback if motion or optional audio cannot load.

Unity Timeline is suitable for cinematic and gameplay sequences, but the product should not require a bespoke Timeline asset for every short mission exchange: https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.timeline.html

This is a technology recommendation only; exact runtime architecture belongs in a later implementation plan.

## High-Level Acceptance Criteria

- The sequence advances a specific mission or character question.
- Principal characters and project assets are visually consistent.
- The setting feels lived-in and regionally grounded without referencing a real current conflict.
- The scene reaches its point within its tier's time budget.
- Skip, subtitles, replay, and reduced motion are supported by design.
- The transition into or out of gameplay preserves spatial and objective clarity.
- No unreviewed generated text, symbols, faces, equipment, or cultural content ships.
- Story-critical information remains available without audio and without spending.

## Deferred To Implementation Planning

Sequence schemas, Unity component ownership, Timeline tracks, render pipelines, image-generation batches, voice vendors, file naming, memory budgets, test matrices, estimates, and task ownership will be defined after high-level approval.
