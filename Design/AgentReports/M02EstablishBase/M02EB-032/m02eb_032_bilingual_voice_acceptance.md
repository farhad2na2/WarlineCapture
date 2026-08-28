# M02EB-032 Bilingual Voice Acceptance

Date: 2026-08-29
Status: Accepted

## Integrated Voice Set

- Narrative: 18 WAV files, covering 9 lines in English and Persian.
- Tutorial: 14 WAV files, covering 7 instructions in English and Persian.
- Total: 32 mono PCM source files at 44.1 kHz.
- Speakers: Dalia, Samira, and ARIA use their established ElevenLabs voice identities.
- Runtime catalog: 14 M2 tutorial events on the Voice bus, nonspatial, with no runtime-load fallback.

Manifest hashes:

- Narrative voice manifest: `fb2813511b8a3f638f85c33c7759c19c24c85d8395a8cd33bccb6ed3feebe74b`
- Tutorial ARIA manifest: `bfd320d30d875d39d1d657cdb46288dd52157c4f1e381cd62adb7bb038bca558`

Importers are idempotent and set the production voice policy: compressed in memory, Vorbis quality 0.7, preserve 44.1 kHz, mono, background loading, no preload, and non-ambisonic. Provider, model, voice, locale, license, and offline-availability metadata are recorded in each manifest.

## Validation

- Manifest SHA validation passed for all 32 WAV files.
- Import metadata and exact event-catalog identities passed in the all-M2 suite.
- `TutorialVoiceAssetsMatchEveryDisplayedInstruction` passed.
- `FinalSequencesBindReviewedPanelsAndEnglishVoice` and Persian locale parity passed.
- Shared M1 English/Persian narration compatibility passed 21/21 in `MatchHudAssistantUiSystemHelperTests`.
