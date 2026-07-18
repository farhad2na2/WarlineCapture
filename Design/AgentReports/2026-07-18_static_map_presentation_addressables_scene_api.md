# Static Map Presentation Addressables Scene API

## Scope

- Added an inactive retained-handle Addressables scene API for static-presentation chunks.
- Binds validated manifest scene paths to the shared runtime/editor address contract.
- Retains one load handle per scene path and transfers it to exactly one unload operation.
- Rejects manifest replacement while any chunk is retained.
- Releases failed loads before retry and keeps a loaded scene available after a failed unload so the caller can retry.

## Validation

- Focused Unity EditMode validation: `4 / 4` passed.
- Covered address resolution, load/unload ownership, failed-load retry, failed-unload retry, and replacement rejection.
- Unity compiler errors: `0`.
- `git diff --check`: passed.

## Runtime Effect

- None. `StaticMapPresentationStreamer` still uses its existing scene API. A later slice must bind this API to the streamer and rerun full lifecycle/Android validation before build-scene removal.
