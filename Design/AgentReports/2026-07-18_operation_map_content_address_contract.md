# Operation Map Content Address Contract

## Scope

- Added one deterministic runtime/editor contract for static-presentation chunk addresses.
- Reused the contract in the local Addressables layout builder.
- Rejected invalid map ids and chunk ids before producing an address.
- Preserved the approved address format: `operation-map/{operationMapId}/presentation/{chunkId}`.

## Validation

- Focused address and existing strict layout validation: `8 / 8` passed.
- Real one-map Addressables layout builder: passed and produced no generated asset/settings diff.
- Unity compiler errors: `0`.
- `git diff --check`: passed.

## Runtime Effect

- None. The active static-presentation scene API is unchanged; this contract prevents the later Addressables scene API from duplicating editor-only address logic.
