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

- Before publication, Unity `6000.5.2f1` focused EditMode fixtures discovered 64 tests and passed 63. All new LF/CRLF/lone-CR, omitted-extension, LF legacy-contract, binary, content-sensitivity, and resolver diagnostic tests passed; only the expected stale-manifest resolver case failed with expected `0a587783351110d16353575d15d1b5cd`, actual `db252d7b61b87458dafbd30acb8a5559`.
- A controlled Unity raw-byte diagnostic through the same dependency graph also produced `db252d7b61b87458dafbd30acb8a5559`, proving the published canonical update was required independently of normalization mechanics.
- `Game.Editor.StaticMapPresentationBaker.Bake` was run exactly once to publish the normalized canonical hash. Across all 1,033 tracked generated-presentation files, before/after SHA256 comparison found exactly one changed path: `StaticMapPresentationManifest.asset`.
- The manifest diff contains exactly one changed value: canonical dependency hash `0a587783351110d16353575d15d1b5cd` to `db252d7b61b87458dafbd30acb8a5559`.
- Manifest content hash remains `9eebc7c8aa774d5f505cb684099d133a`. The integrity JSON/meta and every one of the 514 chunk scene/meta pairs are byte-identical to the pre-bake snapshot.
- Post-bake Unity focused EditMode fixtures passed 64/64, including `ResolveForCurrentProject_IncludesEnabledBaseScenesThenEveryManifestChunkExactlyOnce`.
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
- `/private/tmp/build110-authoritative-bake.log`
- `/private/tmp/build110-baker-before-all.sha256`
- `/private/tmp/build110-baker-after-all.sha256`
- `/private/tmp/build110-post-bake-focused.xml`
- `/private/tmp/build110-post-bake-focused.log`

## Risk

Local authoritative bake and resolver validation are complete without chunk/content regeneration. The remaining risk is Windows Jenkins checkout behavior itself; local tests cover CRLF equivalence and Jenkins ordering/configuration statically, but the Windows lane has not yet run on this PR.
