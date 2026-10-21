# Actual grid blocker producer applicability and storage recovery proposal

Proposal only. Base `d1f4dfa2029428b3a0d6e5a46b2c8201f27fd5a8`; exact raw source/blob identities are in the adjacent source-pin manifest. No production code, geometry, budgets, mask contents, native run or validation outcome changes here. This refines the previous ownership proposal: a helper-ready stamp or a subsequent writer tick cannot certify replacement storage.

## Actual producers and applicability

| Stage and pinned source | Observed behavior | Proposed evidence responsibility |
| --- | --- | --- |
| `GridAuthoring.Baker.cs` | Bakes GridConfig and walkable/road/sidewalk/dirt buffers; configured blocked cells affect GridWalkable. Does not allocate dynamic masks. | Authored grid and buffer identity, linked through its real source scene lifecycle. Never conflate GridWalkable with DynamicBlocker.Blocked. |
| `StaticGridBlockerAuthoring.cs`; `OperationMapBuildingAuthoring.cs` | Bake StaticGridBlocker, UnitGrid and GridBlockerSize; buildings also carry authored foundation data. | Source-scene contributor identity and actual foundation/opening semantics, not a bounding-box approximation. |
| `RuntimeGridDeduplicationSystem` | Removes fallback runtime grids when an authored grid is present; disposes their storage. Does not prove only one authored source remains. | Invalidate retired grid certificates before storage disposal. Multiple/foreign authored grids remain an ambiguity, not first-grid success. |
| `DynamicBlockerInitSystem`; storage utility | Select actual grid, allocate missing storage or replace invalid storage. | Exact grid binding and new storage epoch; allocation is not mask readiness. |
| `StaticGridBlockerUpdateSystem` | Global initial/change/removal queries apply counts/blocked/friendly-pass deltas to one grid; plays its ECB at the end. Buildings use RuntimeBuildingCombatInfo.OriginCell, not selection-center UnitGrid. | Actual contributor inventory, actual applied input revision and committed storage epoch only after all deltas/history updates and ECB succeed. |
| Legacy presentation helper | May generate runtime contributors and readiness. EntityScene policy can disable it entirely. Uses global World/grid/dependency selections today. | If applicable, explicit helper producer binding and mutation revision. Its absence or readiness cannot stand in for the actual mask writer. |
| `DynamicOccupancyRebuildSystem` | Independently rebuilds Occupied from its tracked units and clears/repopulates that mask. | Separate live occupancy epoch/observation; never counts as static source equivalence. |

`RuntimeGridBootstrapStartupSystemHelper` is an additional mutation boundary: it may select an existing authored grid, write GridConfig, resize/reinitialize its buffers and ensure storage. Equal cell count can leave dynamic arrays allocated while grid origin/shape semantics change. Thus storage allocation epoch alone is insufficient; source/grid/buffer mutation must also invalidate the binding/input revision even if no array is replaced.

## Confirmed reset failure mode

`RuntimeGridPersistentStorageUtilitySystemHelper.EnsureStorage:68-79,138-155` replaces invalid dynamic storage with zero counts, clear blocked bits and friendly-pass bytes set to 255. Neither EnsureStorage nor DisposeStorage removes StaticBlockerPrevBounds or previous-opening history from contributors.

`StaticGridBlockerUpdateSystem:140-158` skips a blocker when bounds, opening and friendly-pass identity match that history. Consequently a blocker applied to old storage can remain unchanged, survive storage replacement, and never be added to the new mask. A later successful OnUpdate plus ECB playback is **not** proof of reapplication. Moved/removed contributors can also subtract old-epoch bounds from fresh zero counts, yielding invalid counts. Unchanged faction openings/pass cells can stay absent. Retain this as a specific observed source hazard, not a claim that a native reproduction or fix already passed.

## Proposed minimal certificates and their real owners

Keep producer applicability separate from mask/storage ownership. Suggested future fields, subject to Windows review:

