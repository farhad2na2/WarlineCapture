# Operation Map Entities Linkage Acceptance

Date: 2026-07-18
Result: Passed

## Change

- `OperationMapEntitySceneBuildAdditions` uses Unity Entities' official
  `IEntitySceneBuildAdditions` build hook to register the map-owned subscene.
- The registered asset is
  `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01_subscene.unity`
  with GUID `d50925a18e9164ce782536576cb833d8`.
- The generated `.entityheader` and `.entities` files remain build outputs and are
  not manually Addressable.
- `OperationMapEntityScenePackageGate` fails Android APK/AAB builds when the
  expected header, section stream, or `scene_info.bin` is absent.

## Validation

- Focused EditMode tests: `7 / 7` passed.
- Editor local-bundle lifecycle PlayMode test: `1 / 1` passed.
- Gated Android APK build: passed with zero C# compiler errors.
- APK size: `487,659,156` bytes.
- APK payload includes:
  - `assets/EntityScenes/d50925a18e9164ce782536576cb833d8.entityheader`
  - `assets/EntityScenes/d50925a18e9164ce782536576cb833d8.0.entities`
  - `assets/EntityScenes/scene_info.bin`
- Addressables settings contain no explicit entry for the source subscene or its
  generated Entities files.

Detailed logs are stored outside the repository under `/private/tmp`.
