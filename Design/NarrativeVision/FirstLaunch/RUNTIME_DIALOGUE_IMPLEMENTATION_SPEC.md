# First-Launch Runtime Dialogue Implementation Specification

Date: 2026-07-10; amended 2026-07-11 after first live Unity review

Status: active implementation contract; Gate 6 art is approved, and Gate 9R live presentation revision is required before M01

## 1. Scope And Authority

This specification defines the Unity implementation for the approved first-launch runtime dialogue presentation. It covers the reusable dialogue layer used by the first-launch narrative player, its audio/text timing, accessibility behavior, interaction and review semantics, asset imports, data, prefabs, APIs, tests, and performance gates. It does not implement code, create runtime assets, change story text, or authorize use of unapproved panel art.

The implementation must follow, in order:

1. The authority order in `IMPLEMENTATION_TRACKER.md`.
2. The tracker runtime architecture, review, Skip, and gate contracts.
3. `ArtReview/PresentationCandidates/RevisionB_UserFeedback/PRESENTATION_REVISION_BRIEF.md`.
4. `animatic/revision_v2/first_launch_opening_notes.md`, `first_launch_opening_timing_report.tsv`, `first_launch_opening_voice.tsv`, `first_launch_opening_subtitles.srt`, and Gate 5 validation evidence.
5. The user-approved `DIALOGUE-B_GraphicNovel_APPROVED_REFERENCE.png` visual direction.

The revised V2 animatic is a timing and clarity reference, not a shipping video or a frame-accurate runtime timeline. Its Microsoft neural WAV files are temporary offline assets under `animatic/revision_v2/audio/temp_voice/`. They may be imported after Gate 6 for internal/runtime integration only after current distribution rights are verified. Runtime must never call Microsoft or any cloud TTS service.

The tracker's revised Gate 5 result supersedes its older 90-second snapshot: Gate 5 approved the clarity-first `176.5s` linear opening, excluding player-controlled identity and guidance duration. Runtime preserves the V2 story order and line deadlines; tap, Skip, instant text, and auto-advance settings may shorten the experienced route.

## 2. Locked Product Result

- Dialogue is a live Unity Canvas layer over clean cinematic panels.
- The off-white graphic-novel frame is a 9-sliced `UnityEngine.UI.Image`.
- The pointer/notch is a separate optional `Image`; it is never stretched with the frame center.
- Speaker portrait/icon plate, speaker name, role, TMP body, and continue indicator are separate serialized objects.
- Dalia and Samira use their distinct approved portrait crops.
- ARIA uses the exact production focus-reticle sprite at `Assets/Game/Art/UI/Generated/MatchHUD/TargetLockV02/scn08_v02_icon_focus_reticle.png`, tinted with the approved cyan speaker color. Do not use `icon_aria_plate.png` as ARIA's production icon.
- Body text uses TextMeshPro and reveals by visible character while the matching WAV plays.
- Punctuation creates readable pauses without allowing reveal to exceed the voice clip or authored state deadline.
- First tap completes the current text; a later tap advances only when the state permits it.
- Dialogue remains functional with voice disabled, a missing clip, reduced motion, instant text, or paused review playback.
- Commander identity and guidance choice remain separate full-screen live UI states. They are never children of the dialogue card and never baked into story art or video.

### 2.1 Gate 9R User-Review Correction

The first live Unity review invalidates the earlier internal visual/audio acceptance but does not invalidate the reusable player, routing, persistence, Addressables, or approved panel art. These corrections are mandatory before M01:

- At a `1920 x 1080` reference canvas, standard body text starts at `38 px`, speaker name at `36 px`, role at `24 px`, and shipping command labels at `28 px` or larger. Explicit accessibility presets scale from these baselines; normal presentation must not depend on TMP auto-size.
- The normal dialogue frame targets approximately `280 px` height at the reference canvas and expands only for measured text/accessibility need. Empty paper area must not visually dominate the line.
- Shipping controls use at least `88 x 88 px` reference-canvas touch targets, with larger framed targets for text commands. Skip, identity, guidance, and confirmation surfaces use the established mobile HUD scale and visual vocabulary.
- The approved graphic-novel frame remains the dialogue language; navigation, Skip, popups, sliders, and toggles use the current game HUD language. Reviewer-only controls remain development UI but must still be readable.
- Frame corners, edges, alpha, and separate pointer attachment must remain clean at 16:9, 20:9, and tablet scale. Any seam, dark fringe, duplicated border, or stretched pointer fails Gate 9R.
- FL-P01 receives a live localized Sahrin/Old Market location treatment and an ordinary-city establishing hold. It is never baked into panel art.
- Dalia and Samira receive separate readable introduction moments during FL-P04. ARIA receives a distinct activation/introduction during FL-P05 before Commander identity selection.
- The live Unity route, not only an evidence MP4, owns continuous city ambience, restrained score, state-specific conflict/vehicle/radio/blackout events, and voice. Silence at the opening or voice-only playback fails Gate 9R.
- A complete second live user review is required before the FirstLaunch-to-M01 implementation handoff is accepted.

## 3. Concrete Runtime State Contract

The runtime sequence uses the tracker and revised V2 animatic state IDs directly. Engineering must not rename or collapse these IDs during implementation because review mode, Skip, subtitle timing, and future Story Archive replay all key off the same identifiers.

### 3.1 Sequence IDs

- Opening route sequence id: `seq.first_launch.opening`
- Review continuation sequence id: `seq.first_launch.debrief_and_reveal`

### 3.2 Opening Route State Order

