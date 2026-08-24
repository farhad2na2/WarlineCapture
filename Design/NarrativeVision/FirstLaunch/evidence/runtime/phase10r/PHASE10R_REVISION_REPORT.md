# FirstLaunch Phase 10R Revision Report

Status: Implementation batch passed; final live full-playback review remains open.
Date: 2026-07-11
Unity: 6000.5.2f1

## Presentation

- Standard dialogue typography is fixed at 50 px body, 54 px speaker, and 30 px role; TMP auto-size is disabled.
- Standard dialogue has a 292 px minimum; Large and Extra Large accessibility modes have a 376 px minimum. Resolved text is measured after a forced Canvas/TMP mesh pass, and the 9-sliced frame expands upward to fit the complete rendered line block within the safe area. Ellipsis overflow is disabled.
- Narrative surfaces use a 2.2x local presentation scale inside the production Menu CanvasScaler contract of 4800 x 2160. The earlier output-sized capture harness was rejected because it concealed the live scale regression.
- The comic frame remains the dialogue treatment. Shipping controls, identity, guidance, and confirmation surfaces use the current Match HUD panel and button assets.
- Shipping touch targets are at least 88 px high. Reviewer controls are enlarged and top-pivoted so they remain inside the safe area.
- The broken right pointer attachment is disabled. Corrected 9-slice borders render without the reviewed black edge artifact.
- FL-P01 now presents live `SAHRIN` and `OLD MARKET / 10:00 LOCAL` text from localization-ready config keys. Its enlarged plate uses a top-left pivot and remains inside the safe area.

## Narrative Audio

The sequence prefab owns separate Voice, Music, Ambience, Vehicle, and Event sources. They remain runtime UI/audio objects and are not baked into panel art or video.

| Layer | Runtime source | Settings authority |
|---|---|---|
| Voice | Temporary character/ARIA clips | Master x Voice; Voice enabled |
| Music | Dedicated calm and crisis story loops | Master x Music; Music enabled |
| Ambience | Dedicated city market, city attack, and command-room loops | Master x SFX; Sound enabled |
| Vehicle | Dedicated convoy-interior loop | Master x SFX; Sound enabled |
| Events | Restrained distant-attack and emergency-radio textures plus ARIA boot | Master x SFX; Sound enabled |

The eight environment/score clips were generated with ElevenLabs `eleven_text_to_sound_v2`. The retained manifest records prompts and candidate metrics. Prompts explicitly exclude rhythmic booms, pulses, close impacts, and whooshes. After the second live review, city-attack, command-room, and emergency-radio assets were regenerated with explicit no-human-voice/no-spoken-syllable prompts. The radio event is no longer wired during dialogue, so only the dedicated narration source can hold a dialogue clip. All six loops have measured endpoint discontinuities below -40 dBFS. The rejected generic large-explosion, battlefield, objective, and transition assets are no longer referenced by FirstLaunch.

Audio state selection and settings-to-volume policy are owned by `Game.Composition`. `NarrativeSequenceAudioView` remains a passive serialized reference and source presentation view in `Game.UI.Runtime`; assembly-boundary validation passes 31/31.

Pause/resume applies to every layer. Restart, seek, previous/next, and state entry deterministically replace the active beds/cue. Cancel, Skip, and route teardown stop every narrative source and clear its clip.

## Evidence

- `dialogue_standard_1920x1080.png`
- `dialogue_long_1920x1080.png`
- `dialogue_standard_2400x1080.png`
- `dialogue_tablet_1920x1200.png`
- `location_intro_1920x1080.png`
- `identity_1920x1080.png`
- `guidance_1920x1080.png`
- `skip_confirmation_1920x1080.png`
- `reviewer_controls_1920x1080.png`

An initial `-nographics` capture produced uniform gray output and was rejected. The retained PNGs were regenerated with GPU rendering and visually inspected.

## Validation

- Unity script compilation: passed.
- Focused Phase 10R presentation/audio validation: 9/9 passed, including rendered-line dynamic expansion and strict narration-source separation.
- Assembly-boundary validation: 31/31 passed.
- Editor performance/residency validation: passed with zero managed allocations across 1,800 warm ticks and at most two resident panel handles.
- Consolidated FirstLaunch config/presentation/player/menu regression validation: 29/29 passed.
- Live Menu PlayMode integration through the real 4800 x 2160 CanvasScaler, Addressables boot, 2.2x presentation scale, seven-portrait Commander chooser, actual Commander and Guidance commits, FL-P09 single narration clip with an empty event source, interactive states, gameplay placeholder, debrief Skip, and command-base arrival: 1/1 passed.
- Non-ECS naming validation has an unrelated integrated-head inventory failure for `UiShellStateSystem` and `UiShellFlowSystem`; the FirstLaunch diff adds no bare non-ECS `*System` type.
- Final Gate 9R requirement: user live review of full playback with audio and the available reduced-motion, subtitles, Skip, restart, and seek controls.
