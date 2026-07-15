# Static Map Presentation Ownership Decision

Date: 2026-07-15
Status: Accepted shared-foundation classification
Baseline: `ad386af0f79f7c8d27002d4a258059ada54afb5d`
Tracker: `../Architecture/operation_map_scene_split_and_generator_tracker.md`

## Decision

Static-presentation content belongs to the active operation map. The reusable indexing, streaming, renderer-suppression transaction, and teardown mechanisms belong to the shell/presentation boundary. Existing code that discovers the one canonical Match map through hardcoded paths or a direct `MatchSceneView` reference is temporary compatibility and must remain until the extracted current map passes parity and the atomic cutover is ready.

No loader or generator choice is made by this decision.

## Ownership Inventory

| Stable authority | Classification | Evidence | Migration disposition |
|---|---|---|---|
| `Assets/Game/GeneratedStaticMapPresentation/StaticMapPresentationManifest.asset` | `MapOwned` | Manifest identifies one canonical source scene and its chunk/source set. | Move under the extracted current map's stable identity without changing bytes/GUIDs before parity. |
| `StaticMapPresentationSceneIntegrity.json` and generated `Scenes/` | `MapOwned` | Integrity ledger and chunk scenes are derived from that manifest/content hash. | Keep as one atomic map product set; never share or discover foreign chunks. |
| `Game.Rendering.StaticMapPresentationManifest` type | `SharedConfig` | Reusable schema; each asset instance describes one map. | Keep schema in `Game.Rendering`; map registration owns the selected instance. |
| `MatchSceneView.staticMapPresentationManifest` | `TemporaryCompatibility` | Current shell/map scene directly serializes the one manifest. | Preserve until map registration supplies the active map manifest; remove only at atomic cutover. |
| `Game.Composition.StaticMapPresentationManifestIndex` | `ShellOwned` | Pure reusable validation/index/projection logic over a supplied manifest and camera. | Retain; do not add map discovery or asset loading. |
| `Game.Composition.StaticMapPresentationStreamer` | `ShellOwned` | Reusable bounded scene-stream state machine; `Bind`, `BeginDrain`, `Update`, and `Unbind` operate on supplied data. | Retain as the presentation boundary; later loader integration must bind explicitly. |
| `MenuBootstrapCompositionSystemHelper.staticMapPresentationStreamer` lifecycle | `ShellOwned` | Shell gates match start on preload, begins drain before unload, updates progress, and unbinds on shutdown. | Retain one-owner lifecycle and one-active-map semantics. |
| `Game.Rendering.StaticMapPresentationOwnership` transaction | `ShellOwned` | `Initialize` validates/suppresses map renderers; `Dispose` restores exact prior enabled states and disposes fallback batching. | Retain as narrow managed presentation logic; bind explicit map roots and manifest. |
| Canonical `MeshRenderer` sources below the current `Map` root | `MapOwned` | Suppression resolves manifest source identities against the active map hierarchy. | Move with map roots; no shell-owned renderer names or hierarchy assumptions. |
| `MatchBootstrapCompositionSystemHelper.mapVisuals` direct binding | `TemporaryCompatibility` | Resolves a root named `Map` and consumes `MatchScene.StaticMapPresentationManifest`. | Replace with explicit active-map root/manifest binding after the split; preserve current fallback until parity. |
| `Game.Editor.StaticMapPresentationOutputOwnership` | `SharedConfig` | Reusable transaction/output-set rules protect manifest-owned scenes and stale cleanup. | Parameterize by explicit map output ownership before multi-map production; no directory-wide discovery. |
| `Game.Editor.StaticMapPresentationSceneWiring` | `TemporaryCompatibility` | Opens canonical `Match.unity` and writes the hardcoded manifest field. | Keep compatibility entry point; later map products are wired through explicit map-scoped input. |
| `Game.Editor.StaticMapAndroidBuildSceneResolver` | `TemporaryCompatibility` | Validates the one hardcoded canonical Match manifest/integrity set and includes only owned chunks. | Preserve for current Android builds; later delegate to selected catalog-approved map products without scanning all outputs. |
| `Game.Editor.StaticMapPresentationBaker` canonical Match/output constants | `TemporaryCompatibility` | Current authoritative bake is hardcoded to the one Match source/product set. | Retain as compatibility entry point; map-scoped bake input is direction-dependent and remains later. |

## Lifecycle Contract

1. The active map supplies one validated manifest, map root, canonical renderer set, and generated chunk set.
2. The shell binds the streamer and renderer-ownership transaction explicitly.
3. Gameplay readiness waits for required presentation preload.
4. Exit/failure begins chunk drain before map unload.
5. Renderer ownership is restored and map-specific references are cleared even when preload/drain fails.
6. Android build resolution includes only the accepted current map's manifest-owned chunks until a later delivery design replaces the compatibility resolver.

## Source Evidence

| File | SHA-256 |
|---|---|
| `Assets/Game/Scripts/Composition/StaticMapPresentationStreamer.cs` | `5732b2574f4c66280b9c62ab9a26d9a8d7f69541954c7bffbf1e494f631eddc0` |
| `Assets/Game/Scripts/Composition/StaticMapPresentationManifestIndex.cs` | `401e245927d600d07b793f9b598398ec354c51bcd76b4797e5a41c91ca39a8e4` |
| `Assets/Game/Scripts/Composition/MenuBootstrapCompositionSystemHelper.cs` | `e1d2c94ab91fc0af7b3ef6d3b1cd1d3fcf14ce5ae0f950303c55771791190d4d` |
| `Assets/Game/Scripts/Composition/MatchBootstrapCompositionSystemHelper.cs` | `4ab539902e867989d99fc934de572a1c4958881e81d8d812cab2b45b5d17024a` |
| `Assets/Game/Scripts/Rendering/StaticMapPresentationManifest.cs` | `f4c1d26d86140c3399e6830cf32c0f112ca81e67152144b05795db1cb699d4ce` |
| `Assets/Game/Scripts/Rendering/StaticMapPresentationOwnership.cs` | `290b52a9061ccd9f72c3d47e571e890e5ccd7227a51a1cc54dd2e0cd2d11ed7b` |
| `Assets/Game/Scripts/Editor/StaticMapPresentationSceneWiring.cs` | `77c2a1acbb6069d54cff618149ab1055e59f52a27df62c583a5bd7251db7bd04` |
| `Assets/Game/Scripts/Editor/StaticMapAndroidBuildSceneResolver.cs` | `5ba0eb16227529e3f809d2fb2f62e47e6a9b11e9c5de53342b024594d6f61f16` |
| `Assets/Game/Scripts/Editor/StaticMapPresentationOutputOwnership.cs` | `ebf1ec4d661479f4bb2819ccb45091e92d7d0992f336bd99c306159b0c2cf985` |
| `Assets/Game/Scripts/Editor/StaticMapPresentationBaker.cs` | `48e026f28d2c49f7053ddf57c290b8427b8d10dc2d2998771400dc4e56417beb` |

This slice is documentation-only. It changes no scene, manifest, integrity ledger, generated chunk, config, build setting, or runtime source.
