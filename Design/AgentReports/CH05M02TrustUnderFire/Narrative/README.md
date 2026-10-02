# Trust Under Fire narrative

Seven played panels: briefing3, radio1, debrief3, each in English and conversational Persian. Existing cast: Samira3, Dalia2, Dr. Lina1, ARIA1 lines; fourteen total voice clips.

The approved story centers on keeping two evacuation routes open, protecting named convoys to functioning shelters, and securing the verified broadcast source while preserving equipment and witness/signal evidence. The copy uses the certified convoy fallback and makes no unsupported boarding promise. The source compound is identified by maintenance noise, confirmed with signal records, then secured at its external access point.

Samira acknowledges mixed earned reputation: some residents remember help, others remember delays and fear. Today's proof is safe arrivals and open routes. The debrief recognizes those actual outcomes without uncritical praise or denying remaining damage. There is no invented Trust meter; access to the truth is unconditional.

## Integration

`CH05M02TrustUnderFireNarrativeBuilder.BuildAndInstall()` creates three own sequences and registers them through the Menu bootstrap editor API. Main asset name matches its filename. All seven panels receive distinct approved-art framings at16:9 and20:9. No new Chapter opening or ChapterIV close is inherited. Completion payloads are `request.trust_under_fire.{brief,comms,debrief}.complete`; debrief uses the existing DebriefArrival route.

`SeedCopyLocalization()` seeds all seven EN/FA captions. `InstallComicVoices()` requires fourteen files and seven EN bindings, writes the seven Persian bindings, uses compressed mono with preload disabled, and gives each caption the longer language duration plus margin.

Audio manifest path: `Assets/Game/Audio/Narrative/CH05M02TrustUnderFire/trust_under_fire_voice_manifest.json`. Voice paths use `Voice/en` and `Voice/fa` with canonical `trust_under_fire-*` IDs. Runtime network TTS is disabled.

## Voice approval and readiness

The local exact payload check passed seven bilingual pairs and fourteen clips. Initial automatic approval reviews rejected generation; root then supplied the complete preceding exact question plus the direct human reply, and automatic review accepted that stronger authorization evidence. Root generated all fourteen clips. Independent WAV/caption hashes, canonical copy, existing cast IDs, mono44100Hz PCM16, durations and seven conversational Persian profiles passed. Earlier failed approval evidence is retained in `voice_generation_gate.json`; full audit is in `voice_validation.json`. Native installation passed in `voice-install-01.log` (exit 0). Full normal-input English and Persian journeys passed in `voiced-journey-en-02.log` and `voiced-journey-fa-01.log` (exit 0), each with seven naturally completed voices and seven dialogue lines, actual victory, rewards and Campaign return.

Native visual review, automated rules/packing, complete normal-input EN/FA gameplay and natural voice playback are recorded separately in `../review-readiness.md`. Real player/device acceptance remains pending user review.
