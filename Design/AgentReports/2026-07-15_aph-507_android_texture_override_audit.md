# APH-507 Android Texture Override Audit

- Task: `APH-507`
- Audit status: `complete`
- Decision: `BLOCK_ALL_LIMIT_REDUCTIONS`
- Limit reduction authorized: `false`
- Analyzed revision: `ebdba0b4bb4c59963ee07ae84a96b2071eab9f8f`
- Tracked worktree clean: `true`
- Importers/assets changed: none
- Unity run: none

## Decision

No Android texture limit may be changed based on this report unless its candidate row says `limitReductionAuthorized=true`. The current audit blocks all limit reductions because the required current BuildReport and visual-proof gates are not both accepted.

A candidate is authorized only when its current importer is valid, a clean same-revision complete Android BuildReport proves inclusion, and same-revision hash-verified Android near/medium/far/combat before-and-after visual proof preserves ASTC format and clears all quality rejection checks.

## Current Static Audit

The deterministic scan found **60** tracked source textures whose current effective Android max-size setting and source dimensions are at least 4096. That includes **55** 4K-limit and **5** 8K-limit candidates. Explicit Android overrides are enabled on **0** of those candidates.

Historical top-100 BuildReports positively include **13** candidates and attribute **135.34 MiB** in the AAB report. That evidence is context only; it is not current authorization.

| Asset | Source | Android limit/source | ASTC evidence | Quality | Est. payload | Est. 2K saving | Current build | Visual | Authorized |
|---|---|---|---|---|---:|---:|---|---|---|
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_01_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_01_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_01_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_02_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_02_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_02_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_03_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_03_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_03_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_04_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_04_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/Alts/PolygonMilitary_04_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_01_A_Normals.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC4X4_SRGB; historical content residency | very-high; high-quality compressed/normal | 21.33 MiB | 16.00 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_UNorm; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_WhiteTest.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | RGBA_ASTC6X6_SRGB; historical content residency | balanced; compressed/normal | 9.50 MiB | 7.12 MiB | `false` | `false` | `false` |
| `Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Sprites/SPR_HUD_MilitaryCombat_ExampleScreenshot_3rdPerson.png` | 5040x2160 PNG | 8192 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Sprites/SPR_HUD_MilitaryCombat_ExampleScreenshot_Downed.png` | 5040x2160 PNG | 8192 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Sprites/SPR_HUD_MilitaryCombat_ExampleScreenshot_FirstPerson.png` | 5040x2160 PNG | 8192 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Sprites/SPR_HUD_MilitaryCombat_ExampleScreenshot_LooterShooter.png` | 5040x2160 PNG | 8192 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/InterfaceMilitaryCombatHUD/Samples/Sprites/SPR_HUD_MilitaryCombat_ExampleScreenshot_Survival.png` | 5040x2160 PNG | 8192 (DefaultTexturePlatform) | unknown; not evidenced | unknown; compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_01_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_01_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_01_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_02_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_02_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_02_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_03_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_03_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_03_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_04_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_04_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Alts/Generic_04_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_01_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_01_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_01_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_02_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_02_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_02_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_03_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_03_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_03_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_04_A.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_04_B.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Emissive/Generic_Emissive_04_C.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Generic_Ivy_Normals.tga` | 4096x4096 TGA | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/Generic_Normals_01.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/HairMask.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |
| `Assets/Synty/PolygonGeneric/Textures/SkinMask.png` | 4096x4096 PNG | 4096 (DefaultTexturePlatform) | unknown; not evidenced | unknown; high-quality compressed/normal | n/a | n/a | `false` | `false` | `false` |

ASTC payload estimates are deterministic 16-byte block calculations over the source dimensions clamped to the importer limit, including the full mip chain when enabled. They exclude container, alignment, and BuildReport overhead and are not substituted for measured build bytes.

## Evidence Gates

### Android BuildReports

| Path | Package | Revision | Complete texture rows | Accepted |
|---|---|---|---:|---|
| `Design/AgentReports/architecture_performance_android_aab_build_report.json` | `AAB` | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | 0 | `false` |
| Blockers |  |  |  | `complete-texture-export-marker-not-true, complete-texture-export-not-array, revision-mismatch:a527e151e9e43a491ba30f4c19a0320dc54faf5c->ebdba0b4bb4c59963ee07ae84a96b2071eab9f8f` |
| `Design/AgentReports/architecture_performance_android_apk_build_report.json` | `APK` | `5a49ab8f010674ca8b364af1245fe2902401b305` | 0 | `false` |
| Blockers |  |  |  | `complete-texture-export-marker-not-true, complete-texture-export-not-array, revision-mismatch:5a49ab8f010674ca8b364af1245fe2902401b305->ebdba0b4bb4c59963ee07ae84a96b2071eab9f8f` |

### Android Visual Proof

- Path: `Design/AgentReports/architecture_performance_android_texture_override_visual_evidence.json`
- Evidence revision: `None`
- Device / graphics API: `None` / `None`
- Candidate rows: `0`
- Accepted: `false`
- Validation errors: `file-missing`

The visual contract requires hash-verified PNG pairs at identical recorded camera state for near, medium, far, and combat views. It rejects atlas blur, color bleeding, mip pop, detail loss, or UI contamination and requires the same ASTC format before and after the limit change.

## Fail-Closed Blockers

- `no-current-complete-Android-BuildReport`
- `no-current-hash-verified-Android-visual-proof`

Importers with an oversized configured limit but unreadable static source dimensions: `0`. These are retained as a blind spot and cannot be treated as safe exclusions.

## Reproduction

```sh
PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 -m unittest \
  Tools.CI.tests.test_aph507_android_texture_override_audit -v
PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 \
  Tools/CI/aph507_android_texture_override_audit.py --write
PYTHONPYCACHEPREFIX=/tmp/aph507-pyc python3 \
  Tools/CI/aph507_android_texture_override_audit.py --check
```
