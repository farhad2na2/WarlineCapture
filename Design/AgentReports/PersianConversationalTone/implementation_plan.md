# Approved conversational Persian rollout

The user approved the M3 ARIA sample on 2026-09-15 and requested the same tone everywhere. Apply natural Iranian Persian to first-launch dialogue, M1–M5 tutorials/comics, mission guidance and shared spoken command feedback. Keep character identities, button labels, gameplay facts, placeholders and English recordings intact.

Delivery baseline: the approved preview's existing character voice, `eleven_v3`, Persian language `fa`, stability 0.5, similarity 0.75, and `[conversational tone]`. Keep delivery tags out of visible captions. Short direct clauses, singular player address for ARIA, appropriate plural address when commanders speak to a team, no audiobook narration or invented filler.

1. Inventory canonical copy, runtime locale keys, events, audio paths and manifests, including alternate commander voices.
2. Rewrite the Persian source copy and matching visible instructions. Record exact payloads before generation. Centralize the approved delivery profile so future regenerations keep the tone.
3. Generate the Persian recordings using the existing paid account and cast; keep asset paths/GUIDs stable. Cache generation inputs and results for resumability. Update hashes, durations and spoken-text provenance in manifests.
4. Reimport localization and audio in the Editor; validate source/caption/voice coverage, unchanged English assets, placeholders, actual locale routing, comic timing and representative UI layouts. Keep evidence images outside Design.

Completed all four stages. All 261 recordings are installed, matching captions are imported, and offline plus six-suite Editor validation passed. Eleven final transcription samples were checked with explicit upload approval. See [completion_report.md](completion_report.md) for scope, fixes, results and limitations. No runtime network TTS is introduced.
