# APH-700 First-Party Assembly Dependency Report

- Task: `APH-700`
- Exact commit: `9a0aa14252e6559680328e520d26c16bfc7b444e`
- Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
- Dirty at capture start: `false`
- Source fingerprint (SHA-256): `7b9abb5fb16999ed458dbddda8af851f30a0697cf8d774773b383f66da268200`
- Determinism: Explicit evidence identity, no timestamp, ordinal path ordering, normalized LF output, and a content-derived source fingerprint.
- Scope: Direct dependencies and source-level cross-domain type references for first-party asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.

## Summary

| Metric | Count |
|---|---:|
| First-party assemblies | 21 |
| First-party asmdef edges | 92 |
| External declared references | 102 |
| Owned C# source files | 1436 |
| Indexed visible types | 2877 |
| Resolved cross-domain type occurrences | 34599 |
| Distinct cross-domain type references | 3069 |
| Ambiguous type tokens omitted | 70 |
| Unowned scoped C# source files | 0 |

## First-Party Assemblies

| Assembly | asmdef | Sources | Types | First-party edges | External refs |
|---|---|---:|---:|---:|---:|
| `Game.Authoring` | `Assets/Game/Scripts/Authorings/Game.Authoring.asmdef` | 15 | 17 | 2 | 5 |
| `Game.Catalog.Contracts` | `Assets/Game/Scripts/Catalog/Contracts/Game.Catalog.Contracts.asmdef` | 3 | 5 | 0 | 0 |
| `Game.Components` | `Assets/Game/Scripts/Components/Game.Components.asmdef` | 56 | 536 | 0 | 5 |
| `Game.Composition` | `Assets/Game/Scripts/Composition/Game.Composition.asmdef` | 48 | 58 | 14 | 11 |
| `Game.Configs` | `Assets/Game/Scripts/Configs/Game.Configs.asmdef` | 54 | 142 | 3 | 6 |
| `Game.Editor` | `Assets/Game/Scripts/Editor/Game.Editor.asmdef` | 124 | 168 | 15 | 14 |
| `Game.Narrative.Contracts` | `Assets/Game/Scripts/Narrative/Contracts/Game.Narrative.Contracts.asmdef` | 1 | 6 | 0 | 0 |
| `Game.Narrative.Runtime` | `Assets/Game/Scripts/Narrative/Runtime/Game.Narrative.Runtime.asmdef` | 3 | 9 | 2 | 0 |
| `Game.Rendering` | `Assets/Game/Scripts/Rendering/Game.Rendering.asmdef` | 45 | 73 | 3 | 8 |
| `Game.Rendering.Contracts` | `Assets/Game/Scripts/Rendering/Contracts/Game.Rendering.Contracts.asmdef` | 2 | 3 | 0 | 1 |
| `Game.Runtime` | `Assets/Game/Scripts/Game.Runtime.asmdef` | 548 | 1138 | 7 | 11 |
| `Game.Runtime.Combat` | `Assets/Game/Scripts/Systems/Combat/Game.Runtime.Combat.asmdef` | 1 | 2 | 1 | 3 |
| `Game.Runtime.Pathfinding` | `Assets/Game/Scripts/Systems/Pathfinding/Surface/Game.Runtime.Pathfinding.asmdef` | 4 | 4 | 1 | 3 |
| `Game.Tactical.Contracts` | `Assets/Game/Scripts/Contracts/Game.Tactical.Contracts.asmdef` | 1 | 9 | 0 | 0 |
| `Game.Tests.Editor` | `Assets/Tests/Editor/Game.Tests.Editor.asmdef` | 304 | 310 | 18 | 12 |
| `Game.Tests.PlayMode` | `Assets/Tests/PlayMode/Game.Tests.PlayMode.asmdef` | 13 | 13 | 12 | 8 |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef` | 22 | 118 | 1 | 0 |
| `Game.UI.Runtime` | `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef` | 144 | 189 | 4 | 6 |
| `Game.UI.Shell.Contracts.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef` | 1 | 39 | 1 | 2 |
| `Game.UI.Shell.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef` | 44 | 35 | 8 | 5 |
| `ProjectTools.Editor` | `Assets/Editor/ProjectTools.Editor.asmdef` | 3 | 3 | 0 | 2 |

## Every First-Party Assembly Edge

| Source | Target | Type occurrences | Distinct types | Source files |
|---|---|---:|---:|---:|
| `Game.Authoring` | `Game.Components` | 204 | 105 | 15 |
| `Game.Authoring` | `Game.Configs` | 31 | 18 | 10 |
| `Game.Composition` | `Game.Authoring` | 36 | 5 | 14 |
| `Game.Composition` | `Game.Catalog.Contracts` | 1 | 1 | 1 |
| `Game.Composition` | `Game.Components` | 211 | 58 | 8 |
| `Game.Composition` | `Game.Configs` | 172 | 49 | 18 |
| `Game.Composition` | `Game.Narrative.Contracts` | 39 | 6 | 7 |
| `Game.Composition` | `Game.Narrative.Runtime` | 22 | 6 | 2 |
| `Game.Composition` | `Game.Rendering` | 34 | 9 | 6 |
| `Game.Composition` | `Game.Rendering.Contracts` | 7 | 2 | 1 |
| `Game.Composition` | `Game.Runtime` | 194 | 59 | 15 |
| `Game.Composition` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.Composition` | `Game.UI.Contracts` | 106 | 43 | 12 |
| `Game.Composition` | `Game.UI.Runtime` | 73 | 22 | 14 |
| `Game.Composition` | `Game.UI.Shell.Contracts.Ecs` | 50 | 12 | 4 |
| `Game.Composition` | `Game.UI.Shell.Ecs` | 2 | 2 | 1 |
| `Game.Configs` | `Game.Catalog.Contracts` | 12 | 5 | 4 |
| `Game.Configs` | `Game.Components` | 97 | 22 | 7 |
| `Game.Configs` | `Game.Narrative.Contracts` | 2 | 1 | 1 |
| `Game.Editor` | `Game.Authoring` | 156 | 10 | 26 |
| `Game.Editor` | `Game.Catalog.Contracts` | 9 | 4 | 1 |
| `Game.Editor` | `Game.Components` | 684 | 127 | 24 |
| `Game.Editor` | `Game.Composition` | 141 | 4 | 25 |
| `Game.Editor` | `Game.Configs` | 351 | 55 | 40 |
| `Game.Editor` | `Game.Narrative.Contracts` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Rendering` | 38 | 4 | 5 |
| `Game.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Runtime` | 230 | 49 | 23 |
| `Game.Editor` | `Game.Runtime.Pathfinding` | 6 | 2 | 4 |
| `Game.Editor` | `Game.Tactical.Contracts` | 6 | 1 | 2 |
| `Game.Editor` | `Game.UI.Contracts` | 31 | 12 | 7 |
| `Game.Editor` | `Game.UI.Runtime` | 329 | 57 | 25 |
| `Game.Editor` | `Game.UI.Shell.Contracts.Ecs` | 83 | 5 | 7 |
| `Game.Editor` | `Game.UI.Shell.Ecs` | 1 | 1 | 1 |
| `Game.Narrative.Runtime` | `Game.Catalog.Contracts` | 3 | 1 | 2 |
| `Game.Narrative.Runtime` | `Game.Narrative.Contracts` | 2 | 2 | 1 |
| `Game.Rendering` | `Game.Components` | 514 | 72 | 31 |
| `Game.Rendering` | `Game.Configs` | 19 | 6 | 5 |
| `Game.Rendering` | `Game.Rendering.Contracts` | 2 | 2 | 2 |
| `Game.Runtime` | `Game.Components` | 10450 | 425 | 372 |
| `Game.Runtime` | `Game.Configs` | 318 | 64 | 52 |
| `Game.Runtime` | `Game.Rendering.Contracts` | 3 | 2 | 1 |
| `Game.Runtime` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Runtime` | `Game.Runtime.Pathfinding` | 10 | 4 | 6 |
| `Game.Runtime` | `Game.Tactical.Contracts` | 153 | 3 | 30 |
| `Game.Runtime` | `Game.UI.Contracts` | 136 | 17 | 22 |
| `Game.Runtime.Combat` | `Game.Components` | 14 | 3 | 1 |
| `Game.Runtime.Pathfinding` | `Game.Components` | 29 | 11 | 4 |
| `Game.Tests.Editor` | `Game.Authoring` | 61 | 3 | 13 |
| `Game.Tests.Editor` | `Game.Catalog.Contracts` | 3 | 3 | 3 |
| `Game.Tests.Editor` | `Game.Components` | 10538 | 398 | 171 |
| `Game.Tests.Editor` | `Game.Composition` | 164 | 24 | 23 |
| `Game.Tests.Editor` | `Game.Configs` | 685 | 76 | 61 |
| `Game.Tests.Editor` | `Game.Editor` | 141 | 31 | 18 |
| `Game.Tests.Editor` | `Game.Narrative.Contracts` | 14 | 5 | 4 |
| `Game.Tests.Editor` | `Game.Narrative.Runtime` | 35 | 6 | 2 |
| `Game.Tests.Editor` | `Game.Rendering` | 275 | 49 | 14 |
| `Game.Tests.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Runtime` | 2855 | 391 | 152 |
| `Game.Tests.Editor` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Runtime.Pathfinding` | 9 | 2 | 2 |
| `Game.Tests.Editor` | `Game.Tactical.Contracts` | 105 | 7 | 6 |
| `Game.Tests.Editor` | `Game.UI.Contracts` | 311 | 70 | 31 |
| `Game.Tests.Editor` | `Game.UI.Runtime` | 741 | 86 | 34 |
| `Game.Tests.Editor` | `Game.UI.Shell.Contracts.Ecs` | 472 | 31 | 29 |
| `Game.Tests.Editor` | `Game.UI.Shell.Ecs` | 71 | 19 | 24 |
| `Game.Tests.PlayMode` | `Game.Authoring` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Components` | 541 | 100 | 10 |
| `Game.Tests.PlayMode` | `Game.Composition` | 13 | 3 | 2 |
| `Game.Tests.PlayMode` | `Game.Configs` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Narrative.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Runtime` | 125 | 53 | 11 |
| `Game.Tests.PlayMode` | `Game.Tactical.Contracts` | 2 | 1 | 1 |
| `Game.Tests.PlayMode` | `Game.UI.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.UI.Runtime` | 7 | 4 | 3 |
| `Game.Tests.PlayMode` | `Game.UI.Shell.Ecs` | 0 | 0 | 0 |
| `Game.UI.Contracts` | `Game.Tactical.Contracts` | 22 | 7 | 6 |
| `Game.UI.Runtime` | `Game.Catalog.Contracts` | 44 | 4 | 8 |
| `Game.UI.Runtime` | `Game.Narrative.Contracts` | 6 | 1 | 1 |
| `Game.UI.Runtime` | `Game.Tactical.Contracts` | 73 | 7 | 10 |
| `Game.UI.Runtime` | `Game.UI.Contracts` | 526 | 92 | 57 |
| `Game.UI.Shell.Contracts.Ecs` | `Game.UI.Contracts` | 29 | 16 | 1 |
| `Game.UI.Shell.Ecs` | `Game.Catalog.Contracts` | 4 | 1 | 1 |
| `Game.UI.Shell.Ecs` | `Game.Components` | 871 | 103 | 31 |
| `Game.UI.Shell.Ecs` | `Game.Configs` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Runtime` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Tactical.Contracts` | 13 | 2 | 3 |
| `Game.UI.Shell.Ecs` | `Game.UI.Contracts` | 267 | 60 | 20 |
| `Game.UI.Shell.Ecs` | `Game.UI.Runtime` | 34 | 7 | 7 |
| `Game.UI.Shell.Ecs` | `Game.UI.Shell.Contracts.Ecs` | 531 | 39 | 36 |

## Top Cross-Domain Type References

| Rank | Source | Target | Type | Occurrences | Source files |
|---:|---|---|---|---:|---:|
| 1 | `Game.Runtime` | `Game.Components` | `Game.Components.GridConfig` | 701 | 140 |
| 2 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitGrid` | 382 | 87 |
| 3 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.Faction` | 380 | 57 |
| 4 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitGrid` | 377 | 51 |
| 5 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 344 | 16 |
| 6 | `Game.Runtime` | `Game.Components` | `Game.Components.Faction` | 321 | 75 |
| 7 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RuntimeBuildingEntity` | 302 | 17 |
| 8 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitHealth` | 295 | 50 |
| 9 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitHealth` | 277 | 64 |
| 10 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridConfig` | 271 | 51 |
| 11 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 269 | 18 |
| 12 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitFootprint` | 231 | 56 |
| 13 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitFootprint` | 205 | 34 |
| 14 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMove` | 200 | 26 |
| 15 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTransportPassengerElement` | 200 | 6 |
| 16 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.ResourceExchangeQueueComponent` | 196 | 17 |
| 17 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandResultElement` | 185 | 15 |
| 18 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 179 | 22 |
| 19 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.EngageTarget` | 176 | 28 |
| 20 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.SelectedUnitTag` | 175 | 25 |
| 21 | `Game.Runtime` | `Game.Components` | `Game.Components.GridWalkable` | 174 | 41 |
| 22 | `Game.Runtime` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 165 | 58 |
| 23 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.FactionEconomy` | 164 | 33 |
| 24 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridWalkable` | 164 | 24 |
| 25 | `Game.Runtime` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 162 | 28 |
| 26 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RuntimeGameplayStateComponent` | 153 | 7 |
| 27 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.FactionTacticalMaterialsComponent` | 143 | 30 |
| 28 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitPathRequest` | 142 | 49 |
| 29 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitAirComponent` | 140 | 15 |
| 30 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTarget` | 139 | 21 |
| 31 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitPathRequest` | 138 | 22 |
| 32 | `Game.Runtime` | `Game.Components` | `Game.Components.FactionEconomy` | 136 | 23 |
| 33 | `Game.Runtime` | `Game.Components` | `Game.Components.EngageTarget` | 132 | 42 |
| 34 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitTarget` | 131 | 43 |
| 35 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionInputStateComponent` | 124 | 18 |
| 36 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.AudioPlaybackRequestElement` | 123 | 17 |
| 37 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMovementBehavior` | 118 | 25 |
| 38 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitAirComponent` | 114 | 27 |
| 39 | `Game.Runtime` | `Game.Components` | `Game.Components.FactionTacticalMaterialsComponent` | 114 | 20 |
| 40 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitAirMovement` | 112 | 57 |
| 41 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 112 | 24 |
| 42 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitSourcePrefabKey` | 108 | 39 |
| 43 | `Game.Runtime` | `Game.Components` | `Game.Components.GridRoad` | 107 | 38 |
| 44 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RtsSelectionInputCompositionSystemHelper` | 107 | 7 |
| 45 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitCombat` | 105 | 24 |
| 46 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionInputStateComponent` | 103 | 10 |
| 47 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.BuildingDefinition` | 102 | 18 |
| 48 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionCommandResultElement` | 102 | 11 |
| 49 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitTransportPassengerElement` | 100 | 17 |
| 50 | `Game.Runtime` | `Game.Components` | `Game.Components.RuntimeGameplayStateComponent` | 98 | 27 |

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
| `Game.Composition` | `Unity.Addressables` | externalName |
| `Game.Composition` | `Unity.Collections` | externalName |
| `Game.Composition` | `Unity.Entities` | externalName |
| `Game.Composition` | `Unity.InputSystem` | externalName |
| `Game.Composition` | `Unity.Mathematics` | externalName |
| `Game.Composition` | `Unity.RenderPipelines.Core.Runtime` | externalName |
| `Game.Composition` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.Composition` | `Unity.Scenes` | externalName |
| `Game.Composition` | `Unity.TextMeshPro` | externalName |
| `Game.Composition` | `Unity.Transforms` | externalName |
| `Game.Composition` | `UnityEngine.UI` | externalName |
| `Game.Configs` | `Unity.Addressables` | externalName |
| `Game.Configs` | `Unity.Collections` | externalName |
| `Game.Configs` | `Unity.Entities` | externalName |
| `Game.Configs` | `Unity.Mathematics` | externalName |
| `Game.Configs` | `Unity.RenderPipelines.Core.Runtime` | externalName |
| `Game.Configs` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.Editor` | `Unity.Addressables` | externalName |
| `Game.Editor` | `Unity.Addressables.Editor` | externalName |
| `Game.Editor` | `Unity.Collections` | externalName |
| `Game.Editor` | `Unity.Entities` | externalName |
| `Game.Editor` | `Unity.Entities.Graphics` | externalName |
| `Game.Editor` | `Unity.Mathematics` | externalName |
| `Game.Editor` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Editor` | `Unity.ResourceManager` | externalName |
| `Game.Editor` | `Unity.Scenes` | externalName |
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
| `Game.Runtime.Combat` | `Unity.Collections` | externalName |
| `Game.Runtime.Combat` | `Unity.Entities` | externalName |
| `Game.Runtime.Combat` | `Unity.Mathematics` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Collections` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Entities` | externalName |
| `Game.Runtime.Pathfinding` | `Unity.Mathematics` | externalName |
| `Game.Tests.Editor` | `Unity.Addressables` | externalName |
| `Game.Tests.Editor` | `Unity.Addressables.Editor` | externalName |
| `Game.Tests.Editor` | `Unity.Collections` | externalName |
| `Game.Tests.Editor` | `Unity.Entities` | externalName |
| `Game.Tests.Editor` | `Unity.Entities.Graphics` | externalName |
| `Game.Tests.Editor` | `Unity.Mathematics` | externalName |
| `Game.Tests.Editor` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Tests.Editor` | `Unity.ResourceManager` | externalName |
| `Game.Tests.Editor` | `Unity.TextMeshPro` | externalName |
| `Game.Tests.Editor` | `Unity.Transforms` | externalName |
| `Game.Tests.Editor` | `UnityEngine.UI` | externalName |
| `Game.Tests.Editor` | `sniveler-code.gpu-animation` | externalName |
| `Game.Tests.PlayMode` | `Unity.Addressables` | externalName |
| `Game.Tests.PlayMode` | `Unity.Collections` | externalName |
| `Game.Tests.PlayMode` | `Unity.Entities` | externalName |
| `Game.Tests.PlayMode` | `Unity.Mathematics` | externalName |
| `Game.Tests.PlayMode` | `Unity.ResourceManager` | externalName |
| `Game.Tests.PlayMode` | `Unity.TextMeshPro` | externalName |
| `Game.Tests.PlayMode` | `Unity.Transforms` | externalName |
| `Game.Tests.PlayMode` | `UnityEngine.UI` | externalName |
| `Game.UI.Runtime` | `Unity.Addressables` | externalName |
| `Game.UI.Runtime` | `Unity.InputSystem` | externalName |
| `Game.UI.Runtime` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.UI.Runtime` | `Unity.ResourceManager` | externalName |
| `Game.UI.Runtime` | `Unity.TextMeshPro` | externalName |
| `Game.UI.Runtime` | `UnityEngine.UI` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Collections` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Entities` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Burst` | externalName |
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
