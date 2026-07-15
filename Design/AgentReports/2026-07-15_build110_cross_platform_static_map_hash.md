# Build 110: Cross-platform static-map canonical source hash

Date: 2026-07-15
Branch: `codex/opmap-build-110-cross-platform-source-hash`

## Root cause and fix

The canonical source hash previously SHA256-hashed dependency and `.meta` files as raw checkout bytes. A Windows fresh clone with unpinned `core.autocrlf` could therefore hash CRLF while the manifest was baked from LF bytes.

- Text detection now scans full file content with strict UTF-8 decoding and rejects NUL, invalid encoding, and non-line-ending control characters.
- Known binary formats bypass text normalization even when their payload is valid UTF-8.
- Detected text normalizes CRLF and lone CR to LF before SHA256; binary bytes remain unchanged.
- Resolver validation recomputes the actual hash and reports both manifest expected and recomputed actual values on mismatch.
- `.gitattributes` enforces LF for Git-detected text while preserving the existing LFS `-text` override.
- Jenkins clones without checkout, sets repository-local `core.autocrlf=false`, initializes sparse checkout, sets sparse paths, and only then checks out `main`.

## Validation

- Unity `6000.5.2f1` focused EditMode fixtures: 64 tests discovered, 63 passed. All new LF/CRLF/lone-CR, omitted-extension, LF legacy-contract, binary, content-sensitivity, and resolver diagnostic tests passed.
- Remaining focused failure: `ResolveForCurrentProject_IncludesEnabledBaseScenesThenEveryManifestChunkExactlyOnce` reported expected `0a587783351110d16353575d15d1b5cd`, actual `db252d7b61b87458dafbd30acb8a5559`.
- Controlled Unity raw-byte diagnostic through the same dependency graph also produced `db252d7b61b87458dafbd30acb8a5559`. This proves the current fresh-worktree drift is independent of line-ending normalization.
- The worktree was clean before validation. Unity's first import temporarily rewrote `Assets/Settings/UniversalRenderPipelineGlobalSettings.asset`; that generated mutation was restored. The raw diagnostic was then repeated with tracked files clean and produced the same actual hash.
- No CR bytes were found in tracked non-generated `Assets` or `Packages` inputs, no Git LFS pointer payloads were present, and `git diff --check` passed.
- Jenkins checkout-order static assertion passed: clone line 38, `core.autocrlf` line 42, sparse materialization line 47, checkout line 48.
- Python Jenkins/CI contract suite passed: 21 tests.
- `ScriptArchitectureAlignmentContractTests.RunAssemblyBoundaryValidation` passed with marker `tests=31`.
- `EcsBurstHotPathArchitectureTests.RunFocusedValidation` passed with marker `tests=10`.
- Android was not built locally, per task scope.

Logs:

- `/private/tmp/build110-focused-editmode-final.xml`
- `/private/tmp/build110-focused-editmode-final.log`
- `/private/tmp/build110-raw-hash-diagnostic.xml`
- `/private/tmp/build110-raw-hash-diagnostic.log`
- `/private/tmp/build110-architecture-boundaries.log`
- `/private/tmp/build110-architecture-ecs.log`

## Blocker / risk

The required existing LF hash `0a587...` is not reproducible from this clean fresh worktree even with the pre-fix raw-byte algorithm; Unity computes `db252...`. Manifest/chunk regeneration was intentionally not performed because those files are outside build110 scope. The PR fixes cross-platform line-ending invariance and fail-closed diagnostics, but the independent canonical dependency-graph drift must be reconciled before the real-project resolver gate can pass.
