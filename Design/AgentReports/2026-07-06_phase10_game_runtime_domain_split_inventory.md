# Phase 10 Game.Runtime Domain Split Inventory

## Purpose
Reopen Phase 10 of `Design/Architecture/architecture_performance_audit_followup_tracker.md` and identify a compiler-safe first assembly split. This inventory is behavior-preserving: it does not move files, change gameplay ownership, or add new runtime logic.

## Current Assembly Surface

Current runtime-facing asmdefs under `Assets/Game/Scripts`:

| Assembly | Current role |
|---|---|
| `Game.Components` | ECS component/data currency. |
| `Game.Configs` | Config assets and config-side runtime types. |
| `Game.Tactical.Contracts` | Tactical contract types. |
| `Game.Catalog.Contracts` | Catalog contract types. |
| `Game.Rendering.Contracts` | Rendering contract types. |
| `Game.UI.Contracts` | UI contract types. |
| `Game.UI.Shell.Contracts.Ecs` | UI shell ECS contract buffer/component surface. |
| `Game.Runtime` | Main gameplay runtime assembly, currently too broad. |
| `Game.Rendering` | Rendering ECS/presentation assembly. |
| `Game.Authoring` | Authoring/baking surface. |
| `Game.UI.Runtime` | Canvas/UI runtime views and binders. |
| `Game.UI.Shell.Ecs` | UI shell ECS read/write bridge surface. |
| `Game.Composition` | Scene/bootstrap/composition glue. |

Current source count under `Assets/Game/Scripts`: `814` C# files.

Largest top-level buckets:

| Bucket | C# files |
|---|---:|
| `Systems` | 353 |
| `UI` | 129 |
| `Editor` | 63 |
| `Components` | 47 |
| `Rendering` | 44 |
| `Environment` | 44 |
| `Configs` | 37 |
| `ScenarioLab` | 24 |
| `Composition` | 23 |
| `Authorings` | 15 |

## Target Runtime Domains

The Phase 10 target domains remain:

| Target assembly | Intended owner | First-slice notes |
|---|---|---|
| `Game.Runtime.Pathfinding` | Path request collection, scheduling, result apply, grid/surface path snapshots, path diagnostics. | Do not split only `Systems/Pathfinding/PathfindBatchJob.cs`; it depends on internal path/surface helpers still in `Game.Runtime`. A safe split needs the cohesive pathfinding owner set. |
| `Game.Runtime.Combat` | Attack orders, attack execution, damage, missile launch/projectile state, combat VFX requests that are gameplay-owned. | Must keep presentation-only VFX instantiation in rendering/presentation edges. |
| `Game.Runtime.Buildings` | Building runtime, placement, production, resource storage, faction summaries, building command request/result flows. | Highest churn risk because building systems currently bridge UI, runtime buildings, resource storage, and production transport visuals. |
| `Game.Runtime.Transport` | Boarding, capacity, passenger state, deploy, rope disembark, airdrop, transport diagnostics. | Phase 9 already created helper seams; still keep movement/deploy/rope/airdrop authority in ECS command owners. |
| `Game.Runtime.SelectionCamera` | Selection command modes, selected-unit command routing, RTS camera request/mode systems, tactical follow camera ECS mode state. | Must not move Canvas visual-state applicators into gameplay runtime domains. |

## Cross-Domain Rule

Contracts and data assemblies are the only intended cross-domain currency:

- `Game.Components`
- `Game.Configs`
- `Game.Tactical.Contracts`
- `Game.Catalog.Contracts`
- `Game.Rendering.Contracts`
- `Game.UI.Contracts`
- `Game.UI.Shell.Contracts.Ecs`

Domain assemblies should not call into each other through managed helper shortcuts unless a later slice explicitly documents and validates that dependency. If a dependency is genuinely shared, move the shared type to a contract/data assembly only when it is a data contract rather than gameplay ownership.

## First Split Risk Finding

The apparent quick split, `Assets/Game/Scripts/Systems/Pathfinding/PathfindBatchJob.cs`, is not actually safe as a one-file assembly split:

- It is already isolated in a subfolder, but it uses internal `Game.Runtime` path/surface helper types.
- A child asmdef for that single file would either fail compilation, force those helper types public prematurely, or create an assembly reference cycle.
- The correct first pathfinding split is a cohesive owner-set split that includes the scheduler/apply/request/snapshot/helper types needed by `PathfindBatchJob`.

## Validation Baseline

Before physical Phase 10 splits, the local compiler baseline is clean:

- `dotnet build Game.Runtime.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors.
- `dotnet build Game.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors.
- `dotnet build Game.Tests.Editor.csproj --no-restore -v:q -clp:ErrorsOnly`: passed, 0 errors.

## Next Slice

Choose one first physical split:

1. Pathfinding cohesive owner-set split, if the moved type set can be kept internal to `Game.Runtime.Pathfinding` and referenced from `Game.Runtime` only through a minimal public system/job surface.
2. A smaller non-pathfinding domain where all required types already sit together and depend only on contracts/data assemblies.

After the asmdef/reference change, immediately run `git diff --check`, runtime/editor/editor-test dotnet builds, and the focused Unity validation for that domain when Unity is available.
