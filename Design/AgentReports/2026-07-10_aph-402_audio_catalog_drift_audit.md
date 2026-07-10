# APH-402 Audio Catalog Drift Audit

## Result

- Project WAV importers: `262`
- Music/Ambience WAV assets: `15`
- Music/Ambience clips referenced by source JSON: `9`
- Music/Ambience clips referenced by serialized runtime catalog: `9`
- Source JSON and serialized catalog subsets agree after GUID resolution: `yes`
- Catalog-referenced Music/Ambience importer mismatches: `0`
- Unused legacy Music/Ambience clips: `6`

APH-403 requires no importer correction on the current catalog. Changing the six unused legacy files would not reduce runtime catalog residency and is intentionally excluded.

## Cataloged Streaming Set

All nine clips use `Streaming`, `preloadAudioData=false`, `loadInBackground=true`, stereo-preserving import, Vorbis compression, and the existing sample-rate policy.

- `Assets/Game/Audio/Music/music_splash_intro_01.wav`
- `Assets/Game/Audio/Music/music_menu_loop_01.wav`
- `Assets/Game/Audio/Music/music_briefing_loop_01.wav`
- `Assets/Game/Audio/Music/music_match_calm_loop_01.wav`
- `Assets/Game/Audio/Music/music_match_combat_loop_01.wav`
- `Assets/Game/Audio/Music/music_result_victory_01.wav`
- `Assets/Game/Audio/Music/music_result_defeat_01.wav`
- `Assets/Game/Audio/Ambience/amb_city_day_loop_01.wav`
- `Assets/Game/Audio/Ambience/amb_base_distant_loop_01.wav`

Required importer state:

| Field | Value |
|---|---|
| `defaultSettings.loadType` | `2` (`Streaming`) |
| nested and top-level `preloadAudioData` | `0` |
| `forceToMono` | `0` |
| `loadInBackground` | `1` |
| `compressionFormat` | `1` (`Vorbis`) |
| sample-rate setting / override | `0` / `44100` |

## Unused Legacy Inventory

These files are absent from both catalog sources and have no serialized GUID reference under `Assets`:

- `Assets/Game/Audio/Ambience/amb_battlefield_loop_01.wav`
- `Assets/Game/Audio/Ambience/amb_city_strategic_loop_01.wav`
- `Assets/Game/Audio/Music/music_battle_intensity_01_loop.wav`
- `Assets/Game/Audio/Music/music_battle_intensity_02_loop.wav`
- `Assets/Game/Audio/Music/music_stinger_defeat_01.wav`
- `Assets/Game/Audio/Music/music_stinger_victory_01.wav`

Their legacy `DecompressOnLoad`/preloaded state does not affect current catalog residency. They remain cleanup/build-size inventory rather than APH-403 runtime drift.

## Reproduction

- Catalog JSON: `Assets/Game/Audio/Config/audio_event_catalog_v0_1.json`
- Serialized catalog: `Assets/Game/Audio/Events/AudioEventCatalogConfig.asset`
- Policy: `Assets/Game/Audio/Config/audio_import_profiles_v0_1.json`
- Import workflow: `Tools/Audio/apply_audio_import_profiles.py`
- Contract gate: `AudioConfigContractTests.RunFocusedValidation`
