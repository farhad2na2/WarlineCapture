# APH-507 Android Texture Override Audit

- Task: `APH-507`
- Status: `report-only audit complete; optimization not accepted`
- Audit date: `2026-07-11`
- Importer, texture, code, tracker, Unity asset, Jenkins, and CI changes: none
- Unity run: none

## Decision

The tracked evidence identifies **13 included 4096 x 4096 textures** and no observed 8192 x 8192 texture. All 13 are from the Polygon Military texture set. They account for `141,909,908` packed AAB bytes (135.34 MiB) in the clean historical BuildReport, so they are material optimization candidates.

No Android override should be changed from this audit alone. The evidence revisions do not match each other or the current revision, the BuildReports export only their largest 100 included assets, and none of the tracked evidence exports the explicit Android override flag or configured Android max-size value. The effective imported Android formats and dimensions are available from the historical content-residency evidence.

## Evidence And Limits

| Evidence | Revision | Use in this audit | Limitation |
|---|---|---|---|
| `architecture_performance_android_aab_build_report.json` | `a527e151e9e43a491ba30f4c19a0320dc54faf5c` | Clean positive inclusion and packed bytes | Largest 100 assets only; not a complete texture inventory |
| `architecture_performance_android_apk_build_report.json` | `4c05a2da10bf5117ca592cf8daac05459ab3b74c` | Corroborates the same 13 paths and packed bytes | Dirty build and different revision; corroboration only |
| `architecture_performance_content_residency_baseline.json` | `7084805d771142706f340e9f2e52a68570bcb72b` | Dimensions, effective Android format, imported bytes, mipmap state, and streaming state | Historical revision; 637 detailed texture rows versus the Markdown summary of 639 |
| `2026-07-10_aph-502_texture_importer_classification.md` | `bc0287616ac225de524d836cd8409c4fd0d49eb0` | Current semantic categories | Explicitly rejects historical inclusion as current evidence; does not export Android override/max-size fields |

`Included` below means positively present in the clean historical AAB BuildReport, not proven included at current `HEAD`. `Override` is `not evidenced` because the tracked reports do not record whether the Android platform override checkbox is enabled or which max-size value is configured.

## Included 4K/8K Candidates

| Texture | Size | Category | Android override | Effective Android format | Packed AAB | Visual risk | Reason |
|---|---:|---|---|---|---:|---|---|
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC4X4_SRGB` | 21.33 MiB | Very high | The denser ASTC 4x4 format is an explicit quality signal; reducing resolution or compression quality could affect many shared military surfaces. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_A_Normals.png` | 4096 x 4096 | World normal/mask | Not evidenced | `RGBA_ASTC6X6_UNorm` | 9.50 MiB | High | Normal-map loss can create visible lighting, silhouette-detail, and compression artifacts across shared units or structures. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_B.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Shared atlas usage is not resolved by the evidence; a global override may affect many close-camera assets. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_01_C.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Shared atlas usage is not resolved by the evidence; a global override may affect many close-camera assets. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_A.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_B.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_02_C.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_A.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_B.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_03_C.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_A.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_B.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |
| `Assets/PolygonMilitary/Textures/PolygonMilitary_Texture_04_C.png` | 4096 x 4096 | World albedo | Not evidenced | `RGBA_ASTC6X6_SRGB` | 9.50 MiB | High | Material-slot coverage and minimum camera distance are not evidenced. |

All 13 had mipmaps enabled and mip streaming disabled in the historical residency evidence. Their combined imported runtime size was `283,829,813` bytes (270.68 MiB). The clean AAB BuildReport and dirty APK BuildReport report the same packed byte count for each listed path.

## 8K Result

No 8192 x 8192 texture appears in the 637 detailed historical texture-residency rows. This is **not proof that the current Android build contains no 8K texture** because:

1. The residency report is historical.
2. Its detailed rows are two textures short of its 639-texture summary.
3. The BuildReports expose only the largest 100 included assets and do not export texture dimensions.

## Blind Spots

- Current-revision inclusion is unproven for every candidate.
- Explicit Android override enabled/disabled state, max-size limit, compressor quality, and crunch settings are not present in the tracked evidence.
- The reports do not map each atlas to material slots, prefabs, minimum camera distance, or visible screen coverage.
- No matched before/after Android screenshots exist for a 4096-to-2048 pilot.
- No device memory, loading-time, frame-time, or texture-pop comparison exists for an override change.
- The top-100 BuildReport cannot prove that smaller 4K/8K textures are absent.
- Historical effective format proves what Unity imported for that build, not why that format was selected.

## Bounded Next Pilot

Do not bulk-edit this family. Run one reversible pilot on **one ASTC 6x6 world-albedo texture only**, selected after a dependency report proves its material/prefab consumers and the closest supported camera view. Exclude `PolygonMilitary_Texture_01_A.png` and the normal map from the first pilot.

1. Regenerate a clean, current-revision Android BuildReport that exports all included textures with dimensions and Android platform settings.
2. Resolve material and prefab consumers for the 12 ASTC 6x6 albedo candidates; choose the candidate with the narrowest runtime usage and least close-camera screen coverage.
3. Capture fixed near, medium, far, and representative combat views on the target Android device.
4. Change only that candidate's Android max size from 4096 to 2048 while retaining ASTC 6x6 and all unrelated importer settings.
5. Rebuild and repeat the exact captures. Reject on visible atlas blur, color bleeding, mip pop, unit/building detail loss, or UI contamination.
6. Record packed-size, runtime texture memory, match-load time, and steady-state frame-time deltas. Accept only if visual comparison passes and the measured reduction is material.
7. Restore the importer if rejected. Do not expand to the remaining candidates until the first pilot has accepted visual and device evidence.

## Acceptance State

This report completes only the read-only APH-507 candidate audit. APH-507 remains incomplete because no importer override has been changed or accepted through current-revision BuildReport and Android visual validation.
