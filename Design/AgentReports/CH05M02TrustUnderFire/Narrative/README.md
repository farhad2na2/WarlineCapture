# Trust Under Fire narrative

Seven played panels: briefing3, radio1, debrief3, each in English and conversational Persian. Existing cast: Samira3, Dalia2, Dr. Lina1, ARIA1 lines; fourteen total voice clips.

The approved story centers on keeping two evacuation routes open, protecting named convoys to functioning shelters, and securing the verified broadcast source while preserving equipment and witness/signal evidence. The copy uses the certified convoy fallback and makes no unsupported boarding promise. The source compound is identified by maintenance noise, confirmed with signal records, then secured at its external access point.

Samira acknowledges mixed earned reputation: some residents remember help, others remember delays and fear. Today's proof is safe arrivals and open routes. The debrief recognizes those actual outcomes without uncritical praise or denying remaining damage. There is no invented Trust meter; access to the truth is unconditional.

## Integration

`CH05M02TrustUnderFireNarrativeBuilder.BuildAndInstall()` creates three own sequences and registers them through the Menu bootstrap editor API. Main asset name matches its filename. All seven panels receive distinct approved-art framings at16:9 and20:9. No new Chapter opening or ChapterIV close is inherited. Completion payloads are `request.trust_under_fire.{brief,comms,debrief}.complete`; debrief uses the existing DebriefArrival route.

`SeedCopyLocalization()` seeds all seven EN/FA captions. `InstallComicVoices()` requires fourteen files and seven EN bindings, writes the seven Persian bindings, uses compressed mono with preload disabled, and gives each caption the longer language duration plus margin.

Audio manifest path: `Assets/Game/Audio/Narrative/CH05M02TrustUnderFire/trust_under_fire_voice_manifest.json`. Voice paths use `Voice/en` and `Voice/fa` with canonical `trust_under_fire-*` IDs. Runtime network TTS is disabled.

## Voice approval and readiness

The local exact payload check passed seven bilingual pairs, fourteen clips, zero external requests. Automatic approval review rejected the upload because it required exact new text/destination authorization. Root is requesting that exact approval. No provider upload, retry or workaround occurred.

Unity integration, crop/caption inspection, natural EN/FA playback, full normal-input evacuation/relay mission, ARIA, result/return, earned Archive replay and real player/device acceptance remain separate root-owned gates.