```text
first_launch.logo
-> FL-P01
-> FL-P02
-> FL-P03
-> FL-P04
-> FL-P05
-> FL-P06
-> FL-P07
-> first_launch.commander_identity
-> first_launch.guidance_choice
-> FL-P09
-> FL-P10
-> FL-P11
-> FL-P12
-> FL-P13
-> FL-P14
-> FL-P15
-> FL-P16
-> FL-P17
-> FL-P18
-> first_launch.m01_handoff
```

Review-only post-handoff continuation:

```text
first_launch.gameplay_placeholder
-> FL-P19
-> FL-P20
-> FL-P21
-> FL-P22
-> first_launch.command_base_reveal
```

`first_launch.commander_identity`, `first_launch.guidance_choice`, `first_launch.m01_handoff`, `first_launch.gameplay_placeholder`, and `first_launch.command_base_reveal` are first-class states. They must not be hidden as side effects inside neighboring panel records.

### 3.3 Revised V2 Opening Timing And Line Mapping

The revised Gate 5 V2 opening contract is the timing source for the non-interactive route:

| Order | State id | State window | Motion | Dialogue lines |
|---:|---|---|---|---|
| 0 | `first_launch.logo` | `0.0-2.5s` | `Static` | none |
| 1 | `FL-P01` | `2.5-17.5s` | `PushIn` | none |
| 2 | `FL-P02` | `17.5-25.5s` | `StaticImpact` | `p02_radio` |
| 3 | `FL-P03` | `25.5-34.5s` | `DriftRight` | `p03_radio` |
| 4 | `FL-P04` | `34.5-58.5s` | `PushIn` | `p04_dalia`, `p04_samira` |
| 5 | `FL-P05` | `58.5-68.5s` | `PushIn` | `p05_aria` |
| 6 | `FL-P06` | `68.5-78.5s` | `DriftLeft` | `p06_aria` |
| 7 | `FL-P07` | `78.5-88.5s` | `PushIn` | `p07_aria` |
| 8 | `first_launch.commander_identity` | player-paced | `StaticInteractive` | none |
| 9 | `first_launch.guidance_choice` | player-paced | `StaticInteractive` | optional helper VO only |
| 10 | `FL-P09` | `88.5-96.5s` | `PushIn` | `p09_aria` |
| 11 | `FL-P10` | `96.5-104.5s` | `DriftRight` | `p10_aria` |
| 12 | `FL-P11` | `104.5-113.5s` | `PushIn` | `p11_dalia` |
| 13 | `FL-P12` | `113.5-122.5s` | `PushIn` | `p12_samira` |
| 14 | `FL-P13` | `122.5-130.5s` | `DriftLeft` | `p13_aria` |
| 15 | `FL-P14` | `130.5-138.5s` | `PushIn` | `p14_commander` |
| 16 | `FL-P15` | `138.5-148.5s` | `PushIn` | `p15_dalia` |
| 17 | `FL-P16` | `148.5-158.5s` | `DriftRight` | `p16_aria` |
| 18 | `FL-P17` | `158.5-166.5s` | `PushIn` | `p17_dalia` |
| 19 | `FL-P18` | `166.5-176.5s` | `PushIn` | `p18_aria` |
| 20 | `first_launch.m01_handoff` | immediate typed route handoff | transition | none |

The state window is a ceiling for authored timing, not a minimum watch time. Completion tap, Skip, instant text, and auto-advance may shorten experienced duration as long as route validity, identity/guidance validity, and mandatory payload behavior remain intact.

### 3.4 Review Continuation State Contract

| Order | State id | Purpose | Notes |
|---:|---|---|---|
| 20 | `first_launch.gameplay_placeholder` | proves route continuity without pretending M01 exists | review-only; never retail |
| 21 | `FL-P19` | secured-corridor debrief beat | concise consequence |
| 22 | `FL-P20` | revoked-credential clue escalation | ARIA evidence beat |
| 23 | `FL-P21` | field consequence / next-step beat | Dalia operational beat |
| 24 | `FL-P22` | command-base reveal lead-in | ARIA transition beat |
| 25 | `first_launch.command_base_reveal` | earned HQ destination | static or limited motion |

## 4. Runtime Asset Boundary

No runtime object may reference a file under `Design/`.

After Gate 6, approved runtime exports belong under:

```text
Assets/Game/Art/Narrative/FirstLaunch/Dialogue/
    Frames/
    Pointers/
    Portraits/
Assets/Game/Audio/Narrative/FirstLaunch/Voice/
Assets/Game/Configs/Narrative/FirstLaunch/
Assets/Game/Prefabs/UI/Narrative/
```

The current `dialogue_runtime_source/dialogue_frame_9slice.png` is an extraction/reference source. It contains both a frame and a right-side pointer plus excess transparent canvas, so it must not be imported as the final sliced sprite. Produce two trimmed exports from the exact approved revision:

- `dialogue_frame_body.png`: frame body only, no pointer, no excess transparent perimeter.
- `dialogue_pointer.png`: fixed-size pointer only, transparent background, with a defined attachment pivot.

The 9-slice border is set from the approved trimmed body in Sprite Editor. The art owner records the final pixel values in the runtime export manifest. The border must fully contain the chamfered corners, outer shadow, double-rule line, and all non-repeatable distress marks. The center rectangle may contain only continuous paper texture. Automated validation must reject zero borders, a center rectangle that intersects corner geometry, or a frame whose corners change pixel size at minimum/maximum supported dimensions.

The Design reference crops `portrait_dalia.png`, `portrait_samira.png`, and `icon_aria_plate.png` are not automatically production assets. Dalia and Samira exports must be traced to the approved portrait revisions in the Gate 6 ledger. The ARIA production icon remains the existing focus-reticle asset; its surrounding plate is prefab chrome.

