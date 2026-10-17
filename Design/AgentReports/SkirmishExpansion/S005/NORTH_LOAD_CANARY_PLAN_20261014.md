# North1632 scoped native compilation/load plan

No Assets were imported while preparing this plan. The root owns the active
native lane and must finish the current capture/validation before changing the
candidate. This gate verifies compilation and read-only load compatibility;
it must not start a north mission or claim player readiness.

## Exact future imported files

Copy bytes without line-ending conversion, after the root releases its freeze:

| Existing external source | Suggested unique Editor filename | SHA256 |
|---|---|---|
| `windows-north1632-draft/S005NormalNorthFlank1632Observer.cs.txt` | `Assets/Game/Scripts/Editor/Skirmish/S005NormalNorthFlank1632Observer.cs` | `5dda43e1f4a9b6c2aad6d7b2d5b0e6659aeb5541851113da8153fea02ba90fd7` |
| `windows-paid-plane-canary/NormalPaidPlaneLineageCanary.cs.txt` | `Assets/Game/Scripts/Editor/Skirmish/SkirmishS002AriaRunHarness.NormalPaidPlaneLineageCanary.cs` | `03da52225f3d16ba66b457e8695299ab8d92327b615ecc6fba372005190c776e` |

Use the existing Editor directory and its `Game.Editor` assembly. Let Unity
generate each new `.meta`, check its GUID uniqueness, and finish import warmup
before the candidate freeze. Both new `.cs` files and `.meta` files belong in the
fresh source manifest. Do not reuse the source0df6 fingerprint, historical Mac
assembly/PID, old one-use permit or the original preparation Git HEAD as a current
source pin. No file or meta is created by this plan.

The north file contains14 global static companion classes, all suffixed1632.
A current Assets `*.cs` definition scan found no collisions for any of them:
`S005NormalNorthFlank1632Observer`, `S005ManualFogFallback1632`,
`S005NativeIntroReady1632`, `S005PassengerCarrierIdentity1632`,
`S005ManualHomeDefense1632`, `S005PaidProducerCensus1632`,
`S005NativeOwnedActorBridge1632`, `S005PackedRenderCensus1632`,
`S005NorthFlank1632`, `S005FreshRocketeerPlanner1632`,
`S005ManualAuditOperandCensus1632`, `S005CatalogTouch1632`,
`S005EarlyRoute1632`, `S005GroundTouchDiagnostic1632`.

The paid-plane file intentionally extends the existing partial
`Game.Editor.SkirmishS002AriaRunHarness`, rather than creating a duplicate type.
Its unique gameplay entry is `RunS005NormalPaidPlaneLineageCanary`; its actual full
Game.Editor offline type compile passed. The root should still check loaded
Game.Editor assembly/MVID and exact installed file hashes in the native gate.

## Scoped entry and required evidence

The exact global executeMethod for the north **load-only** check is:

```
S005NormalNorthFlank1632Observer.Run
```

Its required marker is:

```
[S005NormalNorthFlank1632] loadedOnly=1 gameplayStarted=0 nativeAuthorityBridge=ExactReflectedNativeMethod
```

`Run` checks existing harness/private helper signatures, typed prepared route,
fresh-cohort planner and the exact reflected native owned-actor authority bridge.
It does not call `RunValidation`, register gameplay callbacks, select units,
purchase anything or launch a mission. Route/serializer QA caches may initialize.

Use `Tools/CI/ResolveUnityEditor.ps1` and the checked Windows
`InvokeUnityExecuteMethodValidation.ps1` with GUI licensing, explicit unique full
log and timeout. Keep Hub open/signed in, verify no Editor owns this project,
preserve failures, and enforce zero exit plus the exact marker. A timeout,
nonzero exit, source change, compile failure or missing marker fails this gate.

For a combined root-owned load checker, it is safe to call the north `.Run` and
inspect (without invoking) the plane entry via reflection. Record that both
methods resolve to the expected freshly compiled Game.Editor assembly/MVID and
that no matching global north observer type is duplicated in another loaded
assembly. Capture Editor idle/not-playing before and after and confirm no
external gameplay callbacks were added. Do not invoke either gameplay entry
merely to test method availability.

## Gameplay remains a different gate

The north gameplay entry is **`S005NormalNorthFlank1632Observer.RunValidation`**.
It requires a separately reviewed pinned job/claim and the Windows trial runner.
Its evidence filenames are `native1632-*` in the S005 report directory; preserve
all existing names and refuse collisions. Installed source must exactly match
the prepared observer pin above. Freeze the complete current source plus native
compiled-DLL manifest after all grouped imports/metadata stabilize; verify the
same identity after the run. The root owns the single native dispatch schedule.

The plane gameplay entry requires a fresh absolute
`WARLINE_S005_PLANE_CANARY_OUTPUT` and checks only normal-funded Airport/Plane
delivery lineage/home parking. Its later native trial does not close north,
Fighter/AA, cargo flight/unload, full normal Victory/result/return or device and
real-player gates. Owner screenshots remain asynchronous review evidence.
