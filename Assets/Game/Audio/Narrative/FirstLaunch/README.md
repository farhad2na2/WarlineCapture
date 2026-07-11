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
