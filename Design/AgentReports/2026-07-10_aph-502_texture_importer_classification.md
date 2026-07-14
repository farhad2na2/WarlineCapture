# APH-502 Texture Importer Classification Inventory

- Task: `APH-502`
- Status: `incomplete`
- Final buckets accepted: `false`
- Analyzed revision: `cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- Tracked worktree clean: `false`
- Scope: Git-tracked `.meta` files whose current YAML contains `TextureImporter:`
- Import settings changed: none
- Unity run: none

## Current Result

The current tracked input set contains **3,536** texture importers. All category, candidate, overlap, ambiguity, and evidence counts below are generated from the current tracked importer metadata and evidence inputs; no inventory total is hard-coded.

The semantic classification is current, but the final inclusion/exclusion buckets remain unaccepted. `excluded/unreferenced` means only that no clean same-revision complete evidence pair was accepted; it does not prove that an asset is unused, unreachable, or safe to remove.

### Mutually Exclusive Chosen Semantic Categories

| Chosen category | Count |
|---|---:|
| UI | 2,952 |
| World albedo | 401 |
| World normal/mask | 37 |
| VFX | 33 |
| Impostor/atlas | 39 |
| Generated source/reference | 74 |
| **Total** | **3,536** |

### Overlapping Semantic Candidates

| Candidate | Membership count |
|---|---:|
| UI | 3,048 |
| World albedo | 401 |
| World normal/mask | 46 |
| VFX | 33 |
| Impostor/atlas | 39 |
| Generated source/reference | 74 |

Ambiguous importers: **105**. Unclassified importers: **0**.

| Exact candidate overlap | Count |
|---|---:|
| UI + VFX | 22 |
| UI + generated source/reference | 74 |
| UI + world normal/mask | 7 |
| impostor/atlas + world normal/mask | 2 |

### Current Inclusion Evidence Status

| Evidence status | Count |
|---|---:|
| accepted current inclusion | 0 |
| excluded/unreferenced | 3,536 |

## Evidence Gate

Acceptance requires both a clean same-revision complete Unity content-residency inventory and at least one clean same-revision detailed Android BuildReport with a deterministic complete `buildReportIncludedTextures` export. Until both exist, the analyzer accepts zero inclusion paths and zero exclusion claims.

### Content Residency

- Path: `Design/AgentReports/architecture_performance_content_residency_baseline.json`
- Evidence revision: `7084805d771142706f340e9f2e52a68570bcb72b`
- Texture rows: `637`
- Summary texture rows: `639`
- Complete inventory: `false`
- Accepted for current revision: `false`
- Disposition: historical/incomplete/rejected only
- Validation errors: `revision-mismatch:7084805d771142706f340e9f2e52a68570bcb72b->cbf6fd48846b40dd086faa0feb364fce0462a1bf, summary-texture-asset-count-mismatch:639!=637, tracked-worktree-dirty`

### Android BuildReports

| Path | Revision | Complete texture rows | Complete export | Accepted |
|---|---|---:|---|---|
| `Design/AgentReports/architecture_performance_android_aab_build_report.json` | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | 0 | `false` | `false` |
| `Design/AgentReports/architecture_performance_android_apk_build_report.json` | `5a49ab8f010674ca8b364af1245fe2902401b305` | 0 | `false` | `false` |

### Remaining Blockers

- `android-build-report:architecture_performance_android_aab_build_report.json:complete-texture-export-marker-not-true`
- `android-build-report:architecture_performance_android_aab_build_report.json:complete-texture-export-not-array`
- `android-build-report:architecture_performance_android_aab_build_report.json:revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `android-build-report:architecture_performance_android_aab_build_report.json:tracked-worktree-dirty`
- `android-build-report:architecture_performance_android_apk_build_report.json:complete-texture-export-marker-not-true`
- `android-build-report:architecture_performance_android_apk_build_report.json:complete-texture-export-not-array`
- `android-build-report:architecture_performance_android_apk_build_report.json:revision-mismatch:5a49ab8f010674ca8b364af1245fe2902401b305->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `android-build-report:architecture_performance_android_apk_build_report.json:tracked-worktree-dirty`
- `android-build-report:no-current-complete-texture-export`
- `content-residency:revision-mismatch:7084805d771142706f340e9f2e52a68570bcb72b->cbf6fd48846b40dd086faa0feb364fce0462a1bf`
- `content-residency:summary-texture-asset-count-mismatch:639!=637`
- `content-residency:tracked-worktree-dirty`
- `tracked-worktree-dirty:59-tracked-change-records`

## Semantic Rules

Candidate rules are case-insensitive and are applied to every importer before precedence:

- Generated source/reference: `/Generated/` plus a reference/source segment or filename token.
- Impostor/atlas: `impostor` path text, an `atlas` filename token, or `/Atlases/`.
- VFX: effects/FX/VFX paths or VFX, particle, muzzle-flash, smoke, or glow tokens.
- World normal/mask: `textureType: 1` or normal/mask/material-channel filename tokens.
- UI: sprite import, `textureType: 8`, or UI/GUI/Interface/Fonts paths.
- World albedo: fallback only when no other candidate applies.

Chosen precedence is generated source/reference, impostor/atlas, VFX, UI, world normal/mask, then world albedo. Exact ambiguous paths remain available in the generated JSON report.

## Reproduction

```sh
PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 -m unittest \
  Tools.CI.tests.test_aph502_texture_importer_classification -v
PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 \
  Tools/CI/aph502_texture_importer_classification.py --write
PYTHONPYCACHEPREFIX=/tmp/aph502-pyc python3 \
  Tools/CI/aph502_texture_importer_classification.py --check
```

No importer or asset mutation is authorized by this report.
