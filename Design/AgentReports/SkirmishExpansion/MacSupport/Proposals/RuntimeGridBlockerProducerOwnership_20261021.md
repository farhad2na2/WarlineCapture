# Runtime blocker producer ownership proposal

Status: proposal only, awaiting Windows source review. No production implementation, native run, compilation, source-fit certification or mission acceptance is included.

Base: `b24cfc2dfaef28cbf9b69236d22f64315eb018a9`. The adjacent `.source-pins.json` pins every input below by SHA-256. No source hash or successful test outcome transfers to a future implementation.

## What the current normal path actually owns

- `MatchSceneView.OperationMapLifecycle.cs:28-43` publishes/validates operation-map metadata before `matchBootstrapSystem.Awake`. Publication obtains the actual root from `OperationMapRuntimeBootstrapSceneSystemHelper.TryPublish`; `CampaignMissionOperationMapReuseUtility` validates the loaded physical source against that root. Bind to this actual returned root/generation, never a guessed generation or the newest singleton by shape.
- `MatchSceneView.OperationMapLifecycle.cs:201-264` requires all seven flags for a loaded map, including source content, SubScene, metadata, surface, authored conversion, presentation manifest and preload. Physical-source validation is a separate metadata byte. They remain independent admission requirements.
- `MatchBootstrapCompositionSystemHelper.cs:977-1010` disables legacy runtime map presentation for `CanonicalPresentationMode.EntityScene`. `GameplayFeatureStartupCompositionSystemHelper.cs:55-72` then does not construct/init the legacy city or blocker helper. Source inspection establishes this branch, not the current live S002 mode. An absent helper is not a ready empty blocker source.
- `RuntimeGridBlockerPresentationSystemHelper.Init` takes config, a Transform and city read model. It stores no World, map root, source identity or generation. `TryGetEntityManager` resolves the mutable default World on every call; `TryGetGridData` picks the global matching grid singleton. Neither proves source ownership.
- `WriteDependencyState:313-342` creates a global dependency entity or adopts a singleton. Its fields describe helper/city readiness, not authored scene ownership. It has no source stamp or legitimate `SceneTag`. The managed helper and its runtime-created entities must not be presented as baked members of a source scene.
- `GameplayRuntimeUpdateCompositionSystemHelper.Runtime.cs:76-101` updates city then helper only while runtime is requested and `RequiresUpdate` is true. A provenance validity check cannot depend solely on this dirty-state path: a ready helper can stop updating while its source is unloading.
- `Dispose:108-124` clears helper records, marks `_initialized=false`, sets `_readyForDependents=true`, then writes the global dependency again. This must not leave an observable valid producer binding. `MatchSceneView` shuts down runtime before disposing metadata/source load, providing an invalidation boundary.
- `DynamicBlockerInitSystem` owns storage on its actual selected grid; `RuntimeGridDeduplicationSystem` prefers authored grids over bootstrap grids. `StaticGridBlockerUpdateSystem` writes Blocked/Counts/FriendlyPass on the selected grid. Shape equality, successful allocation and helper readiness do not prove that the current source's blocker writes have completed.

## Recommended smallest honest contract

Implement only after review, in two separable parts. A helper stamp by itself is insufficient for the EntityScene-disabled-helper case or for proof of mask application.

### 1. Explicit runtime binding, without a fake SceneTag

Propose a new unmanaged `RuntimeGridBlockerSourceBinding` companion on the helper-owned dependency entity. Keep existing gameplay readiness fields separate. Supply a bound World and a source context from the normal map lifecycle through match bootstrap/feature startup; stop using the ambient default World for this producer's grid/dependency writes.

| Field | Actual writer and meaning |
| --- | --- |
| `MapRoot`, `Generation` | Actual validated root returned by publication and its positive active/metadata generation. Reuse reads the existing generation, never increments or hardcodes it. |
| `WorldSequence` | The bound World's sequence, checked against the actual capture World; entity indexes alone are not cross-World identity. |
| `AuthoredSceneRoot`, `AuthoredSubSceneGuid` | Resolve the metadata's navigation GUID using SceneSystem in that World; require the root loaded and referenced by the grid's real SceneTag/section/root chain. No SceneTag is added to the dependency. |
| `GridEntity` | The exact grid actually selected by producer operations, including entity version; reject a bootstrap/foreign/replaced grid even if dimensions match. |
| `SourceIdentityHash`, `SourceContentHash`, `GeneratedMetadataHash` | Snapshot from validated root metadata, compared to that root and the launch-selected source at each publication/read. These prevent a same-root/same-generation metadata substitution. |
| `PublisherEpoch`, `Revision`, `State` | Nonzero epoch for this actual helper instance/binding lifetime, monotonically increasing revision for its writes, state Unbound/Binding/Active/Invalidated. Epoch changes on rebind; no valid default values. |

These proposed fields record what an actual producer used; they are not permission for a witness to attach ownership to arbitrary global state. No capture caller can supply an override, optional missing identity or success boolean.

Concrete writes:

