# FirstLaunch Temporary Voice Assets

The WAV filename stem is the stable clip/line ID. Runtime dialogue data may replace a
temporary WAV with a rights-cleared recording, but must retain that ID.

## Rights status

**TEMP - INTERNAL ONLY - MICROSOFT EDGE NEURAL VOICE - DISTRIBUTION RIGHTS UNVERIFIED.**

These clips were generated through Microsoft Edge neural voices for the V2 animatic.
They are temporary offline integration assets for internal development and review.
They are not cleared for public distribution or a shipping build. Before release,
either verify and record current distribution rights or replace every clip with a
rights-cleared final recording.

The game must play imported local `AudioClip` assets only. Runtime code must not call
Microsoft Edge, Microsoft Azure, or any other network/cloud text-to-speech service.

`first_launch_temp_voice_manifest.json` records the stable IDs, source hashes, voice
mapping, and status. `Game/Narrative/Configure FirstLaunch Temporary Voice Imports`
applies the folder-specific import contract; the adjacent Validate menu item checks it.

## Environment and score assets

`Environment/` contains dedicated AI-generated FirstLaunch music, location ambience,
vehicle ambience, and restrained event textures. These clips replace the generic
gameplay explosion and battlefield assets previously used by the narrative prototype.

- Generator: ElevenLabs `eleven_text_to_sound_v2` through
  `Tools/Audio/generate_elevenlabs_sfx.py`.
- Runtime: imported local `AudioClip` assets only; no network generation occurs in-game.
- Mix intent: continuous city/command-room context under dialogue, with no rhythmic
  impact loop, close explosion, trailer boom, repeated whoosh, generated dispatch
  speech, or other human voice competing with narration.
- Evidence: `first_launch_environment_generation_manifest.json` records prompts,
  duration, loudness, clipping, silence, crest, and selected-candidate results.

Shipping rights remain subject to the commercial-use terms of the ElevenLabs account
used for generation and must be verified during release clearance.
