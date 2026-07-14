# APH-504 Texture Streaming Pilot Candidate Plan

- Evidence date: `2026-07-14`
- Status: `candidate-plan-valid-rollout-blocked`
- Analyzed revision: `d5b2ddeb8166010bbe5337e00243e74015ee4e94`
- Selector valid: `true`
- Pilot ready for importer mutation: `false`
- Importer mutation authorized: `false`
- Pilot expansion authorized: `false`
- Unity and Android runs: `none`

## Decision

The read-only selector proposes two world-albedo textures as a bounded future pilot. It does not authorize either importer change or a wider streaming rollout.

## Proposed Candidate Set

| Texture | Decision | Category | Historical AAB bytes | Reasons |
|---|---|---|---:|---|
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` | proposed | world albedo | 22369788 | pilot-rank:1, texture-family-representative:01, world-albedo, clean-historical-aab-positive-inclusion, asset-and-meta-unchanged-since-aph502-and-aab, mipmaps-enabled, explicit-streaming-baseline-disabled |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` | excluded | world normal/mask | 9961684 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` | excluded | world albedo | 9961676 | texture-family-quota-filled:01 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` | excluded | world albedo | 9961676 | texture-family-quota-filled:01 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` | proposed | world albedo | 9961676 | pilot-rank:2, texture-family-representative:02, world-albedo, clean-historical-aab-positive-inclusion, asset-and-meta-unchanged-since-aph502-and-aab, mipmaps-enabled, explicit-streaming-baseline-disabled |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` | excluded | world albedo | 9961676 | pilot-cap-reached:2 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` | excluded | world albedo | 9961676 | pilot-cap-reached:2 |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` | excluded | world albedo | 9961676 | ignoreMipmapLimit-field-absent, streamingMipmaps-field-absent |

## Evidence Disposition

- Historical AAB revision: `a527e151e9e43a491ba30f4c19a0320dc54faf5c`; dirty=`false`; exported assets=`100/6104`.
- Historical positive rows prove prior inclusion only; they do not prove current-revision inclusion or absence.
- Scoped tracked inputs clean: `true`.
- Control-input hashes unchanged during collection: `true`.

## Mobile Configuration

- Streaming active: `1`
- Add all cameras: `1`
- Streaming memory budget: `256 MiB`
- Global texture mip limit: `1`
- Maximum streaming level reduction: `2`
- Maximum file I/O requests: `1024`

The 256 MiB value is an observed bounded configuration, not an accepted product budget. The global mip limit of 1 prevents full source mip preservation for nearby views while the proposed importers keep `ignoreMipmapLimit: 0`.

## Unresolved Evidence

- `aph502-final-buckets-unaccepted`
- `aph505-near-medium-far-before-after-visual-evidence-absent`
- `aph506-ten-minute-memory-io-evidence-absent`
- `candidate-material-renderer-camera-coverage-unresolved`
- `current-revision-clean-complete-texture-build-report-absent`
- `current-revision-clean-residency-inventory-absent`
- `full-source-near-mips-not-preserved:globalTextureMipmapLimit=1`
- `historical-aab-export-incomplete:100/6104`
- `historical-aab-revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->d5b2ddeb8166010bbe5337e00243e74015ee4e94`
- `normal-mask-representative-not-selected:explicit-streaming-fields-absent`
- `pilot-importer-settings-not-applied`
- `selected-readable-texture-cpu-copy-memory-unmeasured`

## Acceptance Boundary

The selector contract is accepted when its inputs parse deterministically, scoped inputs stay clean, the exact two candidates are proposed, and both mutation flags remain false. APH-504 itself remains incomplete until current-revision build/residency evidence, APH-505 visual captures, and APH-506 ten-minute memory/I/O measurements pass.

## Reproduction

```sh
PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 -m unittest \
  Tools.CI.tests.test_aph504_texture_streaming_pilot_selector -v
PYTHONPYCACHEPREFIX=/tmp/aph504-pyc python3 \
  Tools/CI/aph504_texture_streaming_pilot_selector.py --check
```
