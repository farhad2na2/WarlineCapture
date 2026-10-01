# Grounded Signal narrative production

Eight canonical comic lines cover briefing (3), post-unload comms (2), validated victory debrief (3). The initial authored route is runway unloading, military relay disablement, hardware recovery, APC extraction. No selectable airborne route is promised.

Existing mission-specific comic images are retained: `CH04M04_GroundedSignal`, `_Comms`, `_Debrief` under `Assets/Game/Resources/FutureMissionComics`. All three were visually inspected: airfield preparation, physical connector verification and recovered command schedule. New portraits bind the established pilot and bombsuit unit cards.

## Voice cast

- Laila: established Sarah `EXAVITQu4vr4xnSDxMaL`.
- ARIA: established project voice `Fi9tPTnEcbh3of7hOHC8`.
- Karim: new stable role assignment to existing stock Daniel `onwK4e9ZLuTAKqWW03F9`.
- Yusuf: new stable role assignment to existing stock Eric `cjVigY5qzO86Huf0OWal`.

The account voice inventory was checked read-only: no existing project Karim/Yusuf voice existed. Root approved these distinct existing stock assignments. No voice clone or new provider voice is created.

`Tools/Audio/generate_grounded_signal_bilingual_voice.py` reads the authoritative C# copy, uses the established ElevenLabs eleven_v3 / fa-conversational-v1 pipeline and writes a resume-safe sixteen-clip manifest. Output is offline mono44100Hz normalized PCM, imported compressed without preload.

## Evidence and pending gate

Local payload check passed: eight lines, sixteen clips, zero external requests. Automatic approval review rejected the first generation attempt because the new Grounded Signal payload/destination needed explicit user authorization. No generation occurred in that rejected attempt and no workaround was used. The user subsequently explicitly approved this exact sixteen-clip payload and existing cast destination; the same bounded generator was accepted and completed sixteen clips. Canonical caption hashes, asset checksums, mono44100HzPCM16 encoding and nonempty duration passed for all sixteen. Actual listening remains pending.

Unity compilation, import/binding validation, actual listening and normal-input mission validation are owned by the root task and are not established by this report.
