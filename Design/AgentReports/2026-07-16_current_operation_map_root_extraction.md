# Current Operation Map Root Extraction

Date: 2026-07-16

## Scope

Removed exactly five accepted `ShellOwned` roots from only the staged current
operation-map scene:

- `Bootstrap`
- `Main Camera`
- `Global Volume`
- `Directional Light`
- `Directional Light (1)`

The source `Assets/Game/Scenes/Match.unity` remains unchanged at SHA-256
`dca7c83b765ce40099ce4fd62a53cbee5bc306107f8a026abcb941a59bf53a46`.

## Retained Staged Roots

The staged scene now contains exactly eleven ordered roots:

1. `MatchSubScene` (temporary compatibility reference)
2. `Start` (temporary map marker)
3. `End` (temporary map marker)
4. `Decorations`
5. `Reflection Probe`
6. `Map`
7. `Faction2`
8. `Faction3`
9. `Faction4`
10. `Faction5`
11. `Faction1`

Staged scene SHA-256 after extraction:
`6de8262786ef91bd3f2137e4e7624e59793f16f26b791e563f0651589b148b32`.

`OperationMapCurrentCompatibilityRootExtractor` is idempotent and validates
the complete ordered root set before and after mutation. Any unexpected root,
missing root, duplicate name, or order drift fails before deletion.

## Validation

- Root extraction and original ownership baseline EditMode tests: `28 / 28`
  passed.
  - Results: `/private/tmp/opmap-current-root-extract-tests.xml`
  - Log: `/private/tmp/opmap-current-root-extract-tests.log`
- Source-growth and non-ECS naming architecture gates: `24 / 24` passed.
  - Results: `/private/tmp/opmap-current-root-extract-architecture.xml`
  - Log: `/private/tmp/opmap-current-root-extract-architecture.log`
- Unity compilation: zero compiler errors.
- `git diff --check`: passed for source/tooling/docs; the staged Unity scene is
  stored as a Git LFS object.

The original runtime route remains unchanged. Subscene ownership, map-specific
placement configs, metadata binding, presentation baking, full parity, and the
atomic shell cutover remain open.