## 5. Canvas And Layout

The dialogue prefab uses Screen Space Overlay or the existing shell's screen-space camera Canvas, with the same safe-area root as other first-launch controls. The Canvas Scaler reference resolution is `2400 x 1080`, landscape, `Scale With Screen Size`, match `0.5`. Layout must be verified at `2400 x 1080`, `1920 x 1080`, and a representative tablet landscape viewport.

Layout rules:

- Anchor the dialogue safe root to the device safe area, not raw screen bounds.
- Keep the dialogue group within the storyboard's lower 24 percent reserve when the active panel's authored placement is `Bottom`.
- Support `BottomLeft`, `BottomCenter`, `BottomRight`, `TopLeft`, and `TopRight` authored placements so the card can avoid current faces/action. `TopRight` is invalid when it intersects the Skip reserve.
- Reserve the top-right 12 percent for Skip after the logo state.
- Use authored normalized offsets per state; do not derive placement from face detection at runtime.
- Clamp the card to safe bounds after localization and accessibility sizing.
- Minimum touch target is `48 x 48` density-independent pixels for Continue and Skip.
- The card may grow vertically for localized copy but may not cover a required face, weapon muzzle, protected-civilian read, route marker, identity control, or Skip.
- If maximum supported expansion does not fit the authored placement, validation fails; runtime uses the configured alternate placement, then the compact fallback layout. It never silently shrink-fits body text below the approved minimum.
- The portrait/icon plate has a stable square dimension and never changes based on text length.
- The pointer is optional. Hide it when no unambiguous subject attachment is authored or when it would cross a safe boundary.

Baseline dimensions at the `2400 x 1080` reference resolution are a `1120-1760` card width, `220-260` standard card height, `176 x 176` speaker plate, `40` body font size, `32` name size, and `24` role size. Large/Extra Large text may increase card height to `330` only through an authored alternate placement. Use at least `40` horizontal/vertical content padding and `24` spacing between the speaker plate and body. These values scale through the Canvas Scaler; no script scales font directly from viewport width. Body/name text contrast against its immediate backing must be at least `4.5:1`.

Frame behavior:

- `Image.type = Sliced`, `fillCenter = true`, preserve aspect disabled.
- Pointer uses `Image.type = Simple`, preserve aspect enabled, and a fixed size. Use authored side/orientation; do not place it inside a stretch region.
- Prefer explicit left/right pointer sprites or RectTransform rotation. Do not use a negative scale that reverses child layout or raycast geometry.
- Frame, pointer, portrait plate, portrait/icon, text, and indicator have `raycastTarget = false`; only the full dialogue advance hit area and explicit controls receive raycasts.
- Normal mode may use a restrained frame-in transition and continue-indicator pulse. Reduced motion uses an immediate/static appearance and no pulse.

## 6. Speaker Presentation

Create a `NarrativeSpeakerCatalog` config with one stable record per speaker. Required first-launch records are `aria`, `dalia`, `samira`, `radio.mixed`, and `commander.internal`.

Each record defines:

- stable `speakerId`;
- localized display-name and role keys plus English development fallbacks;
- presentation kind: `HumanPortrait`, `AriaIcon`, `RadioIcon`, or `DynamicCommanderPortrait`;
- portrait/icon sprite;
- frame accent, name, role, and icon tint colors;
- portrait crop mode and optional material;
- accessible speaker label key.

Required treatment:

- `dalia`: approved Dalia crop; display `Major Dalia Rahim`; role identifies JRC field command.
- `samira`: approved Samira crop; display `Engineer Samira Haddad`; role identifies civil infrastructure.
- `aria`: exact production focus-reticle shape, cyan tint, non-human plate. Never substitute a human portrait, typed `[ARIA]` prefix, Civic Relay emblem, or the Design composite plate.
- `radio.mixed`: dedicated radio/channel treatment. Do not reuse Dalia, Samira, or ARIA artwork.
- `commander.internal`: use the committed Commander portrait only after identity selection. Before selection, use the approved neutral/faceless fallback and never imply a canonical face.

The body must not include an authored `[SPEAKER]` prefix. Speaker identity is conveyed by the separate name, role, portrait/icon, and accessibility label. SRT prefixes remain evidence/source metadata only.

## 7. TMP Text And Localization

Use the repository's `Game.UI.Contracts.IGameTextResolver`; do not introduce a second production localization access path as part of this work. Every visible string uses a stable key and an English fallback for development diagnostics. The fallback is not baked into art.

TMP requirements:

- One `TMP_Text` each for speaker name, role, body, optional non-speech caption, and review-only timing diagnostics.
- Body wrapping enabled, rich text enabled only for approved semantic tags, overflow `Truncate` forbidden.
- Disable TMP auto-size for normal body text. Accessibility size is selected from explicit style presets so QA can validate deterministic line breaks.
- Set the full resolved string once at line start, call `ForceMeshUpdate()`, and reveal with `maxVisibleCharacters`. Do not rebuild substrings each frame.
- Count TMP-visible characters from `TMP_TextInfo.characterInfo`; rich-text tags never consume timing slots.
- Combining marks, variation selectors, and zero-width joiners reveal with their base grapheme. Implement a cached visible-index-to-grapheme map when localization expands beyond the initial English set.
- Punctuation timing is based on resolved localized text, with an optional locale-specific `NarrativePunctuationProfile`. Do not assume ASCII punctuation only.
- Screen-reader/accessibility text exposes the complete resolved line immediately; it must not announce one character at a time.

Subtitle settings are on by default. `SubtitlesEnabled = false` hides voiced dialogue body, frame, portrait/icon, and continue indicator as one presentation group while voice and panel playback continue; it does not hide required non-speech captions configured as `Essential`. Reviewer controls and Skip remain visible. Muted-audio acceptance is tested with subtitles enabled.

