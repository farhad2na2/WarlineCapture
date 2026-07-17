# FirstLaunch Voice Assets

The WAV filename stem is the stable clip/line ID. Runtime dialogue data may replace a
recording, but must retain that ID so prefab and configuration references remain stable.

## Rights status

**ELEVENLABS PAID CREATOR - COMMERCIAL LICENSE - LOCAL RUNTIME ASSETS.**

The current 17 clips were generated while the project's ElevenLabs Creator subscription
was active. `first_launch_temp_voice_manifest.json` records the subscription tier at
generation, model, voice ID, request evidence, processing, duration, and output hash.
The legacy filename is retained so existing project tooling does not lose the manifest.

The game plays imported local `AudioClip` assets only. Runtime code does not call
ElevenLabs or another network text-to-speech service.

`Game/Narrative/Configure FirstLaunch Voice Imports` applies the folder-specific import
contract; the adjacent Validate menu item checks it. Clips remain mono Vorbis,
Compressed In Memory, background-loaded, and preloaded so replacing the source WAVs
does not change the established runtime loading policy.

## Permanent cast and regeneration

`Assets/Game/Data/Narrative/FirstLaunch/first_launch_elevenlabs_voice_map.json` is the
permanent cast map. Future chapters and command lines must reuse these speaker voice IDs;
ARIA's ID is also the canonical voice for later match-command generation. Do not create
a fresh Voice Design voice for an existing character during routine regeneration.

`Tools/NarrativeVision/generate_first_launch_voice_elevenlabs.py` reads the English
catalog, reuses the cast map, generates all 17 stable clip IDs, converts them to 44.1 kHz
mono PCM WAV, validates each clip against its dialogue window, and refreshes the manifest.
The API key is read from a local secret file outside the repository.

The District Dispatch lines use narrow-band command-radio processing. Dalia uses
scene-specific urgent performance direction with expressive field-comms processing;
Samira uses lighter field-comms processing. ARIA and the Commander remain clean.
The presentation prefab also layers the dedicated
`first_launch_radio_emergency_event_01.wav` carrier and squelch cue over the
dispatch-radio state.

```powershell
<python.exe> Tools/NarrativeVision/generate_first_launch_voice_elevenlabs.py `
  --api-key-file "$env:LOCALAPPDATA\WarlineCapture\Secrets\elevenlabs_api_key.txt" `
  --ffmpeg <ffmpeg.exe>
```

The old offline SAPI and Edge TTS scripts remain historical development fallbacks only.
They are not the source of the current shipping-intent clips and must not overwrite the
ElevenLabs batch.

## Persian localized batch

The approved Persian source is
`Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json`.
It uses locale `fa-IR` and ElevenLabs TTS language code `fa`. Persian generation must use
`eleven_v3`; the existing multilingual v2 workflow does not support Persian.

Sending the catalog to ElevenLabs exports the approved dialogue to an external service
and consumes project credits, so generation requires explicit project-owner approval.
After approval, generate the 17 stable line IDs and the Commander variants with:

```powershell
<python.exe> Tools/NarrativeVision/generate_first_launch_voice_elevenlabs.py `
  --catalog Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json `
  --voice-root Assets/Game/Audio/Narrative/FirstLaunch/Voice/fa `
  --manifest Assets/Game/Audio/Narrative/FirstLaunch/first_launch_persian_voice_manifest.json `
  --language-code fa `
  --ffmpeg <ffmpeg.exe> `
  --candidate-count 3

<python.exe> Tools/NarrativeVision/generate_first_launch_voice_elevenlabs.py `
  --catalog Assets/Game/Data/Narrative/FirstLaunch/first_launch_persian_text_catalog.json `
  --voice-root Assets/Game/Audio/Narrative/FirstLaunch/Voice/fa `
  --manifest Assets/Game/Audio/Narrative/FirstLaunch/first_launch_persian_voice_manifest.json `
  --language-code fa `
  --ffmpeg <ffmpeg.exe> `
  --candidate-count 3 `
  --commander-variants
```

Then run `Game/Narrative/First Launch/Install Into Menu Scene` to import the WAV files
and rebuild `FirstLaunchPersianLocale.asset` with local `AudioClip` references. Until
that batch exists, Persian mode intentionally plays no voice instead of mismatched
English speech.

## Environment and score assets

`Environment/` contains dedicated AI-generated FirstLaunch music, location ambience,
vehicle ambience, and restrained event textures. These clips replace the generic
gameplay explosion and battlefield assets previously used by the narrative prototype.

- Generator: ElevenLabs `eleven_text_to_sound_v2` through
  `Tools/Audio/generate_elevenlabs_sfx.py`.
- Rights: generated while the project Creator subscription was active; the manifest
  records the paid tier and commercial-use status.
- Runtime: imported local `AudioClip` assets only; no network generation occurs in-game.
- Import policy: 18-24 second music and ambience loops use streaming Vorbis with preload
  disabled; 6-7 second event cues are resident and preloaded for immediate playback.
- Mix intent: continuous city/command-room context under dialogue, with no rhythmic
  impact loop, close explosion, trailer boom, repeated whoosh, generated dispatch
  speech, or other human voice competing with narration.
- Evidence: `first_launch_environment_generation_manifest.json` records prompts,
  subscription snapshot, duration, loudness, clipping, silence, crest, and the selected
  candidate for all eight mapped assets.
