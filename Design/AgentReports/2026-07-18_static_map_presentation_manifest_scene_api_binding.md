# Static Map Presentation Manifest Scene API Binding

## Scope

- Added a narrow optional manifest-binding contract to the existing presentation scene API boundary.
- The streamer now binds and validates a manifest-aware scene API before queuing any chunk work.
- Binding failures fail closed without starting a scene operation.
- Existing Unity scene API behavior remains unchanged because it does not implement the optional contract.

## Validation

- Streamer and retained-handle Addressables API EditMode validation: `33 / 33` passed.
- Unity compiler errors: `0`.
- `git diff --check`: passed.

## Runtime Effect

- No production API switch yet. The default streamer still uses `StaticMapPresentationUnitySceneApi`; activation remains a separate measured slice.