Add settings without changing their meaning elsewhere:

- subtitle enabled;
- subtitle size: `Small`, `Standard` (default), `Large`, `ExtraLarge`;
- subtitle background opacity: `0`, `50`, `75` (default), `100` percent, applied to a neutral backing layer without erasing the approved paper/frame artwork;
- instant text;
- auto-advance;
- reduced motion.

If these settings are added to `UISettingsModel`, preserve backward-compatible PlayerPrefs defaults and migrate absent keys to the values above. Do not overload `LargeText` or assistant narration settings with different semantics.

## 8. Voice And Typewriter Synchronization

### 8.1 Voice Records

Each dialogue line references its own `AudioClip`. The V2 mapping is the contract for the opening:

| Line range | Speaker | Temporary Microsoft voice | Runtime source |
|---|---|---|---|
| `p02_radio`, `p03_radio` | Radio | `en-US-EricNeural` | matching V2 WAV |
| `p04_dalia`, `p11_dalia`, `p15_dalia`, `p17_dalia` | Dalia | `en-US-MichelleNeural` | matching V2 WAV |
| `p04_samira`, `p12_samira` | Samira | `en-US-AvaNeural` | matching V2 WAV |
| `p05_aria`-`p18_aria` where authored | ARIA | `en-US-AriaNeural` | matching V2 WAV |
| `p14_commander` | Commander | `en-US-ChristopherNeural` | matching V2 WAV |

The actual line list, start time, and hard deadline come from `animatic/revision_v2/first_launch_opening_voice.tsv`. Do not use the mixed MP4/AAC track as a voice source. A line config may replace the temporary WAV with a rights-cleared final clip without changing its stable line ID or localization key.

### 8.2 Clock

- Schedule voice on a dedicated `AudioSource` using `AudioSettings.dspTime`/`PlayScheduled` when practical.
- The reveal clock starts at the scheduled audible voice start, not panel activation.
- While voice is playing, derive progress from `AudioSource.timeSamples / clip.frequency`; DSP time is the scheduling reference and sample position is the resynchronization reference.
- Pause/resume pauses both source and reveal. Review pause, application suspension, route cancellation, and audio-device reset may not let one advance without the other.
- If sample position becomes unavailable or voice is disabled/missing, use accumulated unscaled presentation time.
- On seek/review step, stop the source, clear scheduled audio, reset line state, and restart the target line from its beginning unless the reviewer explicitly selects silent-frame capture.

### 8.3 Authored And Derived Reveal

`AuthoredMarkers` take priority. A marker is `(visibleCharacterIndex, secondsFromVoiceStart)` and markers must be monotonic, in range, and end no later than the reveal deadline. Interpolate character times between markers using the punctuation weights below.

Without markers:

1. Resolve text and visible grapheme indices before playback.
2. Set `availableRevealSeconds` to the earlier of `clip.length - 0.12s` and the authored line deadline relative to voice start minus `0.12s`. If no clip exists, use the configured silent-reading duration.
3. Assign a base weight of `1.0` to each visible non-whitespace grapheme. Whitespace reveals with the preceding grapheme and has no standalone delay.
4. Add pause targets after punctuation: comma `0.09s`; colon or semicolon `0.14s`; sentence period, question mark, or exclamation mark `0.22s`; ellipsis `0.28s`. Consecutive punctuation uses only the largest applicable pause.
5. Allocate the remaining time across base weights, targeting `30` visible characters/second and clamping the base cadence to `18-55` visible characters/second.
6. If base time plus punctuation exceeds the budget, scale punctuation toward zero first, then scale base delays. Never move the final reveal past the budget.
7. If the budget is longer than needed, leave the surplus as a fully revealed tail hold; do not slow type to an unnatural crawl.

The final visible character must appear by the earliest of clip end minus `0.12s`, state deadline minus `0.12s`, or an authored earlier marker. Very short/invalid budgets reveal instantly and log one development diagnostic per line ID.

Silent/missing-audio behavior uses `30` visible characters/second plus the same punctuation targets, clamped to the state deadline. It then holds fully revealed text for `max(0.6s, configuredReadHoldSeconds)` before auto-advance. A missing optional voice clip is never a route blocker.

### 8.4 Instant And Completion

- Instant-text mode sets `maxVisibleCharacters = int.MaxValue` before voice starts; voice and normal state timing still play unless the player advances.
- Tap-to-complete sets all text visible immediately and cancels only the reveal schedule. It does not stop voice.
- The first pointer/submit input that arrives while revealing is consumed exclusively by completion.
- After text is complete, require a `0.15s` debounce before an input may advance.
- A second tap advances only when `advancePolicy` allows manual advance, no modal state owns input, no transition/route request is pending, and any configured minimum line hold has elapsed.
- Manual advance stops the current voice, clears queued line cues, and emits one typed `Continue` request. The view never changes routes itself.
- Auto-advance occurs at voice end plus the configured tail hold, or silent reveal completion plus read hold. It is disabled while identity/guidance/confirmation UI is active and when reviewer auto-advance is off.
- Multiple taps in one frame may produce at most one state transition request.

## 9. State And Input Behavior

Dialogue line phases are explicit:

```text
Hidden -> Preparing -> Revealing -> CompleteHolding -> AdvanceReady -> Exiting
                         | tap          | tap if allowed
                         v              v
                     CompleteHolding -> Exiting
```

Input priority for the full-screen narrative route is:

1. Active confirmation/modal choice.
2. Skip button.
3. Reviewer controls in review mode.
4. Dialogue complete/advance hit area.
5. Non-interactive panel background.