1. The lifecycle provides context only after physical validation. Startup may retain an Unbound/Binding context while SubScene/grid/readiness is pending; it must not manufacture seven ready flags or stall existing gameplay solely to make diagnostics pass.
2. The helper owns a fresh dependency entity it created in its bound World. Do not adopt a foreign/unstamped ready singleton and bless it retrospectively. If another dependency already exists, leave diagnostic binding unavailable and expose a conflict; do not create a second gameplay singleton or delete an unknown entity. Handling an old unowned singleton requires its existing owner to perform verified teardown.
3. Before every actual grid selection/write, validate bound World, root/generation/source hashes, loaded source root and grid's baked scene ownership. Capture the exact selected GridEntity. Binding to grid A while mutating singleton grid B is forbidden; route those operations through the bound grid.
4. Publish binding and dependency state on the same owned entity, synchronously on the producer's main-thread write boundary, with revision updated last. Active means the helper has a valid source binding, not that all masks/scenery are complete. Revalidate independently of `RequiresUpdate`, at the normal lifecycle/update boundary. A source change immediately invalidates the binding; never relabel old records as the new source.
5. Dispose invalidates first, before destroying visuals/clearing records or metadata unload. Never call the existing ready-state writer after invalidation. Remove only the dependency/binding entity verified as belonging to this exact publisher epoch and World. A later publisher must not be destroyed by an old helper's Dispose. World replacement cannot redirect disposal into the new default World.

Concrete reads in the future witness:

- Retain the real grid SceneTag/section/source-root check, all seven mandatory flags, source validation, exact launch/session joins, pre/post-fence component rereads and separate Occupied storage/provenance.
- Replace only the dependency's currently impossible baked SceneTag requirement with a mandatory producer binding join. Require every field above, exact dependency entity and publisher epoch/revision, and the same actual source root/grid/World. Missing binding, stale epoch, unowned singleton or any mismatch remains unavailable. A genuinely untagged and unbound legacy dependency still refuses; no fallback to dimensions/readiness.
- Read binding before the fence, after the fence, and after mask copy. Any change or loss refuses and disposes owned copies. Keep the one-shot synchronization/allocation cost explicit; not a per-frame observer.

### 2. Acknowledgment by the actual grid mask writer

Helper readiness is not an acknowledgment that static blocker deltas reached the copied grid. Propose a small `RuntimeGridBlockerAppliedRevision` on the actual grid, written only by the existing mask producer after its real delta/ECB work has completed. It references binding owner/epoch, grid/source generation and the last applied producer revision. The witness requires the applied epoch/revision to cover the captured active binding revision, and repeats this read across fence/copy. Invalid/unbound producer revisions are never acknowledged. Storage recreation/deduplication/source rebind invalidates the acknowledgment; old bits are never certified merely by changing its stamp.

Before implementation, pin and review all mutation paths: initial spawn/finalization, blocker removal/destruction, friendly-pass changes and storage replacement. Each must advance/invalidate the appropriate revision before capture can observe stale masks as current. This acknowledgment establishes that the selected producer revision was applied, not per-bit source attribution or complete correspondence with rendered city scenery.

## EntityScene and missing-producer boundary

The minimal helper binding above enables a trustworthy observation only when the normal source actually uses that producer. If the actual S002 launch disables it, this patch alone must continue refusing availability. Do not instantiate a helper, synthesize a dependency, or add a disabled/empty source-owned sentinel just to satisfy the witness.

For that path, a separate reviewed contract must originate from the actual authored/packed grid-mask owner and the existing normal composition policy: explicit producer applicability from that policy, genuine bound grid/source lifecycle, completed actual mask writes, and evidence of which scenery contributors exist. Absence of the legacy helper does not establish absence of authored/packed blockers. Windows must review that real path before replacing the mandatory dependency query. This is an unresolved integration boundary, not a waiver or claimed working capture.

## Meaningful controls required for an implementation

1. A real producer-bound fixture publishes from an actual loaded authored source, uses its exact grid and has mask revision acknowledged only after the writer runs. Synthetic identity-only controls remain separate from this native integration control.
2. Omitted binding, zero epoch/revision, metadata-only or any omitted seven flags, missing physical validation, missing real grid SceneTag, missing scene root/section membership all refuse.
3. Equal-shaped grids from another source, replacement entity/version, foreign World with equal entity indexes, same source with stale generation, and same-generation changed source hashes refuse.
4. Source unload/rebind while `RequiresUpdate=false` invalidates capture. Dispose cannot leave ready ownership or delete a newer publisher. Default-World replacement does not redirect the old helper's writes/disposal.
5. A foreign ready singleton is not adopted; duplicate/unknown owners refuse without creating another singleton or destroying external entities.
6. Pending spawn/city/finalization, producer revision newer than applied revision, mask/storage recreation, removal/friendly-pass mutation and acknowledgment from another grid/epoch refuse. No successful acknowledgment before actual writer completion.
7. Binding/readiness/dependency/grid/applied revision changes across the real job fence or copy refuse and release copies. Occupied remains a separate live-occupancy mask, not source/static equivalence.
8. EntityScene-disabled helper remains unavailable until its distinct real producer contract is reviewed and measured. No caller omission can turn it into an empty-success case.

## Review and validation boundary

Likely future implementation surfaces: GridComponents (companion contracts), MatchSceneView lifecycle/context handoff, match/feature startup, the helper's binding/grid selection/writes/disposal, the actual storage/mask owner acknowledgment, witness reads and controls. This proposal changes none of them. Avoid unrelated gameplay/UI/map assets. Independently recompile reverse consumers when signatures change.

The ten b24 witness controls remain written but unexecuted; the earlier nine were also unexecuted. No previous six-control pass transfers. A later focused native run first requires fresh Menu/Fig/Hub ownership checks and lane coordination, the checked macOS GUI wrapper, unique full log/timeout, required marker and explicit settings/cleanup checks. No run is authorized by this proposal. Full source-fit, all-size facility/packing/service and normal-input mission/ARIA/result/return/device acceptance remain separate.
