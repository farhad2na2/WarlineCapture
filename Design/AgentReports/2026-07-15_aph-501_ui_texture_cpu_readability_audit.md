# APH-501 UI Texture CPU Readability Audit

- Status: source audit complete; importer mutation deferred until same-artifact device evidence proves it is needed.
- Scope: the 203 sprites governed by `Aph501AndroidMainMenuTexturePolicy` and `Aph501AndroidSupportingUiTexturePolicy`.
- Exclusions: First Launch, operation-map content, world textures, and generated/reference sources outside the two exact policy roots.

## Result

Seventy-two scoped textures remain CPU readable: 1 BrightCommand sprite, 44 Match HUD sprites, and all 27 V15C sprites. Forty-two of those paths are included in the clean `2327b2bbf5bf1bb03a1f6fa349b11eb0b90d357e` APK BuildReport.

The modeled mipless ASTC CPU-copy upper bound is `12,510,528` bytes across all 72 readable sources and `6,854,624` bytes across the 42 build-included sources. Two included Match HUD portraits are SpriteAtlas packables whose atlas is already non-readable; excluding those source copies gives a likely current-build upper bound of `6,617,952` bytes (`6.31 MiB`). These are modeled upper bounds, not measured simultaneous residency.

## Code Evidence

- No production runtime or required editor workflow reads pixels from these governed assets through `GetPixel*`, `GetRawTextureData`, `ReadPixels`, or `EncodeToPNG`.
- The Match HUD minimap reads a runtime-created readback texture, not a governed source asset.
- Screenshot validators create temporary capture textures before reading pixels.
- `ContentResidencyInventoryGenerator` restricts raw-data fallback to generated animation textures.
- Governed UI assets are consumed as sprites, `RawImage` textures, or dimension metadata.

## Decision

Do not invalidate the package-compliant APK before its APH-804 device run. If measured release memory still exceeds the accepted ceiling, add an explicit `isReadable=false` contract to both scoped policies, update their focused tests, validate atlas/prefab-generation workflows, rebuild the exact release artifact, and measure named Android texture residency before accepting the saving.

Source-level evidence supports that bounded follow-up; device residency and unload evidence remain required.