Skip and explicit UI buttons must stop pointer propagation so they cannot also complete or advance dialogue. Identity and guidance states disable the dialogue advance hit area and hide dialogue unless an explicitly non-interactive line is authored outside the choice surface.

On route cancellation or object disable: stop voice, cancel scheduled clips/tweens, clear TMP text and accessibility announcements, unbind callbacks, release panel/dialogue asset handles, and publish no completion result. Composition decides whether cancellation is replaced by a typed handoff.

## 10. Skip And Review Semantics

### 10.1 Production Skip

- Skip appears in the top-right safe area after `first_launch.logo` and remains available with subtitles off or reduced motion on.
- `NarrativeDialogueView` does not own Skip. `NarrativePlaybackControlsView` emits a typed `SkipRequested` action to composition.
- Pending Skip disables repeat input immediately and supplies visual/accessible feedback.
- Before identity exists, Skip opens the concise default-neutral-Commander plus `FullGuidance` confirmation. Confirm uses the same typed commit APIs as normal selection, then routes to `first_launch.m01_handoff`; cancel resumes the same line and sample position.
- After identity/guidance selection, Skip preserves committed values.
- Opening/brief Skip and watched completion create the same typed M01 handoff context.
- Debrief Skip applies the same mandatory clue/completion payload as watched debrief, then enters `first_launch.command_base_reveal`.
- Skip requests are idempotent. Composition owns a route-transition token and publishes at most one handoff/completion result.

### 10.2 Review Mode

Review mode uses transient state and does not write the real profile by default.

- Pause freezes panel motion, voice, reveal, timers, and auto-advance.
- Previous/Next seek to the target panel/state and reset its first line to time zero.
- Scrub/seek resolves a deterministic panel and dialogue line; it does not emit narrative completion, identity, guidance, clue, reward, or route persistence.
- Restart clears transient review choices and starts at `first_launch.logo` unless the reviewer elects to preserve transient identity/guidance.
- Auto-advance off leaves a completed line in `AdvanceReady` until explicit input.
- Subtitle size/background, reduced motion, and safe-area previews rebind the current state without replaying persistence actions.
- Jump To Debrief starts the debrief in transient review state. Skip To Game exercises the typed handoff but targets the review placeholder until M01 exists.
- Capture mode can hide reviewer-only chrome but may not hide production dialogue or Skip unless the requested evidence preset explicitly says so.

## 11. Separate Identity And Guidance UI

Build these as sibling state prefabs under the narrative route root, never inside `NarrativeDialogueView`:

- `CommanderIdentityView`: six free portraits plus neutral fallback, editable/localized-safe Commander name field, valid default, Continue, and explicit commit request.
- `GuidanceChoiceView`: Full Guidance, Tactical Hints, Veteran, default Full Guidance, Continue, and explicit commit request.

Both use the clean Relay/story background supplied by the sequence state. Their selectable cards, focus state, labels, and choices are live Unity UI. The reference images under `interactive_ui/` are visual targets only.

Composition must await a successful typed commit result before leaving either state. Selection data is not encoded into dialogue configs. `commander.internal` speaker binding reads the committed/transient identity through a narrow read model after selection.

## 12. Data Schema

The production config is a ScriptableObject under `Game.Configs`; production never parses the Design JSON/TSV/SRT. Use serializable arrays, stable string IDs, and direct approved asset references or the project's approved async asset-reference mechanism. Do not use Resources path strings.

```csharp
NarrativeSequenceConfig
  int SchemaVersion
  string SequenceId
  NarrativeStateRecord[] States
  NarrativeSpeakerCatalog SpeakerCatalog
  NarrativePunctuationProfile DefaultPunctuation

NarrativeStateRecord
  string StateId
  float DurationSeconds
  float HardDeadlineSeconds
  NarrativePanelRecord Panel
  NarrativeDialogueLineRecord[] Lines
  NarrativeDialoguePlacement DialoguePlacement
  NarrativeAdvancePolicy AdvancePolicy
  NarrativeInteractiveState InteractiveState
  NarrativeStateKind StateKind
  string SkipDestinationId
  NarrativeReducedMotionBehavior ReducedMotion
  NarrativeCompletionPayload CompletionPayload

NarrativeDialogueLineRecord
  string LineId
  string TextKey
  string EnglishFallback
  string SpeakerId
  AudioClip VoiceClip
  float VoiceStartDelaySeconds
  float HardDeadlineSeconds
  float TailHoldSeconds
  float SilentReadHoldSeconds
  NarrativeRevealMode RevealMode
  NarrativeRevealMarker[] AuthoredMarkers
  NarrativeAdvancePolicy AdvancePolicy
  bool EssentialNonSpeechCaption

NarrativeInteractiveState
  NarrativeInteractiveStateKind Kind
  bool BlocksDialogueAdvance
  bool BlocksAutoAdvance
  bool AllowsReviewSeek
  string ConfirmationStateId

NarrativeCompletionPayload
  string PayloadId
  NarrativeCompletionPayloadKind Kind
  bool MandatoryForWatchedRoute
  bool MandatoryForSkipRoute
  string NextRouteId
  string[] StoryArchiveEntryIds
  string[] EvidenceIds
  string[] MissionContextFlags

NarrativeRevealMarker
  int VisibleCharacterIndex
  float SecondsFromVoiceStart

NarrativeSpeakerRecord
  string SpeakerId
  string DisplayNameKey
  string DisplayNameFallback
  string RoleKey
  string RoleFallback
  NarrativeSpeakerPresentationKind Kind
  Sprite PortraitOrIcon
  Color AccentColor
  Color IconTint
```

