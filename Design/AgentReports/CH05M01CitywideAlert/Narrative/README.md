# Citywide Alert narrative

The authoritative copy contains seven complete canonical Chapter5 opening beats and seven mission lines (brief3/comms1/debrief3), English and conversational Persian. The opening explicitly distinguishes facts, predictions and recommendations, preserves Commander approval, and presents Qassem's permanent-authority demand without voicing a Commander decision.

Mechanics match the acknowledged contract: keep clinic and utility fronts intact, clear each front's attackers, bring an original engineer to each recovery point, hold six seconds, and restore both services. Radar, mixed threats and ordinary production support the two fronts. Optional Smoke requires a bounded approval and declining/ignoring it neither blocks victory nor spends Fuel. No new gameplay controls or automatic orders are promised.

## Assets and integration

The existing three Citywide Alert images were visually inspected and retained. The editor builder copies the briefing image through AssetDatabase into an independent opening-art asset, then authors seven distinct opening framings and mission-specific crops for16:9 and20:9. No asset/scene YAML is hand-edited.

Four sequences: `seq.ch05.open.citywide_command`, `seq.ch05.m01.brief`, `.comms`, `.debrief`. Opening completion payload is `request.citywide_alert.open.complete`; root owns normal progression, chapter-opening acknowledgement and Archive routing. `BuildAndInstall()` registers all four through the Menu bootstrap editor API. `SeedCopyLocalization()` exposes all fourteen captions to the shared localization catalog. `InstallComicVoices()` binds fourteen EN and fourteen FA clips with compressed mono, no preload, and deadlines based on the longer language recording.

Preview-only bookends.json's abridged Chapter5 opening was replaced by the seven canonical lines; all other sequence topology and content were preserved exactly.

## Voices and gates

Established ElevenLabs cast: Dalia, Samira, ARIA project voices, Laila Sarah and Qassem George. Generator reads canonical C# copy, prepares the exact local payload, and emits resumable offline mono44100HzPCM16 normalized clips and checksum manifest. Runtime network TTS is disabled.

Local payload check passed fourteen bilingual pairs and 28 clips. Automatic approval review rejected the first upload; after the exact question, the user replied “Approve these 28 Citywide Alert clips”. The approved generation command passed automatic review. All 28 clips are now generated; WAV checksums, canonical captions, caption hashes, established cast IDs, mono 44100Hz PCM16 format and recorded durations passed. Durations range from 10.24 to 19.04 seconds. See `voice_validation.json` and the audio manifest for evidence. Native playback and integration remain separate gates.

Compilation, installed caption/crop inspection, full natural EN/FA playback, ordinary mission completion, approval/decline branches, Archive/result/return and human/device acceptance are separate root-owned gates.
