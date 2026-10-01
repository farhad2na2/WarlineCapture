# Missing campaign comic voices — 2026-10-01

## Exact scope

| Mission | Current canonical gap | Approved recordings |
| --- | --- | --- |
| CH01-M04 Airlift | Brief lines 01 and 02 changed during map migration; previous recordings do not match current captions | 4 replacement clips, EN and FA |
| CH02-M02 Supply Line | All seven briefing/comms/debrief lines captioned without voice assets | 14 new clips, EN and FA |
| CH04-M03 Split Front | All eight briefing/comms/debrief lines captioned without voice assets | 16 new clips, EN and FA |

The exact 34 recording requests contain 5,646 caption characters. Current canonical sources are `M04AirliftCopyCatalog.cs`, `CH02M02SupplyLineCopy.cs` and `CH04M03SplitFrontCopy.cs`. Payload preparation performs no external requests. The owner subsequently approved the concrete 34-clip upload to ElevenLabs in this conversation, superseding the recorded pending Split Front upload approval.

Generation uses the existing project characters, `eleven_v3`, the approved conversational Iranian Persian delivery profile, an existing active paid subscription and local runtime WAV assets. No runtime network TTS is used. Dalia, Samira and ARIA keep their established project voices; Laila uses the established Sarah voice and Qassem the established George voice. The exact request bodies, cast destinations and captions are in [voice_payload_review.json](voice_payload_review.json).

## Generation and installation

`Tools/Audio/generate_missing_mission_comic_voices.py` prepares the review payload by default. Its `--generate-approved-payload` mode refuses canonical copy changes, checks the paid subscription, skips hash/text/profile matches and saves a manifest after each completed clip. Existing Airlift tutorial records and unchanged comic recordings are retained. Source MP3 conversion uses bounded temporary storage and produces mono 44.1 kHz PCM16 WAVs with the project's -18 LUFS processing.

Live Unity installation is owned by the coordinating agent. The following methods patch existing narrative sequences and Persian locale references without opening scenes:

- `Game.Editor.M04AirliftNarrativeBuilder.InstallComicVoices()`
- `Game.Editor.CH02M02SupplyLineNarrativeBuilder.InstallComicVoices()`
- `Game.Editor.CH04M03SplitFrontNarrativeBuilder.InstallComicVoices()`

The Supply Line builder now preserves available voice references on later narrative rebuilds. Airlift's map-copy refresh and media importer previously unconditionally detached the two changed briefing recordings. They now preserve replacement clips only when their manifest caption, expected path and file SHA-256 match the current canonical line; stale geographic recordings still detach. Each installer uses compressed-in-memory Vorbis voice imports with background loading and no preload, extends caption/state deadlines to cover both recordings and validates expected line/clip counts.

`Game.Editor.MissingMissionComicVoiceValidation.Validate()` performs read-only focused Unity checks for all 34 approved imported/bound clips, current EN/FA captions, unique FA bindings, localized runtime resolution and caption/state windows. It does not claim listening or normal-input mission acceptance.

## Evidence and acceptance

- Exact payload preparation passed: 17 bilingual pairs, 34 clips, 5,646 caption characters, zero external requests during preparation.
- Generator Python syntax check passed; Git whitespace check passed.
- Existing manifest integrity audit passed: 18 voice manifests, 352 clip records, no missing files or SHA-256 mismatches. This proves file integrity, not caption agreement or audible performance; the two Airlift caption mismatches were audited separately.
- Generation completed with `[MissingMissionVoiceGeneration] result=Passed clips=34 runtimeNetworkTts=0`.
- Decoded waveform QA passed: 34 clips, both locales, 36,976,092 source bytes (35.3 MiB). Every recording matches the current canonical caption, expected existing character voice and SHA-256; all are mono 44.1 kHz PCM16, have non-silent signal, no clipped samples and reasonable duration. Exact records are in [generated_waveform_qa.json](generated_waveform_qa.json). The coordinating agent independently reran this validator successfully.
- Unity compilation, imported/bound clip checks and audible native comic playback require the coordinating agent's validation.
- Human listening and player/device acceptance remain separate pending gates; generated files alone do not establish mission readiness.

No Unity process was started, stopped, compiled or controlled by the voice agent. No shared Unity scene or serialized asset was hand-edited.

## Coordinating agent integration

All three voice-only installers ran in the primary connected Editor, in Edit mode. The imported narrative assets and Persian locale were saved through Unity APIs. Compilation finished with `compiling=False failed=False`; all 34 current EN/FA clip bindings passed the focused validator, including localized voice resolution, exact captions, unique locale records and caption/state duration windows. The Airlift guard accepted all four refreshed clips and rejected four synthetic stale-caption inputs without modifying assets.

Evidence: [full integration log span](unity-voice-integration.log), [pass markers](unity-pass-markers.txt), [waveform QA](generated_waveform_qa.json).

The initial synchronous AssetDatabase refresh returned a Pipeline main-thread timeout after 5 seconds while imports and compilation continued. A subsequently scheduled integration callback did not produce completion markers across the domain reload; it is not counted as successful evidence. After the Editor finished compiling, each installer was called directly and returned successfully, followed by the binding validator and stale-caption guard check. No Editor or licensing process was terminated or restarted.

Status: generated, imported and bound; automated waveform and Unity binding checks passed. Audible native comic playback, human pronunciation/performance review and real player/device acceptance remain pending. No normal-input mission playthrough was performed for this voice-only change.
