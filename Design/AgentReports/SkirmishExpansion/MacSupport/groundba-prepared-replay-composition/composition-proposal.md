# GroundBA prepared replay composition proposal

This is a Docs-only design against Mac source `923ed3cd5baf44251b65684c8e7a1c302db1305b`, with evidence parent `b4fc03bb25389e73cc54d6fd204effb26c05f29d`. No executable composition is produced. Windows committed source, exact patch hashes and test entry/marker are required before implementation. The attached JSON pins the four actual Mac source files and relevant method bodies; line numbers refer to that source commit.

## Exact overlap

| Location | Mac change | Windows change reported by coordinator | Composition requirement |
|---|---|---|---|
| `MpGroundBaPilot.cs`, `ApplyMpPilotEndpointRevision`, line 23 | Method itself remains identical to f90; the same file adds generic tuple, compiler-signature and report controls | Add optional `preparedAuthoring=false`; prepared path uses existing endpoint preflight without revising layout | Preserve exact committed Windows implementation once supplied, including original ownership/source/grid/route guards. Only the explicit prepared path may avoid initial four-point revision. Preserve original default behavior. Do not replace the whole MP file. |
| `SimultaneousRefinement.cs`, `RefineLoadedCandidateCore`, lines 64–82 | Adds own-source generic service authority/grid guard, nine-profile service and strict route/independence/receipt wiring throughout this method | Forward the existing `preparedAuthoring` parameter into the MP endpoint revision call | Compose both deltas within this one method: preserve Mac generic guards and all later service/route checks, and the exact Windows prepared forwarding call. Preserve MP-only invocation condition. Never activate MP revision/geometry for generic S026. |
| `FacilityPreferences.cs`, `RefineFacilityPreferences`, lines 285–295 | Generic service-authority admission elsewhere in this file; existing replay call already passes prepared flag true | No direct Windows byte delta supplied here | Verify existing positional argument mapping (`compounds,true,false,true`) still reaches `preparedAuthoring=true`. Do not add another solver or reapply revision before prepared replay. |
| `PilotFacilityRelocation.cs`, existing relocation service guard | Generic authority joins the existing MP/AP authority alternatives | No direct Windows byte delta reported | Keep exact reviewed Mac postimage unless exact committed Windows patch demonstrates another owned dependency. |

The shared MP file also contains Mac `MpPilotServiceAccess` generic authority admission, independent expected compiler signatures, exact whole-signature receipt comparison, the generic entry and 47 proposed controls. Keep them all. The shared Simultaneous method also keeps source grid matching, exact joint routes, canonical coverage, all-profile independence and every member service ingress/return. A successful text merge cannot establish these properties by itself.

## Behavior to preserve

For default/unprepared MP authoring, retain exact source ownership, unqualified binding, actual grid, exact three-route identities, original four-point provisional route requirement, typed endpoint ownership, projection/envelope checks, trial preflight before mutation and actual-layout preflight. Preserve six-endpoint revision, pads and later hull validation. A prepared replay must validate its current route endpoints without restoring original waypoint count, reverting bounded repair or changing layout. It returns the reported `PreparedMpPilotRouteEndpointValidation_NoRevision_NotNativeFit` scope; this is endpoint evidence only.

Prepared replay must still fail on malformed or foreign endpoint identities, invalid coordinates/projection/envelope, invalid source/grid ownership or preflight rejection. It must not turn `preparedAuthoring` into a skip of ownership, source, normal compiler, route independence, service access, candidate cleanup or downstream geometry checks. The exact failure order and implementation will be reviewed against actual Windows bytes rather than invented here.

Generic GroundBA S026 continues through its existing allocator with MP geometry flag false. S049/S050 still delegate to their dedicated entry. Preserve S049 main-corridor segmentation and native route/service predicates. The prior preservation claim for the entire `ApplyMpPilotEndpointRevision` body applies to the frozen Mac proposal; future composition must explicitly record its intentional Windows delta, rather than falsely claim its body remains byte-identical. All other protected methods need explicit unchanged-body checks or a separately reviewed delta.

## Implementation sequence after exact Windows source arrives

1. Pin committed Windows revision, parent, both raw files, owned diff, helper dependencies and exact 20-control method/marker. Root must identify any other changed paths; do not import an entire moving Windows checkout.
2. Review three inputs separately: f90 baseline, frozen Mac four-file successor and exact Windows endpoint/forwarding delta. Produce a narrow composed proposal outside Assets first. Preserve unrelated CE and other Windows changes. Never treat this Docs branch as executable approval.
3. Inventory every modified method and compare protected bodies, default branch semantics, prepared branch nonmutation and generic service/signature wiring. Record actual before/after raw SHA256s, patch hash and explicit overlap resolution. Keep new source epoch distinct from both old sources and old compiled receipts.
4. Run only separately authorized compilation and controls after source/API review, actual owner/RSP/reference closure and fresh capacity checks. Keep Windows clean4 compiler64818 failure intact. Actual 47 and Windows20 execution results remain separate, and normal input/native mission readiness remains a later gate.

No heavy checkout/import/compile/native is authorized by this proposal. No active Library copy, physical-map change, budget substitution, main merge or overwrite of Windows source is proposed. Preserve original CC operational failures and all original runtime/settings evidence.
