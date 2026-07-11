# FirstLaunch Phase 8 Validation Report

Status: In progress; implementation foundation and first visual acceptance evidence complete
Date: 2026-07-11
Gate: Gate 7 remains open pending integrated playback and reduced-motion capture

## Completed Evidence

- English text catalog: `Assets/Game/Data/Narrative/FirstLaunch/first_launch_english_text_catalog.json`
  - 17 voiced subtitle lines with stable localization keys.
  - Five distinct speaker identities, labels, roles, and accessible labels.
  - Six essential non-speech captions.
- Audio cue plan: `Assets/Game/Data/Narrative/FirstLaunch/first_launch_audio_cue_plan.json`
  - Existing local project assets selected for market ambience, impact, radio interruption, blackout, ARIA boot, identity confirmation, handoff, and temporary score.
  - Voice, ambience, score, muted-playback, and offline-TTS rules recorded.
- Temporary voice assets: `Assets/Game/Audio/Narrative/FirstLaunch/Voice/`
  - 17 stable-ID Microsoft Edge neural WAVs imported as local mono clips.
  - Rights status is `TEMP_INTERNAL_ONLY_DISTRIBUTION_RIGHTS_UNVERIFIED`; these clips are not shipping-cleared.
  - Runtime network/cloud TTS is prohibited.
- Dialogue presentation prefab: `Assets/Game/Prefabs/UI/Narrative/FirstLaunch/FirstLaunchNarrativeSequence.prefab`
  - Approved comic frame imported with a non-zero 9-slice border.
  - Pointer is a distinct sprite and transform.
  - Dalia and Samira portraits remain distinct assets.
  - ARIA uses the exact production Match HUD focus-reticle sprite.
  - TMP text, speaker identity, Skip, safe-area root, and dedicated voice source are serialized.
- Reusable config assets: `Assets/Game/Configs/Narrative/FirstLaunch/`
  - One connected 26-state sequence graph covers all 22 approved panels plus identity, guidance, handoff, gameplay placeholder, and command-base arrival states.
  - All 17 voice lines bind stable keys, speaker IDs, timing windows, and local clips.
  - Five-speaker and punctuation profiles are separate reusable assets.
- Accessibility settings:
  - Narrative subtitles default on.
  - Four subtitle-size presets and four background-opacity presets persist independently of ARIA subtitles.
  - Instant text and auto-advance preferences persist.
  - Reduced motion migrates from the legacy preference and is consumed through `SettingsService`.

## Visual Evidence

- `dialogue_standard_1920x1080.png`: standard subtitle size and 75% background preset.
- `dialogue_max_expansion_2400x1080.png`: extra-large setting with an intentionally expanded English line.

The first maximum-expansion render exposed text crossing the lower frame/pointer zone. The prefab was corrected with explicit text-safe offsets and bounded TMP auto-sizing. The retained capture is the corrected render: text remains inside the frame and does not overlap the portrait, pointer, or Skip control.

## Automated Evidence

- Narrative settings persistence: 5 focused tests passed.
- Temporary voice imports: 1 focused batch test passed across 17 clips.
- Presentation/typewriter/import/prefab/auto-advance: 6 focused tests passed.
- English catalog and local audio cue plan: 2 focused tests passed.
- Sequence graph/config/speaker assets: 3 focused tests passed (`26` states, `22` panels, `17` lines, `5` speakers).
- Unity compile and prefab generation passed under Unity 6000.5.2f1.

## Muted Clarity Audit

Every spoken fact in the V2 opening has a complete English subtitle entry. Story-critical sound-only events have essential captions for market explosions, radio static, power failure, relay blackout, ARIA boot, and tactical-link establishment. Subtitles and essential captions remain runtime UI layers; no text is baked into panel art.

## Remaining Before Gate 7

- Bind the catalogs, voice clips, and presentation prefab into the reusable sequence player.
- Demonstrate auto-advance and voice/reveal synchronization in integrated playback.
- Capture subtitles-off, reduced-motion, and muted playback states.
- Run internal integrated presentation acceptance.
