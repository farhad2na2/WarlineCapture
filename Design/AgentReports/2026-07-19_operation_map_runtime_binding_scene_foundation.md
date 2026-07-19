# Operation Map Runtime Binding Scene Foundation

## Scope

- Added the editor-only `OperationMapRuntimeBindingSceneBuilder` and `OperationMapRuntimeBindingSceneValidator`.
- Generated one presentation-only runtime binding scene for `opmap.skirmish.desert_base_01`.
- Did not change the current Addressables source-scene entry or runtime definition reference.

## Output

- Authoring input: `Assets/Game/Scenes/OperationMaps/Skirmish/opmap_skirmish_desert_base_01.unity`
- Runtime binding scene: `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime.unity`
- Runtime binding ledger: `Assets/Game/GeneratedOperationMaps/RuntimeBinding/opmap.skirmish.desert_base_01/opmap_skirmish_desert_base_01_runtime_ledger.json`
- Runtime scene size: 12,837 bytes.
- Scene SHA-256: `d76262b42ced151629d2bba5b822eb07784c4e93374f821e12cb57e28e5dd9a9`.
- Ledger SHA-256: `674b899f83495dcb9f8dcc326f268491f691532367fed2f5f4dfc17b52df439c`.

The scene contains exactly one `OperationMapSceneView` and one `RuntimeMapBindings` root. It retains the map definition, grid, surface, placement, and SubScene asset references while keeping decoration/building/vehicle roots empty. It uses `OperationMapCanonicalPresentationMode.PresentationOnly` and contains no renderer, camera, light, collider, or rigidbody under the map root.

Ledger schema 2 records 47,192 stripped source renderers, zero source colliders, zero copied managed-physics identities, seven output GameObjects, and exact source/input/output hashes.

## Validation

- Unity 6000.5.2f1 compiled `Game.Editor` and dependent assemblies with zero compiler errors.
- First successful Unity run generated, reopened, and structurally validated the scene.
- A second accepted run logged `result=Passed reused=true`.
- Focused scene validation passed `3 / 3`, covering committed structure, authoring/presentation-scene dependency exclusion, and stray-renderer rejection.
- Scene, scene `.meta`, ledger, and ledger `.meta` SHA-256 files were byte-identical across the accepted no-op run.
- `git diff --check` passed.
- The final editor scene-close lifecycle adjustment compiled with zero errors via `dotnet build Game.Editor.csproj --no-restore`. Its Unity rerun was blocked before compilation by a licensing-client connection timeout; the earlier accepted Unity generation, no-op, and focused-test evidence remains the feature validation basis.

Logs:

- `/private/tmp/operation-map-runtime-binding-ledger.log`
- `/private/tmp/operation-map-runtime-binding-ledger-noop.log`
- `/private/tmp/operation-map-runtime-binding-ledger-v2.log`
- `/private/tmp/operation-map-runtime-binding-ledger-v2-noop.log`
- `/private/tmp/operation-map-runtime-binding-focused-3.log`
- `/private/tmp/runtime-binding-ledger-before.sha256`
- `/private/tmp/runtime-binding-ledger-after.sha256`
- `/private/tmp/runtime-binding-v2-before.sha256`
- `/private/tmp/runtime-binding-v2-after.sha256`

## Remaining Gate

The generated scene is not yet a package input. The next slice must add it to the Core group under the existing stable source-scene address, remove the authoring scene from Addressables, update the definition GUID reference, and validate real-bundle Editor/Android behavior before claiming the memory/package optimization.