Validation rules:

- Sequence, state, line, and speaker IDs are non-empty and unique in their scope.
- Every line resolves text and a speaker.
- Every state has a reduced-motion behavior and Skip destination.
- All voice start/deadline values are finite, non-negative, ordered, and within the state.
- Authored reveal markers are monotonic, index-valid, and deadline-valid.
- Every required human speaker has an approved distinct portrait; ARIA resolves to the production focus-reticle GUID.
- Interactive identity/guidance states contain no dialogue-card child prefab or baked choice texture.
- Every production state with optional motion has a static fallback.
- Missing required config fails validation in development; release composition routes to the safe fallback/handoff and logs once.

Required first-launch `StateKind` mapping:

- `first_launch.logo`, `FL-P01`-`FL-P07`, `FL-P09`-`FL-P18`, `FL-P19`-`FL-P22`: `PanelDialogue`
- `first_launch.commander_identity`: `InteractiveIdentity`
- `first_launch.guidance_choice`: `InteractiveGuidance`
- `first_launch.m01_handoff`: `RouteHandoff`
- `first_launch.gameplay_placeholder`: `ReviewOnlyPlaceholder`
- `first_launch.command_base_reveal`: `RouteArrival`

Required first-launch completion-payload behavior:

- Opening panel states before `first_launch.m01_handoff` carry no terminal payload; they contribute only route context.
- `first_launch.m01_handoff` emits the typed M01 handoff payload used by both watched and skipped routes.
- `FL-P19`-`FL-P22` may contribute review-only archive/evidence context but must not write production persistence by default.
- `first_launch.command_base_reveal` emits a review-only route-arrival payload until the real post-M01 owner replaces it.

## 13. Prefab Hierarchy

All runtime references are `[SerializeField]` fields. Runtime implementation must not use `transform.Find`, object-name lookup, `FindObjectOfType`, or hierarchy rebuilding.

```text
NarrativeSequenceView.prefab
  SafeAreaRoot
    PanelLayerRoot
      BackgroundImage
      MidgroundLayerRoot
      CharacterLayerRoot
      ForegroundLayerRoot
      EffectLayerRoot
      StaticFallbackImage
    DialoguePlacementRoot
      NarrativeDialogueView
        AdvanceHitArea                    [Button]
        FrameGroup
          BackgroundOpacityImage
          FrameImage                      [Image, Sliced]
          PointerImage                    [Image, Simple]
          SpeakerPlateImage
          SpeakerPortraitOrIconImage
          SpeakerNameText                 [TMP_Text]
          SpeakerRoleText                 [TMP_Text]
          BodyText                        [TMP_Text]
          EssentialCaptionText            [TMP_Text]
          ContinueIndicatorImage
    ProductionControlsRoot
      NarrativePlaybackControlsView
        SkipButton
          SkipIcon
          SkipLabelText
    InteractiveStateRoot
      CommanderIdentityView               [inactive sibling]
      GuidanceChoiceView                  [inactive sibling]
      DefaultSkipConfirmationView         [inactive sibling]
    AccessibilityAnnouncementRoot
    ReviewRoot                             [development/editor only]
      NarrativeReviewControlsView
      SafeAreaOverlay
      IdAndTimingOverlay
```

`NarrativeDialogueView` serializes every image, TMP text, RectTransform, CanvasGroup, Button, and Animator/tween host it uses. `OnValidate` reports missing references. Reviewer roots are excluded or inactive behind development/editor compilation and never exposed by a retail route.

## 14. Runtime APIs And Ownership

Planned source allocation:

| Assembly | Planned source |
|---|---|
| `Game.Configs` | `Assets/Game/Scripts/Configs/Narrative/NarrativeSequenceConfig.cs`, `NarrativeSpeakerCatalog.cs`, `NarrativePunctuationProfile.cs` |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts/Narrative/NarrativeUiContracts.cs`, `NarrativeIdentityContracts.cs` |
| `Game.UI.Runtime` | `Assets/Game/Scripts/UI/Narrative/NarrativeSequenceView.cs`, `NarrativeDialogueView.cs`, `NarrativePlaybackControlsView.cs`, `NarrativeSequencePresentationSystemHelper.cs`, `NarrativeDialogueRevealSystemHelper.cs`, `NarrativeVoicePlaybackSystemHelper.cs` |
| `Game.Composition` | `Assets/Game/Scripts/Composition/Narrative/FirstLaunchNarrativeCompositionSystemHelper.cs` |
| `Game.Editor` | `Assets/Game/Scripts/Editor/Narrative/FirstLaunchNarrativeConfigValidator.cs`, review entry/capture tooling |
| Tests | `Assets/Tests/Editor/Narrative/` and `Assets/Tests/PlayMode/Narrative/` |

Add `Game.Configs` as a `Game.UI.Runtime` assembly reference only after confirming no reverse dependency. `Game.UI.Contracts` remains Unity-object-free; Editor tooling may depend on runtime/config assemblies, never the reverse.

### `Game.UI.Contracts`

Define pure serializable IDs/actions/results with no Unity object references:

```csharp
enum NarrativeUiActionKind
{
    CompleteText,
    Continue,
    Skip,
    ConfirmDefaultAndSkip,
    CancelSkip,
    CommitCommanderIdentity,
    CommitGuidance,
    ReviewSeek,
    JumpToDebrief
}

readonly struct NarrativeUiAction
{
    string SequenceId;
    string StateId;
    string LineId;
    NarrativeUiActionKind Kind;
    ulong TransitionToken;
}

