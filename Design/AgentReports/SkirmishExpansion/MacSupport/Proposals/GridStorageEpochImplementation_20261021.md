# Storage epoch implementation for Windows source review

Review only; not integrated or native-validated. Parent inventory commit `3ccc3e838591bf3b02b2a27c4c46ba3ff0f99eab` precedes code edits on `codex/mac-grid-storage-epoch-ownership-implementation-20261021`, based on exact 4f88. Adjacent manifest pins six implementation source/meta files. No main push/merge, heavy compile or Unity run was performed.

## Concern A: actual mask correctness

The real storage component now carries a monotonic StorageEpoch, applied epoch/revision/layout, pending-write flag and exhaustion flag. EnsureStorage invalidates before allocation; replacement/disposal retains monotonic identity on the same grid entity. Missing/wrong array lengths also trigger replacement. No-op ensures preserve epoch. Occupied remains a separately allocated component; blocker reset leaves it alone.

The existing actual static blocker writer checks both grid layout and history's exact GridEntity/version+epoch. Reset, pending recovery, layout change or old/mismatched history causes fresh masks and a full positive replay. It retains the existing three query phases and their natural ECS iteration order; no sorting, filtering or structural history reset moves contributors between phases. New blockers apply first, previous blockers replay second, removed history retires third without subtracting into fresh masks or clearing new pass cells. Normal same-epoch incremental overlap/pass behavior, including the legacy removal clear, is retained. Application history is committed through the actual ECB; applied epoch/revision is published only after successful playback, reacquiring component data after structural changes. Pending is published before clearing or applying, and a failed pass requires a full replay next time. Overflow leaves diagnostics unavailable while gameplay can continue.

This changes DynamicBlockerComponent and StaticBlockerPrevBounds layouts. Windows must cleanly recompile all reverse consumers and Burst-generated ECS code; old binaries, existing six-control receipts, or normalized helper comparison do not establish compatibility. Source review should also assess the extra history scans and metadata writes per update. No native performance claim exists.

## Concern B: diagnostic ownership, independent of gameplay

The witness requires an actual committed storage/layout/revision before copy and stable writer epoch/revision afterward. It also inspects actual global input and cleanup domains without writing them. New/unapplied/moved/opening/pass-changed inputs, pending removals, malformed leftover history, foreign attempts, unknown source provenance or current attempts lacking real grid/source binding refuse diagnostic capture. It verifies authored SceneTag ownership through the existing real section/root join; no SceneTag is added to global runtime state. Gameplay never reads a diagnostic certificate and still includes foreign/unknown contributors in the global masks.

All previous seven readiness flags, exact source/session/generation, loaded scene ownership, frame, post-fence admission and post-copy guards remain. The untagged helper dependency refusal remains; EntityScene policy is unchanged and no helper is synthesized. **This implementation does not finish producer applicability for normal S002:** an enabled helper still needs reviewed producer binding, and a disabled helper still needs authoritative policy/actual-writer applicability before replacing the mandatory dependency contract. The new writer metadata proves only work done on these arrays, not presentation completeness or per-bit static-source equivalence. Unknown/malformed histories do not become source-owned by surviving a replay.

## Twelve independent expected-mask/control groups

`GridBlockerStorageEpochTests` creates isolated Worlds, uses the actual DynamicBlockerInitSystem allocation owner and StaticGridBlockerUpdateSystem+ECB, and compares all 64 count/blocked/pass cells to independent rectangle/opening expectations. The ordering control reads the existing query iteration order only to identify the legitimate last pass writer; it also checks mixed new/existing replay and preserves the old removal clear.

1. Initial allocation is unavailable until real writer and history ECB commit.
2. Same-size reset with unchanged history restores rectangle, one-cell opening and friendly-pass mask.
3. Reset plus moved/removed inputs produces no negative old-epoch subtraction.
4. Incremental overlap/pass ordering and mixed new→existing replay order are retained.
5. A deliberately seeded partial pending pass replays cleanly; exhausted epoch continues gameplay but never certifies. This models an interrupted state; it is **not** an injected allocator/ECB exception or successful recovery of a real native failure.
6. No-op ensure preserves epoch; equal-cell-count changed origin invalidates and replays.
7. Retired-grid cleanup/new grid entity cannot inherit old history subtraction.
8. A foreign/prior attempt remains in gameplay masks but fails source ownership; a current attempt without real binding also refuses.
9. Authored writer runs without a legacy helper; synthetic scene tags do not grant normal Play/helper policy or mission availability.
10. Effective input mutation and storage replacement invalidate observations; owned copied masks survive source replacement. This is a synchronous mutation model, **not** a scheduled-job race reproduction. Existing ten b24 tests retain their explicit simulated fence-state limitations.
11. Blocker reset preserves live occupancy; occupancy-only replacement preserves blocker epoch/masks.
12. Destroyed history cleans up; unknown and malformed leftover contributors remain unqualified while gameplay remains unfiltered.

All twelve are written and **unexecuted**. Original ten b24 controls are byte-identical and **unexecuted**. Source ordering/whitespace checks alone were performed. Direct allocator/ECB exception injection, concurrent scheduled mutation, source unload under a real idle helper, duplicate authored grids, and full normal helper-disabled policy remain pending beyond these isolated controls. Do not mark the complete prior twelve-group proposal matrix accepted merely because this suite has twelve methods.

Required next boundary: Windows independent source review and clean reverse-consumer compilation before integration. Any later native control needs a fresh process ownership check and coordinated wrapper-only lane. Current private process receipt observed only Hub646/Fig6010, with no Warline Editor; this is not a Menu lane-release signal or new native run authorization. Full source-fit, all sizes, normal mission/ARIA/result/return, queue/720, paid-resource and device/APK acceptance stay separate.
