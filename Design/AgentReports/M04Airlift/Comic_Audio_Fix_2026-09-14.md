# M4 comic audio repair — 2026-09-14

## Cause

The seven M4 narrative lines had no English voice references and their Persian locale entries also had null clips. The recordings were absent from the project, so translated captions did not have matching spoken dialogue.

The shared `first_launch_radio_emergency_event_01.wav` effect contained unintended English speech despite its original generation prompt requesting no speech. Each M4 comic panel requested the Radio event, replaying that same seven-second effect. Local CPU Whisper inspection transcribed English words from this effect; the other sampled narrative music/ambience did not produce speech. This was not a localized M4 actor recording looping.

## Change

- Generated the explicitly approved seven English/Persian script pairs through the existing paid ElevenLabs account: 14 clips, 156.40 seconds, from the canonical M4 copy catalog. Voice provenance, captions and SHA-256 hashes are recorded in `m04_voice_manifest.json`.
- Installed all English story references and Persian locale overrides. Each panel accommodates the longer localized recording plus one second.
- Replaced the shared radio mapping with `Shared/narrative_radio_squelch_01.wav`, a deterministic 0.8-second filtered-noise effect with no speech sources. Its local generator and provenance are checked in. The previous generated effect remains as historical source material and is no longer bound to the narrative prefab or cue plan.
- Updated the prefab builder so regeneration preserves the new sound. The fix applies to every comic using this shared presentation.
- Added a comic-only generator option and importer so this repair does not request or modify tutorial audio. Artwork, game rules and save data are unchanged.

## Validation

- Canonical payload check: seven story pairs / 2,036 characters.
- All 14 WAVs passed mono/44.1 kHz, non-silence, unclipped-sample and manifest hash checks.
- Editor import and serialized binding checks passed: 14 localized clips, three sequences, seven sufficient narration windows, speech-free non-looping radio.
- Two focused regression tests passed, including actual shared-prefab regeneration and independent audio-layer cancellation.
- Local speech recognition returned `[BLANK_AUDIO]` for the replacement effect; its deterministic construction contains only pseudorandom filtered noise.
- Real-time playback through the shipping comic player passed in `/private/tmp/m04-comic-voice-playback.log`: all 14 localized recordings played once to completion, with no wrong-language clip in Farsi, no voice restart, and the speech-free radio on a non-looping source. All three authored sequences were exercised per language.
- All 44 unrelated Persian voice mappings, including M5, were preserved.
- This focused pass exercises the actual comic player and prefab; it does not claim a new full M4 gameplay playthrough.

Editor tests run in isolated project copies. No user save data, Editor session or Hub process was terminated. The first probe compile preflight failed on two namespace imports; those were fixed and the corrected preflight passed. No screenshots are stored in Design.
