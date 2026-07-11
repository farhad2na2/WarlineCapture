# APH-700 First-Party Assembly Dependency Report

- Task: `APH-700`
- Source fingerprint (SHA-256): `13405427ecd913342c639ca357335800f9203d7e1fe4345aeceacab6b18b37bb`
- Determinism: No timestamp or mutable VCS state; ordinal path ordering, normalized LF output, and a content-derived source fingerprint.
- Scope: Direct dependencies and source-level cross-domain type references for first-party asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.

## Summary

| Metric | Count |
|---|---:|
| First-party assemblies | 18 |
| First-party asmdef edges | 79 |
| External declared references | 82 |
| Owned C# source files | 1141 |
| Indexed visible types | 2477 |
| Resolved cross-domain type occurrences | 29754 |
| Distinct cross-domain type references | 2614 |
| Ambiguous type tokens omitted | 39 |
| Unowned scoped C# source files | 0 |

## First-Party Assemblies

| Assembly | asmdef | Sources | Types | First-party edges | External refs |
|---|---|---:|---:|---:|---:|
| `Game.Authoring` | `Assets/Game/Scripts/Authorings/Game.Authoring.asmdef` | 15 | 17 | 2 | 5 |
| `Game.Catalog.Contracts` | `Assets/Game/Scripts/Catalog/Contracts/Game.Catalog.Contracts.asmdef` | 1 | 1 | 0 | 0 |
| `Game.Components` | `Assets/Game/Scripts/Components/Game.Components.asmdef` | 52 | 489 | 0 | 5 |
| `Game.Composition` | `Assets/Game/Scripts/Composition/Game.Composition.asmdef` | 28 | 40 | 12 | 9 |
| `Game.Configs` | `Assets/Game/Scripts/Configs/Game.Configs.asmdef` | 44 | 101 | 2 | 5 |
| `Game.Editor` | `Assets/Game/Scripts/Editor/Game.Editor.asmdef` | 83 | 119 | 14 | 10 |
| `Game.Rendering` | `Assets/Game/Scripts/Rendering/Game.Rendering.asmdef` | 44 | 72 | 3 | 8 |
| `Game.Rendering.Contracts` | `Assets/Game/Scripts/Rendering/Contracts/Game.Rendering.Contracts.asmdef` | 2 | 3 | 0 | 1 |
| `Game.Runtime` | `Assets/Game/Scripts/Game.Runtime.asmdef` | 472 | 1053 | 6 | 11 |
| `Game.Runtime.Pathfinding` | `Assets/Game/Scripts/Systems/Pathfinding/Surface/Game.Runtime.Pathfinding.asmdef` | 4 | 4 | 1 | 3 |
| `Game.Tactical.Contracts` | `Assets/Game/Scripts/Contracts/Game.Tactical.Contracts.asmdef` | 1 | 9 | 0 | 0 |
| `Game.Tests.Editor` | `Assets/Tests/Editor/Game.Tests.Editor.asmdef` | 227 | 233 | 15 | 9 |
| `Game.Tests.PlayMode` | `Assets/Tests/PlayMode/Game.Tests.PlayMode.asmdef` | 6 | 6 | 11 | 4 |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef` | 20 | 113 | 1 | 0 |
| `Game.UI.Runtime` | `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef` | 116 | 149 | 3 | 4 |
| `Game.UI.Shell.Contracts.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef` | 1 | 37 | 1 | 2 |
| `Game.UI.Shell.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef` | 22 | 28 | 8 | 4 |
| `ProjectTools.Editor` | `Assets/Editor/ProjectTools.Editor.asmdef` | 3 | 3 | 0 | 2 |

## Every First-Party Assembly Edge

| Source | Target | Type occurrences | Distinct types | Source files |
|---|---|---:|---:|---:|
| `Game.Authoring` | `Game.Components` | 202 | 104 | 15 |
| `Game.Authoring` | `Game.Configs` | 31 | 18 | 10 |
| `Game.Composition` | `Game.Authoring` | 34 | 5 | 13 |
| `Game.Composition` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Composition` | `Game.Components` | 162 | 46 | 6 |
| `Game.Composition` | `Game.Configs` | 99 | 37 | 8 |
| `Game.Composition` | `Game.Rendering` | 32 | 9 | 5 |
| `Game.Composition` | `Game.Rendering.Contracts` | 7 | 2 | 1 |
| `Game.Composition` | `Game.Runtime` | 173 | 52 | 12 |
| `Game.Composition` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.Composition` | `Game.UI.Contracts` | 94 | 41 | 7 |
| `Game.Composition` | `Game.UI.Runtime` | 29 | 9 | 4 |
| `Game.Composition` | `Game.UI.Shell.Contracts.Ecs` | 43 | 10 | 2 |
| `Game.Composition` | `Game.UI.Shell.Ecs` | 2 | 2 | 1 |
| `Game.Configs` | `Game.Catalog.Contracts` | 2 | 1 | 2 |
| `Game.Configs` | `Game.Components` | 75 | 13 | 3 |
| `Game.Editor` | `Game.Authoring` | 139 | 8 | 20 |
| `Game.Editor` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Components` | 623 | 123 | 19 |
| `Game.Editor` | `Game.Composition` | 76 | 2 | 11 |
| `Game.Editor` | `Game.Configs` | 141 | 25 | 26 |
| `Game.Editor` | `Game.Rendering` | 29 | 4 | 4 |
| `Game.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Runtime` | 199 | 46 | 21 |
| `Game.Editor` | `Game.Runtime.Pathfinding` | 6 | 2 | 4 |
| `Game.Editor` | `Game.Tactical.Contracts` | 6 | 1 | 2 |
| `Game.Editor` | `Game.UI.Contracts` | 19 | 9 | 2 |
| `Game.Editor` | `Game.UI.Runtime` | 174 | 34 | 16 |
| `Game.Editor` | `Game.UI.Shell.Contracts.Ecs` | 83 | 5 | 7 |
| `Game.Editor` | `Game.UI.Shell.Ecs` | 0 | 0 | 0 |
| `Game.Rendering` | `Game.Components` | 514 | 72 | 31 |
| `Game.Rendering` | `Game.Configs` | 19 | 6 | 5 |
| `Game.Rendering` | `Game.Rendering.Contracts` | 2 | 2 | 2 |
| `Game.Runtime` | `Game.Components` | 9592 | 395 | 314 |
| `Game.Runtime` | `Game.Configs` | 227 | 51 | 38 |
| `Game.Runtime` | `Game.Rendering.Contracts` | 3 | 2 | 1 |
| `Game.Runtime` | `Game.Runtime.Pathfinding` | 10 | 4 | 6 |
| `Game.Runtime` | `Game.Tactical.Contracts` | 153 | 3 | 26 |
| `Game.Runtime` | `Game.UI.Contracts` | 133 | 17 | 22 |
| `Game.Runtime.Pathfinding` | `Game.Components` | 29 | 11 | 4 |
| `Game.Tests.Editor` | `Game.Authoring` | 56 | 3 | 11 |
| `Game.Tests.Editor` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Components` | 9353 | 364 | 145 |
| `Game.Tests.Editor` | `Game.Composition` | 92 | 13 | 11 |
| `Game.Tests.Editor` | `Game.Configs` | 416 | 49 | 44 |
| `Game.Tests.Editor` | `Game.Editor` | 97 | 22 | 11 |
| `Game.Tests.Editor` | `Game.Rendering` | 235 | 48 | 12 |
| `Game.Tests.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Runtime` | 2393 | 333 | 134 |
| `Game.Tests.Editor` | `Game.Runtime.Pathfinding` | 9 | 2 | 2 |
| `Game.Tests.Editor` | `Game.Tactical.Contracts` | 105 | 7 | 6 |
| `Game.Tests.Editor` | `Game.UI.Contracts` | 267 | 63 | 24 |
| `Game.Tests.Editor` | `Game.UI.Runtime` | 493 | 60 | 20 |
| `Game.Tests.Editor` | `Game.UI.Shell.Contracts.Ecs` | 275 | 25 | 21 |
| `Game.Tests.Editor` | `Game.UI.Shell.Ecs` | 48 | 17 | 17 |
| `Game.Tests.PlayMode` | `Game.Authoring` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Components` | 420 | 81 | 6 |
| `Game.Tests.PlayMode` | `Game.Composition` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Configs` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Runtime` | 73 | 32 | 6 |
| `Game.Tests.PlayMode` | `Game.Tactical.Contracts` | 2 | 1 | 1 |
| `Game.Tests.PlayMode` | `Game.UI.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.UI.Runtime` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.UI.Shell.Ecs` | 0 | 0 | 0 |
| `Game.UI.Contracts` | `Game.Tactical.Contracts` | 22 | 7 | 6 |
| `Game.UI.Runtime` | `Game.Catalog.Contracts` | 32 | 1 | 4 |
| `Game.UI.Runtime` | `Game.Tactical.Contracts` | 73 | 7 | 10 |
| `Game.UI.Runtime` | `Game.UI.Contracts` | 485 | 88 | 50 |
| `Game.UI.Shell.Contracts.Ecs` | `Game.UI.Contracts` | 27 | 15 | 1 |
| `Game.UI.Shell.Ecs` | `Game.Catalog.Contracts` | 4 | 1 | 1 |
| `Game.UI.Shell.Ecs` | `Game.Components` | 830 | 100 | 16 |
| `Game.UI.Shell.Ecs` | `Game.Configs` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Runtime` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Tactical.Contracts` | 13 | 2 | 3 |
| `Game.UI.Shell.Ecs` | `Game.UI.Contracts` | 232 | 58 | 7 |
| `Game.UI.Shell.Ecs` | `Game.UI.Runtime` | 33 | 7 | 6 |
| `Game.UI.Shell.Ecs` | `Game.UI.Shell.Contracts.Ecs` | 507 | 37 | 19 |

## Top Cross-Domain Type References

| Rank | Source | Target | Type | Occurrences | Source files |
|---:|---|---|---|---:|---:|
| 1 | `Game.Runtime` | `Game.Components` | `Game.Components.GridConfig` | 682 | 123 |
| 2 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitGrid` | 381 | 79 |
| 3 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.Faction` | 380 | 57 |
| 4 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitGrid` | 374 | 50 |
| 5 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 342 | 16 |
| 6 | `Game.Runtime` | `Game.Components` | `Game.Components.Faction` | 317 | 72 |
| 7 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitHealth` | 291 | 49 |
| 8 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitHealth` | 276 | 63 |
| 9 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridConfig` | 250 | 47 |
| 10 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RuntimeBuildingEntity` | 245 | 15 |
| 11 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitFootprint` | 231 | 48 |
| 12 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitFootprint` | 205 | 34 |
| 13 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTransportPassengerElement` | 200 | 6 |
| 14 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMove` | 198 | 25 |
| 15 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandResultElement` | 183 | 15 |
| 16 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 181 | 8 |
| 17 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 179 | 21 |
| 18 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.EngageTarget` | 176 | 28 |
| 19 | `Game.Runtime` | `Game.Components` | `Game.Components.GridWalkable` | 174 | 34 |
| 20 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.SelectedUnitTag` | 173 | 24 |
| 21 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridWalkable` | 164 | 24 |
| 22 | `Game.Runtime` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 162 | 53 |
| 23 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RuntimeGameplayStateComponent` | 147 | 6 |
| 24 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitPathRequest` | 142 | 49 |
| 25 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.ResourceExchangeQueueComponent` | 141 | 13 |
| 26 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitAirComponent` | 140 | 15 |
| 27 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTarget` | 135 | 21 |
| 28 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitPathRequest` | 134 | 22 |
| 29 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitTarget` | 132 | 43 |
| 30 | `Game.Runtime` | `Game.Components` | `Game.Components.EngageTarget` | 132 | 42 |
| 31 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionInputStateComponent` | 124 | 18 |
| 32 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.AudioPlaybackRequestElement` | 121 | 17 |
| 33 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMovementBehavior` | 118 | 25 |
| 34 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitAirComponent` | 114 | 25 |
| 35 | `Game.Runtime` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 114 | 16 |
| 36 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitAirMovement` | 112 | 55 |
| 37 | `Game.Runtime` | `Game.Components` | `Game.Components.GridRoad` | 107 | 38 |
| 38 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitSourcePrefabKey` | 106 | 38 |
| 39 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 106 | 23 |
| 40 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RtsSelectionInputCompositionSystemHelper` | 106 | 7 |
| 41 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitCombat` | 105 | 24 |
| 42 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionCommandResultElement` | 102 | 10 |
| 43 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitTransportPassengerElement` | 100 | 15 |
| 44 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.DynamicOccupancyComponent` | 98 | 22 |
| 45 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionInputStateComponent` | 98 | 10 |
| 46 | `Game.Runtime` | `Game.Components` | `Game.Components.RuntimeGameplayStateComponent` | 94 | 25 |
| 47 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.ResourceExchangeWalletComponent` | 93 | 10 |
| 48 | `Game.Runtime` | `Game.Components` | `Game.Components.ManualMoveOrderTag` | 89 | 31 |
| 49 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitAirMovement` | 88 | 30 |
| 50 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitMove` | 87 | 36 |

