# Bounded recovery from an overflowed wider guard

Trial 03 captured a genuine OpaqueShadowsOn pool deficit: native selection and
assignment failure enums were None; required rows 4725 exceeded the unchanged
4589-slot bucket by 136. Other buckets fit. Allocation, first apply and active
slot counts were present. This is actual capacity pressure for the selected
view, not an invalid configuration enum or an empty-view failure.

Root's checked no-Play replay 01 used the exact captured camera matrices and
the actual baked render database, with matching database hash. It measured:

| Envelope | Minimum X,Z | Maximum X,Z |
| --- | --- | --- |
| Captured published envelope | 4,6 | 39,24 |
| Current required envelope | 26,17 | 32,23 |
| Current materialized envelope | 25,16 | 33,24 |

The current view is a strict contained subset. The original scheduling branch
reuses a containing guard on camera-only changes and retains its selected
allocation. The replay supports this recovery proposal; it did not observe
the failed process's private guard/revision state and does not prove the
smaller view fits its buckets. Preserve the original replay receipt's old
`staleOverflowGuardReuseProven` label as historical raw output; interpret it
only as the measured containment/shrink. The relabel successor uses
`containedSmallerOverflowEnvelopeMeasured`, explicitly records private state
unobserved and smaller bucket fit unestablished. No replay rerun is needed
solely for that evidence-label correction.

The conditional correction changes three existing scheduler seams only. When
the last published state overflowed and the actual new materialized envelope
is a strict contained subset of the cached guard, it schedules that exact
smaller envelope and replaces the cached guard bounds immediately. It never
clears overflow directly. Only the existing actual assignment/publish path can
clear it. An equal envelope cannot request recovery, so an unchanged unfit
view remains failed without scheduling every camera publication. Healthy guard
reuse and normal out-of-guard/state-change behavior remain on the existing path.
No pool capacity, budget, render schema, physical source, camera profile or
opening readiness predicate changes.

Exact candidate imports, owned by root:

- `OperationMapRenderVirtualizationInitializationSystem.cs.txt` here replaces
  `Assets/Game/Scripts/Rendering/Systems/OperationMapRenderVirtualizationInitializationSystem.cs`;
  SHA256 `1b1e6a10bd9280a18dec7b1a09b5657a0e213b0052777f81d58bd80eee65b180`.
- `Tools/CI/SkirmishProductionDrafts/OperationMapRenderOverflowGuardRecovery.cs.txt`
  goes to `Assets/Game/Scripts/Rendering/OperationMapRenderOverflowGuardRecovery.cs`;
  SHA256 `ebe761e33073d5dda9047710864b639d9cf64b4656dd6851656c4b8bcc05ac47`.
- `Tools/CI/SkirmishProductionDrafts/OperationMapRenderOverflowGuardRecoveryTests.cs.txt`
  goes to `Assets/Tests/Editor/OperationMapRenderOverflowGuardRecoveryTests.cs`;
  SHA256 `1098d36a489daf255d42e536ecd21fc6699e22db41ae0beb8b2f8f6cba63f151`.

Four prepared native NUnit controls run the actual initialization/selector/
assignment/publish pipeline on a synthetic three-row, two-slot database. They
require real initial overflow, recovery through a smaller actual camera
projection, unchanged capacity, retained historical deficit, no repeated
allocation for 100 unchanged overflowing views, healthy cache reuse, and
rejection of equal/invalid/noncontained recovery requests. They are synthetic
native unit fixtures, not gameplay or readiness evidence. Entry:
`OperationMapRenderOverflowGuardRecoveryTests.RunFocusedValidation`; marker
`[OperationMapRenderOverflowGuardRecovery] result=Passed tests=4`.

Full Rendering owner (70 retained sources) and full Editor tests (684 retained
sources) type compilation passed against the exact projected system/helper/
tests. Durable evidence: `validation/compile-8_qgjws6`. Initial test type compile
failed for a missing Configs using and is preserved in `compile-6525vvyp`.
No NUnit methods or Unity runs were executed by this lane. Independent authoring
review found no static blocker. Native tests and a fresh normal camera trial
must establish the correction's actual utility before any rendering/mission
acceptance is asserted.

The separately prepared scheduler diagnostic adds actual private guard/revision
fields and current native envelope recomputation to resolve remaining ambiguity
if the next normal run fails. It replaces the same diagnostic partial; it does
not modify the runtime publisher or bypass the gate. All imports and native
execution remain root-owned.
