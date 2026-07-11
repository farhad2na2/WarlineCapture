# APH-508 Generated Animation Texture Audit

- Scope: the six tracked `AnimationTexture0..2.asset` files under generated character batches.
- Method: deterministic static serialization/GUID/clip analysis plus existing clean Android BuildReport and Unity content-inventory evidence.
- Safety: read-only audit; no texture importer, Unity asset, scene, prefab, package, or runtime code was changed.
- Status: bounded audit complete; named device-runtime residency and post-unload memory remain unmeasured.

## Executive Findings

- Project payload: `100,663,296` bytes across six RGBAHalf textures.
- Android build payload: three `CharactersBaked` textures, `50,332,044` attributed AAB bytes. The three legacy-batch textures are absent from both recorded APK and AAB BuildReports.
- Exact duplication: none; all six inline pixel payload hashes are distinct.
- Runtime ownership: each batch material binds all three matrix-row textures together, so any renderer using that material makes the three textures a single residency unit; no per-clip texture loading exists.
- Precision: generation writes three rows of bone matrices into linear, point-filtered `RGBAHalf` textures. Signed floating-point and no color conversion are structural requirements. The audit does not prove that a lower precision is visually safe.
- Unload: no first-party non-Editor explicit unload/release call was found. Direct material dependencies can be released only when their owning material/renderers and scene dependencies become unused and Unity performs an unused-asset unload or process teardown.

## Texture Evidence

| Set / texture | Dimensions | Serialized payload | Unity imported memory | APK packed | AAB packed | Flags | Payload SHA-256 | Direct references |
|---|---:|---:|---:|---:|---:|---|---|---:|
| `CharactersBaked/AnimationTexture0` | 2048 x 1024 | 16,777,216 (16.00 MiB) | 33,555,440 (32.00 MiB) | 16,777,348 (16.00 MiB) | 16,777,348 (16.00 MiB) | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `0e9272a09ede630a...` | 2 |
| `CharactersBaked/AnimationTexture1` | 2048 x 1024 | 16,777,216 (16.00 MiB) | 33,555,440 (32.00 MiB) | 16,777,348 (16.00 MiB) | 16,777,348 (16.00 MiB) | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `32cd225359ac97a8...` | 2 |
| `CharactersBaked/AnimationTexture2` | 2048 x 1024 | 16,777,216 (16.00 MiB) | 33,555,440 (32.00 MiB) | 16,777,348 (16.00 MiB) | 16,777,348 (16.00 MiB) | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `2a441a67db3af0e6...` | 2 |
| `SM_Chr_Bombsuit_Male_01_CombinedSkinned/AnimationTexture0` | 2048 x 1024 | 16,777,216 (16.00 MiB) | not included | not included | not included | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `e851ccb8958b0c38...` | 1 |
| `SM_Chr_Bombsuit_Male_01_CombinedSkinned/AnimationTexture1` | 2048 x 1024 | 16,777,216 (16.00 MiB) | not included | not included | not included | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `d8154f2d0f8dd70a...` | 1 |
| `SM_Chr_Bombsuit_Male_01_CombinedSkinned/AnimationTexture2` | 2048 x 1024 | 16,777,216 (16.00 MiB) | not included | not included | not included | format=17/RGBAHalf, mip=1, readable=1, streaming=0, colorSpace=0, filter=0/Point | `5899ffe42be1d54b...` | 1 |

The source `.asset` files are approximately 32 MiB each because inline binary payload is serialized as hexadecimal text. That source-file size is not runtime memory or package contribution. Existing Unity inventory reports approximately 32 MiB imported memory per reachable texture; the clean Android BuildReports attribute approximately 16 MiB per included texture.

## Clip Coverage

All three textures in a set cover the same texel addresses: texture 0/1/2 store the three matrix rows for every sampled bone. They do not represent separate clip banks.

| Generated set | Animator descriptors | Authored clip entries (excluding T-pose) | Unique layouts | Bone range | Highest addressed texel | Capacity | Used prefix |
|---|---:|---:|---:|---:|---:|---:|---:|
| `CharactersBaked` | 33 | 430 | 33 | 50-50 | 1,425,050 | 2,097,152 | 67.95% |
| `SM_Chr_Bombsuit_Male_01_CombinedSkinned` | 33 | 514 | 33 | 50-50 | 1,641,550 | 2,097,152 | 78.28% |