- **Grid binding**: bound World sequence, exact GridEntity/version, validated map root/version/generation, loaded authored scene root/GUID, source identity/content/generated hashes, grid dimensions/cell size/origin, binding epoch. The normal lifecycle establishes source context; actual grid selection validates it. Any mismatch or ambiguous grid makes the certificate unavailable. No fabricated SceneTag or retrospective blessing of a global singleton.
- **Storage state** on that actual grid: monotonically increasing nonzero StorageEpoch, Pending/Rebuilding/Committed/Invalidated, InputRevision, AppliedRevision, AppliedStorageEpoch, contributor inventory identity and validity. Storage utility/actual storage owner starts or invalidates the epoch before replacing/disposing masks. Failed allocation never leaves a valid previous certificate. A no-op EnsureStorage must not increment epoch or erase progress. Epoch wrap must invalidate/rebind, never reuse an old valid value.
- **Per-contributor application ownership**, extending or accompanying previous-bounds/opening history: exact grid/binding/storage epoch, contributor entity/version, source/attempt/helper owner class and identity, applied input revision. A cleanup-lifetime record is needed for removals after the active blocker component disappears. Existing PrevBounds has no grid/epoch identity and cannot authorize a delta against another storage instance.
- **Actual writer commit**: only StaticGridBlockerUpdateSystem (or a separately reviewed replacement of that actual owner) publishes AppliedRevision and AppliedStorageEpoch after the full work, history ECB playback and input stability checks succeed. A generic Update counter, Time.frameCount, helper readiness, nonzero masks or successful allocation cannot publish it.

InputRevision must describe all effective writer inputs, including additions, removals, bounds/foundation changes, openings, friendly-pass changes, source/attempt membership and grid/buffer/storage mutations. It cannot be only the optional helper revision. A reviewed implementation can conservatively census/fingerprint the writer's actual queries at each diagnostic commit boundary, rather than pretending every existing mutation producer already increments a counter. The census must include identity and exact effective input values; unchanged counts of entities are insufficient. The before/after inventory must agree, and its ordering/conflict semantics must match the actual writer. This adds observation cost and needs measurement before any production scheduling decision.

## Recovering new storage honestly

Recommended future path: a full owner-scoped reapplication transaction for every new/invalid storage epoch, instead of globally deleting previous bounds or blindly resuming incremental deltas.

1. Mark the new epoch Pending before readers can accept it. Complete actual tracked writers/readers using the established owner boundary; the witness never performs recovery or runs systems itself.
2. Resolve one actual source-bound grid and census the exact contributor/query domain. Reject source-fit certification if foreign/unknown source or attempt contributors are in the global writer domain. Do not silently filter them out of a copy of the live mask and call that normal equivalence.
3. Create/retain the new masks with existing initialization semantics. Replay **every current applicable contributor** into the new arrays, including unchanged blockers with old PrevBounds. Use the same authored OriginCell, clamp, opening and faction-pass behavior as the real writer. Never subtract old-epoch rectangles from the new arrays. Do not touch unrelated entities' history.
4. For replayed contributors, replace their application history with current bounds/opening/pass and this exact grid/storage epoch. Old-epoch removed contributors have no contribution in the new storage: retire only their verified owned history, without negative deltas. Same-epoch removals subtract exactly their recorded owned contribution before history retirement.
5. After actual replay and its history ECB complete, verify the complete contributor inventory/input revision stayed stable; verify source binding, actual grid shape/buffer identity and storage epoch unchanged. Then atomically publish Committed, AppliedStorageEpoch=current and AppliedRevision=the replayed InputRevision. Exception, partial replay, ECB failure, membership change or ambiguous ownership leaves Pending/Invalidated and no usable acknowledgment.
6. Only subsequent same-epoch changes use incremental deltas. A contributor with missing/mismatched application epoch cannot take the unchanged fast path. It requires the reviewed replay path or causes invalidation. Source/grid retirement invalidates before disposal; old cleanup cannot invalidate/destroy a new entity/version or another epoch.

A supported implementation may instead reconstruct all masks at each diagnostic boundary, but it still needs actual owner applicability, exact semantics, complete inventory and explicit commit. This proposal does not authorize such a change. Friendly-pass overlap ordering and any pre-existing gameplay conflict rules must be retained and reviewed; do not invent a new resolution policy while adding evidence.

## Source/attempt applicability without inventing an empty producer

The current writer queries are global. Classify every contributor and removal-history row actually participating:

- Authored contributors need the same real loaded SceneTag/section/root/GUID as the grid. `OperationMapBuildingComponent` membership by itself is not proof of that scene.
- Attempt contributors need the exact current `SkirmishAttemptOwnedComponent.SessionId`, plus a real runtime binding to this grid/source/generation; SessionId alone does not prove grid ownership. Keep them labeled as attempt contributions, not authored scenery.
- Runtime helper contributors need its producer-owned binding/epoch from the prior proposal, matching this grid and live applicability. Other runtime blockers require their own existing or newly reviewed producer provenance. Unknown and foreign entries invalidate source-fit attribution; do not delete or relabel them.
- Removed/dead entities must retain enough recorded application ownership to decide which grid/epoch their old deltas belonged to. Looking only at currently active StaticGridBlocker rows loses that information.