readonly struct NarrativeHandoffResult
{
    string DestinationId;
    CommanderIdentityData Commander;
    GuidanceMode Guidance;
    NarrativeCompletionPayload Completion;
    ulong TransitionToken;
}
```

Use repository-compatible serializable forms if `readonly struct` fields conflict with inspector/serialization requirements; preserve the semantic contract.

Additional required contracts:

```csharp
enum NarrativeInteractiveStateKind
{
    None,
    CommanderIdentity,
    GuidanceChoice,
    DefaultSkipConfirmation,
    ReviewGameplayPlaceholder
}

enum NarrativeStateKind
{
    PanelDialogue,
    InteractiveIdentity,
    InteractiveGuidance,
    RouteHandoff,
    ReviewOnlyPlaceholder,
    RouteArrival
}
```

### `Game.UI.Runtime`

- `NarrativeSequenceView.BindActions(Action<NarrativeUiAction>)` / `UnbindActions()`.
- `NarrativeSequenceView.ApplyPanel(in NarrativePanelPresentationModel)`.
- `NarrativeDialogueView.ApplySpeaker(in NarrativeSpeakerPresentationModel)`.
- `NarrativeDialogueView.PrepareLine(string resolvedText, NarrativeSubtitleStyle style)`.
- `NarrativeDialogueView.SetVisibleCharacterCount(int count)` and `CompleteLine()`.
- `NarrativeDialogueView.SetPhase(NarrativeDialoguePhase phase)`.
- `NarrativeDialogueView.SetAccessibilityText(string completeLine)`.
- `NarrativePlaybackControlsView.SetSkipState(bool visible, bool interactable, string accessibleLabel)`.
- `NarrativeSequencePresentationSystemHelper.Start/Pause/Resume/Seek/Cancel/Tick` owns timing and presentation state only.
- `NarrativeDialogueRevealSystemHelper` builds a cached reveal schedule and advances `maxVisibleCharacters`; it has no routing/profile access.
- `NarrativeVoicePlaybackSystemHelper` owns the dedicated voice `AudioSource`, sample clock, pause/resume, and stop behavior.

Views may emit actions but never call scene, route, profile, save, or gameplay APIs.

### `Game.Composition`

`FirstLaunchNarrativeCompositionSystemHelper`:

- resolves config, text, settings, profile/read models, and route destinations;
- binds/unbinds all views;
- owns production versus transient review state;
- commits identity/guidance;
- converts Continue/Skip/completion actions into state transitions;
- owns the idempotent transition token;
- publishes the typed M01 handoff or review placeholder result;
- applies watched and skipped completion payloads through the same code path;
- safely cancels playback on route exit.

Do not send story art/config or typewriter state through ECS simulation hot paths.

## 15. Audio And Texture Import Settings

### Voice WAV

- Source: mono PCM WAV, `48 kHz`, matching V2/final source without MP4 extraction.
- Force To Mono: on; Normalize: off unless the approved audio pipeline explicitly normalizes all dialogue.
- Load Type: `Compressed In Memory` for normal line clips; use `Decompress On Load` only if measured decode latency requires it and memory remains within budget.
- Compression Format: Vorbis, quality starting at `70`; compare intelligibility/sibilance on target devices before lock.
- Preload Audio Data: current and next state only through the selected loading mechanism; do not keep the entire sequence resident without evidence.
- Load In Background: on where supported by the sequence preloader.
- Ambisonic: off.
- Do not stream these short synchronization-critical lines unless device evidence proves stable sample-accurate behavior.
- Route voice through the existing Voice mixer/settings path. `VoiceEnabled` and voice volume must be honored independently of ambience/SFX.

### Runtime Music, Ambience, And Event Sound

- Use separate sequence-owned sources or existing mixer/bus bindings for Voice, Music, continuous Ambience, and one-shot Event/SFX playback. Do not flatten these layers into the runtime voice clip or ship the review MP4 mix.
- City/market ambience starts with FL-P01 and remains clearly audible before the attack. It changes state without hard cuts unless the blackout intentionally requires one.
- The restrained score starts audibly at the opening, ducks beneath speech, changes with the attack/ARIA/handoff arc, and obeys Music Enabled/Volume independently.
- Explosion, distant conflict, radio, blackout, ARIA boot, vehicle movement, and transition cues are keyed by stable state/cue IDs and obey Sound/SFX settings independently.
- Pause/resume preserves synchronization; seek/restart reconstructs the correct loop and one-shot state without duplicating events; Skip/cancel/handoff stops all sequence-owned sources and callbacks.
- Tests must prove voice-only, ambience-only, music-only, fully muted, missing-optional-cue, pause/resume, seek/restart, and route-cancel behavior.

### Frame And Pointer

- Texture Type: Sprite (2D and UI), Sprite Mode Single, sRGB on, Alpha Is Transparency on.
- Read/Write off; mipmaps off; wrap Clamp; filter Bilinear; no physics shape.
- Mesh Type Full Rect for 9-slice reliability.
- Trim source canvas before import; preserve at least 2 transparent pixels around antialiased outer edges.
- Frame has recorded non-zero Sprite Border and is used only as `Image.Type.Sliced`.
- Pointer is a separate sprite with authored pivot at its frame attachment point.
- Start with lossless/RGBA32 in review; select platform compression only after phone captures show no ringing, dark alpha fringe, or damaged line art. Prefer ASTC `6x6` or better on mobile if visual evidence passes.

### Portraits And ARIA Icon

- Sprite (2D and UI), sRGB on, alpha transparency on, Read/Write off, mipmaps off, Clamp, Bilinear.
- Preserve transparent edge padding and use Full Rect unless a measured batching/overdraw issue requires Tight.
- Portrait maximum size starts at `512`; source crop is not upscaled merely to fill that limit.
- ARIA focus-reticle should be reimported with Read/Write off; its current source importer has Read/Write enabled and must be corrected when runtime integration begins, after checking no existing consumer depends on CPU access.
- ARIA cyan is applied by `Image.color`; do not create duplicate color-baked icon textures.

## 16. Performance And Failure Behavior

- Zero recurring managed allocation during stable dialogue playback after warmup.
- No per-frame LINQ, string concatenation, localization lookup, TMP text assignment, asset lookup, component discovery, or hierarchy changes.
- Build the reveal schedule once per resolved line into reused buffers.
- Update `maxVisibleCharacters` only when the visible count changes.
- Cache speaker styles and resolved view references.
- Current and next panel/dialogue packages may be resident; release prior unrelated packages after transition.
- Measure transition hitch, audio start latency, frame time, peak texture/audio memory, and GC allocation on the target development device.
- Missing optional portrait motion, decorative cue, or voice falls back without blocking progression.
- Missing human portrait uses a clearly logged neutral development fallback; a release build must not silently show another named character.
- Missing ARIA icon fails development validation; release uses a neutral non-human fallback, never a human portrait or typed prefix alone.
- Missing frame uses the compact accessible neutral backing, preserving readable TMP text and routing.
- Route exit guarantees no stale voice, subtitle, callback, scheduled audio, or continue request survives.

## 17. Automated Tests

### EditMode

- Config rejects duplicate/missing sequence, state, line, and speaker IDs.
- Every first-launch line resolves a localization fallback, speaker, Skip destination, and reduced-motion behavior.
- V2 line IDs map to the intended distinct speaker and WAV; start/deadline order matches `animatic/revision_v2/first_launch_opening_voice.tsv`.
- Non-interactive opening state ordering, motion tags, and state deadlines match `animatic/revision_v2/first_launch_opening_timing_report.tsv`.
- ARIA sprite GUID is the production focus-reticle GUID, not `icon_aria_plate.png`.
- Dalia and Samira resolve different approved portrait assets.
- Frame sprite has non-zero borders; pointer is a distinct sprite; frame/pointer runtime assets do not live under `Design/`.
- Texture importer tests enforce UI sprite type, mipmaps off, Read/Write off, Clamp, alpha, Full Rect for the frame, and the recorded 9-slice border.
- Voice importer tests enforce mono, `48 kHz` source preservation, synchronization-safe load type, Ambisonic off, and no MP4-derived clips.
- Reveal schedules are monotonic, punctuation-aware, rich-tag-safe, and never exceed clip/state deadline.
- English punctuation cases cover comma, colon, semicolon, period, question, exclamation, ellipsis, consecutive punctuation, and no punctuation.
- Empty, one-character, all-whitespace, malformed rich text, combining-mark, and very short deadline inputs do not throw or stall.
- Instant text starts complete; missing/disabled voice uses silent cadence.
- Runtime assemblies contain no hierarchy/object-name search; Editor references remain outside runtime assemblies.

### PlayMode

- Voice sample progress and TMP reveal remain synchronized through pause/resume.
- First tap during reveal completes without advancing or stopping voice.
- Second eligible tap emits one Continue and stops voice; same-frame repeated taps emit once.
- Auto-advance waits for voice/reveal and tail hold; disabled auto-advance waits indefinitely in `AdvanceReady` without allocation.
- Missing/corrupt optional audio reaches the next state and never stalls.
- Subtitle off hides the complete dialogue group while Essential non-speech captions remain.
- Subtitle size/background presets, 20:9, 16:9, and tablet safe areas do not overlap required subject bounds or Skip.
- Reduced motion removes frame/pointer/indicator animation without changing story order or audio deadlines.
- Identity and guidance own input, commit through typed actions, and cannot click through to dialogue.
- Skip before identity requires confirmation and commits neutral identity plus Full Guidance once.
- Skip after selection preserves identity/guidance; watched and skipped handoff payloads are equivalent.
- Debrief Skip preserves mandatory completion/clue payload.
- Reviewer seek/pause/step does not modify production profile or emit completion.
- Route cancellation releases voice/text state and produces no stale continuation.

### Visual And Performance Evidence

- Golden captures at `1920 x 1080`, `2400 x 1080`, and tablet landscape for Dalia, Samira, ARIA, radio, and dynamic Commander treatments.
- Minimum and maximum card dimensions prove invariant 9-slice corners and undistorted pointer.
- Maximum supported English expansion and a pseudo-localized expansion capture prove safe layout.
- Subtitles off, instant text, reduced motion, missing audio, and missing optional visual fallback captures.
- Profiler evidence shows zero recurring allocation in a 30-second stable hold, recorded audio start latency, peak resident audio/texture memory, and no visible transition hitch.

## 18. Definition Of Done

Runtime dialogue implementation is accepted only when:

- Gate 6 approved runtime exports, not Design references, are integrated.
- The approved 9-sliced graphic-novel treatment is readable at phone scale with an undistorted separate pointer.
- Dalia, Samira, ARIA, radio, and Commander are visually and accessibly distinct.
- TMP reveal is synchronized to the individual WAV clips, honors punctuation, and cannot miss line/state deadlines.
- Tap completion/advance, auto-advance, instant text, subtitles, reduced motion, voice-off, and failed-audio paths are deterministic and non-blocking.
- Identity and guidance remain separate live states and all Skip paths preserve valid typed state.
- Review controls are complete, transient by default, and absent from unintended retail routes.
- Required EditMode, PlayMode, device, visual, memory, and allocation evidence passes the tracker Gate 7 through Gate 10 contracts.
- No runtime code or asset loads from `Design/`, no MP4 ships as playback, and no runtime network TTS exists.