Coverage is derived from each generated animator's `start + frames * bonesCount`. It proves that recorded clip ranges fit the texture capacity; it does not prove that every authored clip is exercised by current gameplay.

## Duplication

- No two textures have the same inline pixel-payload SHA-256. The legacy set is a separate bake, not a byte-identical copy of `CharactersBaked`.
- Included set: `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture0.asset`, `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture1.asset`, `Assets/Game/Prefabs/Generated/CharactersBaked/ModelResources/AnimationTexture2.asset`.
- Project-only set: `Assets/Game/Prefabs/Generated/SM_Chr_Bombsuit_Male_01_CombinedSkinned/ModelResources/AnimationTexture0.asset`, `Assets/Game/Prefabs/Generated/SM_Chr_Bombsuit_Male_01_CombinedSkinned/ModelResources/AnimationTexture1.asset`, `Assets/Game/Prefabs/Generated/SM_Chr_Bombsuit_Male_01_CombinedSkinned/ModelResources/AnimationTexture2.asset`.
- The two sets have parallel 33-character/three-texture structures, but their animator frame ranges and pixel payloads differ. Removing the project-only set is outside this audit and requires workflow-owner confirmation because it remains referenced by its own generated prefabs/material.

## Runtime Residency And Unload Boundary

- Proven packaged: the recorded Android APK and clean AAB reports include only the three `CharactersBaked` textures, each as a 16,777,348-byte packed entry.
- Proven reachable: the Unity content inventory links those three textures to both `Menu.unity` and `Match.unity` dependency roots and measures each loaded Editor object at 33,555,440 bytes.
- Strong static inference: the `CharactersBaked/BatchMaterial.mat` binds all three textures, and generated render prefabs share that material. When that material is loaded for rendering, all three texture dependencies are eligible for residency together.
- Not proven: existing Android memory evidence does not name native texture objects, so it cannot prove exact simultaneous device residency, CPU-copy retention, or release timing.
- Explicit first-party runtime unload paths found: 0.
- `m_IsReadable=1` creates a credible CPU-copy risk, consistent with the Editor imported-memory measurement being approximately twice the 16 MiB pixel payload. Device confirmation requires a named Memory Profiler capture before and after scene transition plus `UnloadUnusedAssets`; this audit intentionally does not change readability.

## Precision Contract

- Generator source creates `TextureFormat.RGBAHalf` textures in linear mode and writes three `Color` rows per bone matrix.
- Shader/material contract binds `_SnivelerMainTextureFirst`, `Second`, and `Third`; point filtering preserves exact frame/bone texel addressing and mipmaps/streaming are disabled.
- A normalized/unsigned color format would corrupt negative transforms. Lossy block compression would interpolate/corrupt matrix values. Either is rejected without a dedicated deformation and grounding visual/geometry validation.
- Half precision is the current proven format, not a proven minimum. Any R32/RG16/quantized alternative requires generator/shader redesign plus near/far animation, foot-grounding, and transition validation on the target device.

## Decision

Do not change texture format or import settings from source size alone. The immediate evidence-backed opportunity is to measure named runtime residency and unload behavior for the included three-texture set. The legacy three-texture set is not an Android size/runtime-memory issue in the recorded builds, although it remains repository storage and maintenance debt.

## Required Follow-Up Evidence

1. Capture a Development Android Memory Profiler snapshot in Menu before character preview material use, then in Match with GPU-animated soldiers visible.
2. Capture after leaving Match, destroying all character preview/render owners, invoking the product-approved unused-asset unload boundary, and waiting two frames.
3. Record the three texture object names, native size, graphics size, ref owners, and readable CPU-copy state in all snapshots.
4. Only then evaluate `Apply(updateMipmaps: false, makeNoLongerReadable: true)` in the generator as a separately validated bake change; do not mutate generated assets manually.

## Evidence Sources

- `Design/AgentReports/architecture_performance_android_apk_build_report.json`
- `Design/AgentReports/architecture_performance_android_aab_build_report.json`
- `Design/AgentReports/architecture_performance_content_residency_baseline.json`
- `Packages/com.sniveler-code.gpu-animation/Editor/Scripts/GenerateProcessor.cs`
- Generated materials, animator prefabs, texture assets, and GUID references under `Assets/Game/Prefabs/Generated`.