EntityScene disabling the legacy helper does **not** disable authored grid initialization or the actual static blocker writer. For a future normal capture, normal composition must publish an explicit applicability record derived from the actual presentation policy: legacy helper ActiveBound or DisabledByEntityScenePolicy. The latter describes only that helper's absence; it asserts neither zero blockers nor complete scenery equivalence. Actual authored/attempt contributor inventory plus a committed current storage epoch are still mandatory. Missing policy evidence remains unavailable. Do not instantiate a helper or dependency solely for diagnostics.

A future witness may replace the currently mandatory helper dependency with this reviewed applicability-and-actual-writer contract only after Windows review and native controls. Enabled helper requires its real bound dependency; policy-disabled helper requires authoritative mode evidence, not caller omission. In both cases the actual grid writer certificate is mandatory. Keep all seven map flags, physical validation, exact launch/session/source joins, and the b24 pre/post-fence/copy component revalidation. Read binding, policy, storage epoch, inventory/input and applied revision at all three points; any change refuses and disposes copies. Preserve Occupied separately and label source-only versus attempt contributions. Current b24 still refuses an untagged dependency; this docs branch does not change it.

## Required controls, all unexecuted

| Control | Required evidence / refusal |
| --- | --- |
| Initial epoch with authored blocker | Exact normal producer allocates then genuinely applies a known contributor and commits after ECB; blocked/count/pass and history match. No allocation-only success. |
| Reset with unchanged PrevBounds/opening/pass | Apply once; replace same-sized storage while retaining old history; ordinary unchanged tick must not certify. Full replay restores expected masks once, with new epoch. This is the decisive regression. |
| Old-epoch moved or removed blocker | No subtraction into fresh zero counts; no negative counts; old history retirement cannot corrupt new epoch. |
| Overlaps, openings and faction changes | Expected count multiplicity, full-cell opening semantics and real friendly-pass ordering preserved across replay and same-epoch deltas. |
| Partial allocation/replay/ECB failure | No Committed acknowledgment or old AppliedStorageEpoch reused; recovery remains pending. |
| No-op storage ensure; equal-count grid mutation | No-op keeps epoch; changed origin/dimensions/buffers invalidates binding/input even if allocations stay alive. |
| Fallback dedup, duplicate authored grids, unload/reload | Retired grid invalidated before dispose; foreign/ambiguous equal-sized grid refuses; new entity/version/source generation cannot inherit old commit. |
| Global source/attempt contamination | A foreign scene, prior SessionId, unknown runtime producer or foreign removal-history row makes source-fit unavailable even if cells/shape match. No silent filtered normal snapshot. |
| EntityScene helper disabled | Actual normal policy evidence plus genuine authored writer commit may support the future contract; missing helper policy cannot imply empty success. Enabled helper with missing/mismatched binding refuses. |
| Fence/copy race | Changed input inventory, applied revision, storage epoch, source generation, readiness, grid or dependency state across fence/copy refuses and releases copies. |
| Occupancy reset/churn | Separate live occupancy provenance retained; cannot satisfy static source completeness. |
| Idle teardown and stale cleanup | Invalidation works when helper RequiresUpdate=false; stale World/entity/epoch cleanup cannot touch a newer publisher. |

Tests must exercise the real allocation/reapplication/writer+ECB path on controlled entities, with an independent expected-mask reference, rather than merely testing an acknowledgment field setter. Synthetic missing/mismatch controls, native producer integration, actual normal capture, scenery-contributor completeness, all-size source fit and normal-input mission/result/return acceptance remain separate gates. No future implementation should be merged on a stamp-only test.

## Raw source identities and current validation limits

The manifest pins 18 exact raw Mac source blobs at d1. Windows reports its specific helper comparison matrix `2618076b` found exactly LF-to-CRLF over 896 lines between its CBD0 helper and the b24 helper. That statement is attributed to Windows: this proposal neither retrieves its raw evidence nor replaces either identity with a normalized hash. The Windows raw SHA is not supplied here and remains in its original receipt. There is no general normalization waiver, cross-checkout build equivalence or transfer of native outcomes.

The ten b24 controls remain written and unexecuted; the previous nine were unexecuted. No earlier six-control result transfers. This docs-only assignment starts no native work and grants no further run authority. Fresh local process ownership is recorded privately in coordination, not this public proposal. Protect Menu, Fig and Hub; before any later proposed native work, repeat ownership checks and report/coordinate one heavy lane. No shared Library copying, direct Unity invocation, macOS batchmode, IPC reset or process killing is part of this work.
