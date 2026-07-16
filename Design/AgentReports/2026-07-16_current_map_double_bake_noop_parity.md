# Current Map Double-Bake No-Op Parity

Date: 2026-07-16

## Scope

Ran the authoritative current-map static-presentation bake twice from current
`main` with Android as the active build target. This validates the existing
compatibility map only; it does not introduce map-scoped outputs, staged-map
baking, Addressables, loading, or future-map generation.

## Result

Both bakes produced the same accepted result:

- sources: `16,542`
- chunks: `514`
- scanned objects: `19,635`
- content hash: `9eebc7c8aa774d5f505cb684099d133a`
- reused scenes: `1`
- scenes written: `0`
- stale scenes deleted: `0`
- reuse rejection: `none`

All `1,033` tracked files under
`Assets/Game/GeneratedStaticMapPresentation/` were SHA-256 hashed before the
first bake and after each bake. Both post-bake lists are byte-identical to the
pre-bake list. The worktree remained clean. The manifest file SHA-256 is
`3940dcac3d42c703f47cf11f134b183c4554f9944629925f7b38957e08d93746`.

## Evidence

- First bake: `/private/tmp/opmap-current-bake-1.log`
- Second bake: `/private/tmp/opmap-current-bake-2.log`
- Pre-bake hashes: `/private/tmp/opmap-current-bake-before.sha256`
- First post-bake hashes: `/private/tmp/opmap-current-bake-after-1.sha256`
- Second post-bake hashes: `/private/tmp/opmap-current-bake-after-2.sha256`
- `git diff --check` passed.
