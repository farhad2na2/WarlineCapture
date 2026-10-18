# Workspace consolidation — 2026-10-01

Active project: `/Users/farhad/Projects/WarlineCapture`.

The primary `main` was fast-forwarded from `2c36f349fb7feef6214cf9e4fbf67c0eeb3111af` to reviewed mission commit `37a7cce2cf0ae7368cd99dcb40b29318da316b90`. The docs and comic commits are already ancestors. No source merge conflicts occurred.

## Remote preservation

| Work | Pushed reference | Commit |
| --- | --- | --- |
| Seven mission reworks | codex/airlift-airfield-review | 37a7cce2cf0ae7368cd99dcb40b29318da316b90 |
| Comics | codex/bookend-comics | 09f765b6dac605240ba4a95fc6bae3d5c89e557f |
| Detached docs | codex/demo2-docs-preserved-20261001 | b4161fb4fa2b3ed133ce1e3b634955d661493586 |
| Primary local changes | codex/workspace-primary-backup-20261001 | fd757c39af5d8640b413bf07f1d1d2b90d671a1c |
| Airlift local evidence/import state | codex/workspace-airlift-backup-20261001 | 1f0a733df00931f00223381aef48e824035ffcb9 |

Backup commits are Git stash snapshots: parent 2 preserves the index; parent 3 preserves untracked files. They preserve work rather than representing a playable candidate. Primary local changes were restored without conflicts. The two runtime/test edits already match the reviewed branch exactly; four pre-existing material/font edits and primary untracked recovery/evidence files remain local.

The redundant `airlift-local-evidence.tar.gz` was removed from the current tree on 2026-10-18. Before removal, all **343 regular files** were compared using their Git blob hashes against the untracked snapshot tree `1f0a733df00931f00223381aef48e824035ffcb9^3`: **zero mismatches and zero missing/extra files**. The remote branch `codex/workspace-airlift-backup-20261001` was verified to point to that preserved commit. The removed archive was 373,161,923 bytes, SHA-256 `50ad226e0ab4ef22529591230d64ae46c84ccd73d000e35bcd51ea771f0f9380`. Preserve that backup reference; it owns the snapshot, which is not duplicated in the current main tree.

Recover the snapshot without changing the checkout:

```sh
rtk git fetch origin codex/workspace-airlift-backup-20261001
rtk proxy git archive --format=tar --output=/private/tmp/warline-airlift-evidence.tar '1f0a733df00931f00223381aef48e824035ffcb9^3'
```

This recreates the file contents in a new tar archive; it does not reproduce the original gzip container byte-for-byte. Extract it outside the active project when needed.

External evidence folders and ignored validation logs are preserved alongside this report. Cache, generated project and zero-byte lock files are not player source. All 64 reviewed source hashes and 28 decompressed validation log hashes match (`verification.json`).

## Cleanup and review

All three active Codex worktrees were removed after remote verification. The Airlift worktree was archived using Codex's managed archival tool. The docs and comics worktrees were clean and removed with Git. Remaining external evidence was copied and checked before deleting their parent directories. No Unity process owned a Codex worktree, and no Editor or Hub was closed.

Continue exclusively in the primary checkout. Review instructions: `Design/AgentReports/MapVariantMissionRework/REVIEW.md`. The seven mission review candidates retain their previously recorded native validation evidence; human and device acceptance remain pending. This checkout consolidation does not constitute a new gameplay acceptance run.