## External Declared References

These are retained so every reference declared by a first-party asmdef remains auditable; they are not first-party domain edges.

| Source | Declared reference | Kind |
|---|---|---|
| `Game.Authoring` | `Unity.Collections` | externalName |
| `Game.Authoring` | `Unity.Entities` | externalName |
| `Game.Authoring` | `Unity.Entities.Hybrid` | externalName |
| `Game.Authoring` | `Unity.Mathematics` | externalName |
| `Game.Authoring` | `Unity.Transforms` | externalName |
| `Game.Components` | `Unity.Collections` | externalName |
| `Game.Components` | `Unity.Entities` | externalName |
| `Game.Components` | `Unity.Entities.Graphics` | externalName |
| `Game.Components` | `Unity.Mathematics` | externalName |
| `Game.Components` | `Unity.Transforms` | externalName |
| `Game.Composition` | `Unity.Collections` | externalName |
| `Game.Composition` | `Unity.Entities` | externalName |
| `Game.Composition` | `Unity.InputSystem` | externalName |
| `Game.Composition` | `Unity.Mathematics` | externalName |
| `Game.Composition` | `Unity.RenderPipelines.Core.Runtime` | externalName |
| `Game.Composition` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.Composition` | `Unity.TextMeshPro` | externalName |
| `Game.Composition` | `Unity.Transforms` | externalName |
| `Game.Composition` | `UnityEngine.UI` | externalName |
| `Game.Configs` | `Unity.Collections` | externalName |
| `Game.Configs` | `Unity.Entities` | externalName |
| `Game.Configs` | `Unity.Mathematics` | externalName |
| `Game.Configs` | `Unity.RenderPipelines.Core.Runtime` | externalName |
| `Game.Configs` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.Editor` | `Unity.Collections` | externalName |
| `Game.Editor` | `Unity.Entities` | externalName |
| `Game.Editor` | `Unity.Entities.Graphics` | externalName |
| `Game.Editor` | `Unity.Mathematics` | externalName |
| `Game.Editor` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Editor` | `Unity.TextMeshPro` | externalName |
| `Game.Editor` | `Unity.Transforms` | externalName |
| `Game.Editor` | `UnityEngine.UI` | externalName |
| `Game.Editor` | `sniveler-code.gpu-animation` | externalName |
| `Game.Editor` | `sniveler-code.gpu-animation.Editor` | externalName |
| `Game.Rendering` | `Unity.Burst` | externalName |
| `Game.Rendering` | `Unity.Collections` | externalName |
| `Game.Rendering` | `Unity.Entities` | externalName |
| `Game.Rendering` | `Unity.Entities.Graphics` | externalName |
| `Game.Rendering` | `Unity.Mathematics` | externalName |
| `Game.Rendering` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Rendering` | `Unity.Transforms` | externalName |
| `Game.Rendering` | `sniveler-code.gpu-animation` | externalName |
| `Game.Rendering.Contracts` | `Unity.Collections` | externalName |
| `Game.Runtime` | `Unity.Burst` | externalName |
| `Game.Runtime` | `Unity.Collections` | externalName |
| `Game.Runtime` | `Unity.Entities` | externalName |
| `Game.Runtime` | `Unity.Entities.Graphics` | externalName |
| `Game.Runtime` | `Unity.InputSystem` | externalName |
| `Game.Runtime` | `Unity.Mathematics` | externalName |
| `Game.Runtime` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Runtime` | `Unity.RenderPipelines.Core.Runtime` | externalName |
| `Game.Runtime` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.Runtime` | `Unity.Transforms` | externalName |
| `Game.Runtime` | `sniveler-code.gpu-animation` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Collections` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Entities` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Mathematics` | externalName |
| `Game.Tests.Editor` | `Unity.Collections` | externalName |
| `Game.Tests.Editor` | `Unity.Entities` | externalName |
| `Game.Tests.Editor` | `Unity.Entities.Graphics` | externalName |
| `Game.Tests.Editor` | `Unity.Mathematics` | externalName |
| `Game.Tests.Editor` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Tests.Editor` | `Unity.TextMeshPro` | externalName |
| `Game.Tests.Editor` | `Unity.Transforms` | externalName |
| `Game.Tests.Editor` | `UnityEngine.UI` | externalName |
| `Game.Tests.Editor` | `sniveler-code.gpu-animation` | externalName |
| `Game.Tests.PlayMode` | `Unity.Collections` | externalName |
| `Game.Tests.PlayMode` | `Unity.Entities` | externalName |
| `Game.Tests.PlayMode` | `Unity.Mathematics` | externalName |
| `Game.Tests.PlayMode` | `Unity.Transforms` | externalName |
| `Game.UI.Runtime` | `Unity.InputSystem` | externalName |
| `Game.UI.Runtime` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.UI.Runtime` | `Unity.TextMeshPro` | externalName |
| `Game.UI.Runtime` | `UnityEngine.UI` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Collections` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Entities` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Collections` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Entities` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Mathematics` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Transforms` | externalName |
| `ProjectTools.Editor` | `Unity.ProBuilder` | externalName |
| `ProjectTools.Editor` | `Unity.ProBuilder.Editor` | externalName |

## Measurement Boundaries

- First-party scope is path-owned: asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.
- Type-reference counts are deterministic source-level lexical resolutions in explicit type contexts against direct first-party asmdef dependencies; they are not compiler symbol counts.
- Comments and string/character literal contents are excluded. Interpolated-string expressions are excluded with their containing strings.
- Ambiguous simple names are counted in the summary and omitted instead of being assigned heuristically.
- Top-level declarations and public nested class, struct, interface, enum, record, and delegate declarations are indexed; generated code outside the scoped roots is excluded.
- Member-access-only and semantically ambiguous parenthesized identifier uses are omitted; syntactically anchored casts and generic type expressions are included.
- First-party .asmref files are rejected with a fail-closed unsupported-condition error until ownership resolution is implemented.
