# First-Launch Presentation Revision Brief

Date: 2026-07-10

Status: Core presentation direction approved; revised pacing/audio animatic remains

Selected dialogue reference: `dialogue_candidates/DIALOGUE-B_GraphicNovel_APPROVED_REFERENCE.png`, accepted by the user on 2026-07-10 as a good reference. It is a target mockup, not a runtime sprite or flattened playback asset.

## User Review Findings

The first animatic is a timing and story reference only. It is not intended to ship as a baked video.

1. The initial Old Market environment was effectively silent. The opening must carry audible market life before the attack: people, carts, distant traffic, birds/wind, work tools, and the aircraft cue.
2. The subtitle rectangle looked like programmer art. Dialogue needs a polished comic presentation language rather than a generic full-width dark bar.
3. ARIA dialogue needs the actual in-game cyan focus-reticle icon, not only a typed `[ARIA]` speaker prefix.
4. Dalia and Samira dialogue needs their approved, correct portraits and visually distinct speaker treatment.
5. Commander identity and guidance choices must be live game UI states. No selectable menu, popup, button, or choice surface may be baked into cinematic art or a video.
6. The sequence moved to gameplay before clearly establishing location, situation, factions, and recurring characters.

## Revised Product Form

- The game uses the reusable real-time narrative player planned by the tracker. Cinematic art, portraits, effects, subtitles, speaker chrome, ARIA icon, Skip, and interaction surfaces remain separate runtime layers.
- Any MP4 is review evidence only. It is never the retail playback source.
- Story panels contain no baked dialogue, menus, buttons, portraits-as-UI, Skip control, or selection state.
- Identity and guidance interrupt the narrative as real Unity UI states and return to the same narrative state machine after selection.
- Dialogue chrome may overlap the scene only as a runtime layer and must never cover the current story subject.

## Clarity And Pacing Revision

Target a deliberate `150-180s` default route to first gameplay, with Skip available throughout and fast defaults for repeat players.

| Section | Target | Clarity job |
|---|---:|---|
| Minimal logo and location | `0-10s` | Name Sahrin and Old Market; establish dawn. |
| Living Old Market | `10-30s` | Let the player hear and see normal civilian life before disruption. |
| Coordinated failures | `30-50s` | Show separate power, road, and command failures without premature attribution. |
| Dalia introduction | `50-70s` | Name Dalia, identify JRC field response, and show rescue competence. |
| Samira introduction | `70-90s` | Name Samira, establish her civic role, and clarify civilian stakes. |
| Relay and ARIA introduction | `90-115s` | Explain what ARIA is, what is damaged, and why the player is contacted. |
| Commander identity and guidance | `115-140s` | Use separate live game UI with valid defaults; never bake choices into art. |
| Situation and first order | `140-180s` | Clarify JRC, armed threat, protected civilians, connected route, and the bounded first command. |

Exact duration is locked only after the revised presentation animatic is readable without prior design-document knowledge.

## Imagegen Exploration Set

Produce presentation mockups before any further final-panel generation:

1. Location/narration treatment over living Old Market.
2. Dalia dialogue treatment with approved portrait.
3. Samira dialogue treatment with approved portrait.
4. ARIA dialogue treatment using `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_focus_reticle.png`.
5. A real interactive Commander identity screen shown as a separate game UI state, not part of cinematic art.
6. A real guidance-choice screen shown as a separate game UI state.

Explore comic-style angular captions, portrait medallions, restrained speaker colors, and compact text regions. Reject full-width programmer bars, speech bubbles that obscure faces/action, pseudo text, and baked controls.

Completed reference package:

- `PRESENTATION_REVISION_CONTACT.png`: six-frame comparison.
- `dialogue_candidates/LOCATION-A_SahrinOldMarket.png`: location and narration treatment.
- `dialogue_candidates/DIALOGUE-B_GraphicNovel_APPROVED_REFERENCE.png`: user-approved ARIA dialogue direction.
- `dialogue_candidates/DIALOGUE-C_DaliaPortraitRole.png`: correct Dalia portrait, full name, and role.
- `dialogue_candidates/DIALOGUE-D_SamiraPortraitRole.png`: correct Samira portrait, full name, and role.
- `interactive_ui/UI-COMMANDER-IDENTITY_Reference.png`: separate live identity UI.
- `interactive_ui/UI-GUIDANCE-CHOICE_Reference.png`: separate live guidance UI.

The user accepted the graphic-novel image as a good reference and explicitly required 9-sliced speech bubbles with character-by-character text during speech. That direction is locked for the revised animatic and later runtime implementation.

## Runtime Dialogue Construction Contract

- Build the selected graphic-novel frame as a 9-sliced Unity `Image`; corners and border weight remain fixed while the center and edges stretch for localized text.
- Keep the pointer/notch as a separate optional sprite or a 9-slice-safe fixed attachment. Do not distort it with the center region.
- Keep the speaker icon/portrait plate, speaker name, role line, body text, and continue indicator as separate serialized objects.
- ARIA uses the production `scn08_v02_icon_focus_reticle.png` shape tinted cyan. Human dialogue uses approved Dalia/Samira portrait crops.
- Render body copy with TextMeshPro. Never bake speaker text or dialogue into the frame sprite.
- Reveal text character-by-character while voice plays. Use authored reveal timing when available; otherwise derive a bounded cadence from clip duration and visible-character count.
- Add short punctuation pauses for comma, sentence end, colon, semicolon, question mark, and exclamation mark without allowing reveal to outlast the voice/state deadline.
- Tapping while text reveals completes the line instantly. A second tap advances only when advancement is allowed.
- Provide an accessibility setting for instant text and honor reduced motion by disabling cursor pulse and decorative frame motion.
- Voice-disabled and failed-audio paths still reveal readable text and never stall the sequence.

## Acceptance Before Final Art Resumes

- The location, situation, Dalia, Samira, ARIA, JRC, civilian stakes, and armed threat are understandable in sequence.
- Opening ambience is audible under dialogue and remains present before the first attack.
- ARIA uses the exact in-game icon.
- Human speakers use approved portraits.
- Dialogue treatment reads as premium comic presentation at phone scale.
- Every interactive choice is demonstrably a separate game UI state.
- The user-approved graphic-novel direction is recorded. Final-panel production remains paused until the revised pacing/audio animatic proves the complete sequence clearly.
