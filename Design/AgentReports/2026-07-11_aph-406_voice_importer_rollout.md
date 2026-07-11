# APH-406 Voice Importer Policy Rollout

## Result

The Android-accepted eight-clip Voice pilot policy is now the category-level `Voice` import policy. All 163 cataloged Voice clips resolve through the existing JSON-driven importer workflow with no explicit per-clip overrides.

## Policy

| Setting | Accepted Voice value |
|---|---|
| Load type | `CompressedInMemory` |
| Compression | `Vorbis` |
| Force mono | `true` |
| Preload audio data | `false` |
| Load in background | `true` |
| Sample rate | `44100` |

The temporary `VoicePilot` importer profile and its eight overrides were removed. This avoids two import policies for the same category and ensures newly cataloged Voice clips inherit the accepted policy automatically. The original eight paths remain frozen separately as the evidence-only `validationSets.APH405VoicePilot` set; this set does not affect importer resolution.

## Applied Scope

- Catalog clips processed by `Tools/Audio/apply_audio_import_profiles.py`: 234
- Cataloged Voice clips: 163
- Voice importers already compliant from the pilot: 8
- Voice importer `.meta` files changed by this rollout: 155
- Explicit profile overrides after rollout: 0
- Frozen APH-405 validation paths: 8
- Non-Voice importer files changed: 0

The 155 changed importer files are the tracked `.wav.meta` files under `Assets/Game/Audio/Voice/ARIA` that were not in the accepted eight-clip pilot. The original eight pilot importers were already byte-equivalent to the promoted category policy.

## Deterministic Evidence

| Evidence | Value |
|---|---|
| Profile JSON SHA-256 | `a607835dc43a2a8f528e1039005777d73fe6bd215628c1a4c70747ff7773f02e` |
| Aggregate sorted Voice importer SHA-256 | `ed0d17a7b581aadb0aae57924933db5d014676918d41846363c528f620ccbbde` |
| First/second application diff SHA-256 | `ba0bb39e1f71186221b1b5a7ada5e2447101ccd4bdb3071e984824f4012f1205` / identical |

## Non-Unity Validation

- JSON parse: passed.
- Focused Python rollout contracts: 3/3 passed.
- Python bytecode compilation with a temporary cache: passed.
- `Game.Tests.Editor.csproj --no-restore --no-dependencies`: passed with zero errors and three existing warnings.
- Import workflow second application: passed and produced an identical diff hash.
- Static importer scan: all 163 Voice clips have `loadType=1`, both preload fields set to `0`, `loadInBackground=1`, `forceToMono=1`, and `sampleRateOverride=44100`.

Unity was intentionally not started because the coordinator owns the exclusive Unity lease. The Unity `AudioConfigContractTests.CatalogAudioImportSettingsMatchProfiles` test remains the required live importer validation.

## Adjacent Historical Evidence

`Tools/CI/aph407_audio_catalog_split_analysis.py` intentionally encodes the pre-rollout eight-pilot/155-legacy state used by its committed historical report. It is outside this slice's ownership and must not be regenerated against the post-rollout working tree without a coordinator-owned evidence-version update.

## Pilot Probe Retirement

The APH-405 runtime probe was a one-shot development-build measurement tool that discovered clips through the bounded pilot importer state. A category-wide rollout would make its exact-eight discovery assertion invalid, so the coordinator retired the runtime probe, its view integration, and its focused implementation tests after committing the Android acceptance evidence. The exact eight measured paths remain frozen in `validationSets.APH405VoicePilot` and in the APH-405 evidence report.

This does not weaken runtime audio contracts or restore broad preload. The production presentation path is unchanged apart from removal of the completed opt-in measurement hook.
