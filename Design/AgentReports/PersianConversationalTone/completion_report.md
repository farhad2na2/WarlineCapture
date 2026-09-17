# Conversational Farsi rollout — 2026-09-15

The approved conversational Iranian Persian tone is now used by the first-launch dialogue, M1–M5 mission recordings and shared ARIA feedback. Visible dialogue and instructional copy use the same tone. ARIA addresses the player directly; commanders retain appropriate team address and their existing voices. Button names, objectives, resource amounts and localization placeholders are preserved.

## Delivered recordings

| Area | Farsi recordings |
| --- | ---: |
| First launch, including alternate commander voices | 19 |
| M1 tutorial | 6 |
| M2 narrative and tutorial | 16 |
| M3 narrative and tutorial | 23 |
| M4 narrative and tutorial | 19 |
| M5 narrative and tutorial | 15 |
| Shared ARIA command and status feedback | 163 |
| **Total** | **261** |

All 261 files are installed locally with manifest hashes, durations, caption provenance and the `fa-conversational-v1` delivery profile. The 12 previously missing M4 Farsi tutorial recordings now have audio-event bindings. Existing clip paths and GUIDs are retained where present. The old M1 Engage event alias resolves to the current conversational attack lesson.

The central localization catalog and the separate comic locale asset were updated from canonical source copy. Additional nonvoiced guidance was rewritten for the same conversational tone. Parameterized feedback retains its detailed caption, while 33 spoken responses use meaningful sentences rather than reading placeholder substitutes aloud. First-launch takes respect existing panel limits; other comic voice windows accommodate both languages. No runtime network speech generation is used.

The generators share the approved delivery settings. Cache and manifest matching now checks the delivery profile and source text, preventing reuse of stale formal recordings. M4 comic-only generation preserves tutorial records. The rollout can resume without buying another copy of unchanged cached audio.

## Issues corrected during verification

- Removed excessive trailing silence from four shared responses while retaining a short natural tail.
- Replaced one M4 tutorial take whose opening word was unclear in both local and provider transcription. The replacement transcript matches the intended instruction.
- Restored 39 missing build-popup localization bindings and updated its prefab builder to preserve them on future rebuilds.
- Preserved complete comic playback windows and Farsi tutorial event routing during import.

## Verification

Offline validation passed for all 261 recordings: payload/source agreement, spoken placeholders, caption hashes, audio hashes, duration metadata, mono 44.1 kHz PCM format, first-launch timing limits and tutorial UTF-8 capacity. Signal checks found no clipping, excessively low peaks, or leading/trailing silence above the review threshold. All **359 non-Farsi WAVs** in the pre-rollout snapshot remain byte-identical.

Eleven representative final recordings were uploaded to ElevenLabs Scribe with explicit user approval. All were identified as Farsi; transcripts preserve the instructions, including M2 resource amounts, M3 Hold guidance, M4 boarding/landing-zone instructions and Stop feedback. These are automated transcription spot checks, not exhaustive native-speaker listening of every clip. The earlier local transcription report is retained as an initial diagnostic; its M4 lesson 10 take was superseded.

Editor validation uses the checked macOS GUI-licensing wrapper in an isolated QA copy. The combined runner rebuilds the build popup, checks canonical Farsi captions and M4 audio bindings, then runs localization, M3/M4 presentation, M2 narrative, M4 comic audio, command feedback and first-launch text suites. Exact final results are recorded in `editor_validation_result.json`.

Re-run offline checks with:

```sh
python3 Tools/Audio/validate_persian_conversational_rollout.py
```

Run Editor checks from a fresh QA copy of the current authored project (some narrative tests rebuild assets):

```sh
Tools/CI/invoke_unity_macos.sh --project <qa-copy> --timeout 600 --log /private/tmp/warline-persian-rollout-validation.log -- -quit -executeMethod PersianConversationalRolloutValidation.Run
```

Read the required `[PersianConversationalRolloutValidation] result=Passed` marker; a zero wrapper exit alone is insufficient. Keep Unity Hub open. Validation is Editor-only, as requested. This report covers the Farsi tone rollout and associated regressions; it does not claim another full gameplay playthrough of every mission.

## Review artifacts

- `voice_payload_review.json`: exact approved 261-script request set.
- `generation_result.json`: generation completion.
- `validation_result.json` and `audio_signal_checks.json`: whole-set offline checks.
- `transcript_spot_checks.json`: final sampled transcripts and file hashes.
- `m04_retake_transcript.json`: accepted M4 replacement take.
- `caption_migration.json` and the copy-change files: reviewed source changes.
