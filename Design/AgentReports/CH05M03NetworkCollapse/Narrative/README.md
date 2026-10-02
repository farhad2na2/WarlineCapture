# Network Collapse narrative

Seven comic panels: briefing3, radio1, debrief3. Canonical English and conversational Persian captions use the existing Dalia, ARIA and Samira cast identities. The story exposes Qassem's authorship, keeps the audit chain intact, and leads to the Last Corridor delivery mission. There is no Chapter opening or close sequence.

## Supported mission actions

The ground recon/capture fallback is explicit. Clear each guard group; bring the original engineer to the external recon point for six seconds; only the confirmed node becomes a valid Attack target. Repeat for three ordered nodes. Secure the audit with a six-second original-engineer custody hold, then Board the APC, Move to extraction, and hold six seconds. Protect two civic buildings, their four staff, the engineer and the carrier. Copy promises no unsupported Scan, long-range targeting or paid objective.

Radio follows the first isolated verified node and reports the opening record while the player must preserve the remaining physical chain. Debrief confirms the complete surviving audit and Qassem's override, attempted erasure, Ash Line bombings, false reports, diverted Fuel and Vanguard hardware. The Relay complex remains sealed; physical access keys and civic supplies still need the city-center corridor.

## Caption-only integration

`CH05M03NetworkCollapseNarrativeBuilder.BuildAndInstall()` and `BuildWithoutVoices()` both build caption-only assets. They register own `seq.ch05.m03.brief`, `.comms`, `.debrief` sequences and seven distinct approved-art panels at16:9 and20:9. Main asset naming matches the filename. `SeedCopyLocalization()` seeds seven captions into both locale tables.

Entry states are `NetworkCollapse-{brief,comms,debrief}-0`. Completion payloads are `request.network_collapse.{brief,comms,debrief}.complete`. Debrief uses the normal DebriefArrival route. The root integration owns verified-node blocking radio, required debrief, earned audit Archive replay and result/return.

English voice references are explicitly null, including gender variants. Persian caption records are installed while own Persian voice records are removed. No previous mission voice clips are reused. Caption deadlines are based on text length. Voice readiness logs `Pending`, separately from successful comic/caption construction.

## Local voice draft

The user explicitly requested building without sending voices. `Tools/Audio/prepare_network_collapse_voice_payload.py` writes only a local fourteen-clip draft for seven bilingual lines with existing cast IDs and conversational Persian direction. It has no upload option, network implementation, account access or secret read. `voice_payload_draft.json` records `LocalDraftOnly_NoUploadAuthorized`, installed clips0 and external requests0. No voice manifest or audio assets are generated.

## Gates

Local source and draft caption-hash checks passed. Unity import, installed panel/caption inspection, natural EN/FA comic traversal, normal-input ordered recon/attack/custody/boarding/extraction, ARIA, result/return, Archive replay and real player/device acceptance remain separate root-owned gates. Voices are intentionally pending.
