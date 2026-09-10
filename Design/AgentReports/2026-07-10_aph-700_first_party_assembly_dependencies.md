# APH-700 First-Party Assembly Dependency Report

- Task: `APH-700`
- Exact commit: `eba47c95f5a9309b2f9124b0594452fd2914fd1e`
- Environment identity SHA-256: `1750156ad389d4f28a392531d19339a96140da898d5c2dfd1920c38d6486239e`
- Dirty at capture start: `true`
- Source fingerprint (SHA-256): `05b89630f39a954a702b7b29a0c80977fc4362586d7946ef65730446de9115f0`
- Determinism: Explicit evidence identity, no timestamp, ordinal path ordering, normalized LF output, and a content-derived source fingerprint.
- Scope: Direct dependencies and source-level cross-domain type references for first-party asmdefs under Assets/Game, Assets/Tests, and Assets/Editor.

## Summary

| Metric | Count |
|---|---:|
| First-party assemblies | 23 |
| First-party asmdef edges | 132 |
| External declared references | 122 |
| Owned C# source files | 2277 |
| Indexed visible types | 3889 |
| Resolved cross-domain type occurrences | 50722 |
| Distinct cross-domain type references | 4386 |
| Ambiguous type tokens omitted | 112 |
| Unowned scoped C# source files | 0 |

## First-Party Assemblies

| Assembly | asmdef | Sources | Types | First-party edges | External refs |
|---|---|---:|---:|---:|---:|
| `Game.Authoring` | `Assets/Game/Scripts/Authorings/Game.Authoring.asmdef` | 35 | 38 | 6 | 8 |
| `Game.Catalog.Contracts` | `Assets/Game/Scripts/Catalog/Contracts/Game.Catalog.Contracts.asmdef` | 3 | 5 | 0 | 0 |
| `Game.Components` | `Assets/Game/Scripts/Components/Game.Components.asmdef` | 68 | 652 | 2 | 5 |
| `Game.Composition` | `Assets/Game/Scripts/Composition/Game.Composition.asmdef` | 83 | 104 | 17 | 12 |
| `Game.Configs` | `Assets/Game/Scripts/Configs/Game.Configs.asmdef` | 87 | 204 | 5 | 6 |
| `Game.Editor` | `Assets/Game/Scripts/Editor/Game.Editor.asmdef` | 340 | 463 | 19 | 20 |
| `Game.Missions.Contracts` | `Assets/Game/Scripts/Missions/Contracts/Game.Missions.Contracts.asmdef` | 2 | 15 | 1 | 0 |
| `Game.Narrative.Contracts` | `Assets/Game/Scripts/Narrative/Contracts/Game.Narrative.Contracts.asmdef` | 2 | 7 | 0 | 0 |
| `Game.Narrative.Runtime` | `Assets/Game/Scripts/Narrative/Runtime/Game.Narrative.Runtime.asmdef` | 3 | 9 | 2 | 0 |
| `Game.Rendering` | `Assets/Game/Scripts/Rendering/Game.Rendering.asmdef` | 62 | 111 | 7 | 9 |
| `Game.Rendering.Contracts` | `Assets/Game/Scripts/Rendering/Contracts/Game.Rendering.Contracts.asmdef` | 3 | 5 | 0 | 2 |
| `Game.Runtime` | `Assets/Game/Scripts/Game.Runtime.asmdef` | 671 | 1200 | 10 | 10 |
| `Game.Runtime.Combat` | `Assets/Game/Scripts/Systems/Combat/Game.Runtime.Combat.asmdef` | 1 | 2 | 1 | 3 |
| `Game.Runtime.Pathfinding` | `Assets/Game/Scripts/Systems/Pathfinding/Surface/Game.Runtime.Pathfinding.asmdef` | 4 | 4 | 1 | 3 |
| `Game.Tactical.Contracts` | `Assets/Game/Scripts/Contracts/Game.Tactical.Contracts.asmdef` | 1 | 9 | 0 | 0 |
| `Game.Tests.Editor` | `Assets/Tests/Editor/Game.Tests.Editor.asmdef` | 515 | 527 | 20 | 14 |
| `Game.Tests.PlayMode` | `Assets/Tests/PlayMode/Game.Tests.PlayMode.asmdef` | 32 | 32 | 18 | 12 |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef` | 34 | 157 | 1 | 0 |
| `Game.UI.Runtime` | `Assets/Game/Scripts/UI/Game.UI.Runtime.asmdef` | 254 | 257 | 5 | 7 |
| `Game.UI.ScenarioLab.Runtime` | `Assets/Game/Scripts/UI/ScenarioLab/Game.UI.ScenarioLab.Runtime.asmdef` | 2 | 2 | 2 | 1 |
| `Game.UI.Shell.Contracts.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Contracts/Game.UI.Shell.Contracts.Ecs.asmdef` | 3 | 43 | 2 | 2 |
| `Game.UI.Shell.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef` | 69 | 40 | 13 | 6 |
| `ProjectTools.Editor` | `Assets/Editor/ProjectTools.Editor.asmdef` | 3 | 3 | 0 | 2 |

## Every First-Party Assembly Edge

| Source | Target | Type occurrences | Distinct types | Source files |
|---|---|---:|---:|---:|
| `Game.Authoring` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Authoring` | `Game.Components` | 306 | 155 | 27 |
| `Game.Authoring` | `Game.Configs` | 50 | 24 | 16 |
| `Game.Authoring` | `Game.Missions.Contracts` | 0 | 0 | 0 |
| `Game.Authoring` | `Game.Narrative.Contracts` | 0 | 0 | 0 |
| `Game.Authoring` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.Components` | `Game.Missions.Contracts` | 17 | 11 | 2 |
| `Game.Components` | `Game.Narrative.Contracts` | 3 | 1 | 1 |
| `Game.Composition` | `Game.Authoring` | 44 | 7 | 15 |
| `Game.Composition` | `Game.Catalog.Contracts` | 2 | 1 | 2 |
| `Game.Composition` | `Game.Components` | 464 | 112 | 28 |
| `Game.Composition` | `Game.Configs` | 331 | 73 | 41 |
| `Game.Composition` | `Game.Missions.Contracts` | 4 | 1 | 2 |
| `Game.Composition` | `Game.Narrative.Contracts` | 54 | 7 | 12 |
| `Game.Composition` | `Game.Narrative.Runtime` | 22 | 6 | 2 |
| `Game.Composition` | `Game.Rendering` | 53 | 11 | 13 |
| `Game.Composition` | `Game.Rendering.Contracts` | 7 | 2 | 1 |
| `Game.Composition` | `Game.Runtime` | 206 | 62 | 18 |
| `Game.Composition` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Composition` | `Game.Runtime.Pathfinding` | 0 | 0 | 0 |
| `Game.Composition` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.Composition` | `Game.UI.Contracts` | 135 | 52 | 20 |
| `Game.Composition` | `Game.UI.Runtime` | 84 | 24 | 16 |
| `Game.Composition` | `Game.UI.Shell.Contracts.Ecs` | 66 | 12 | 7 |
| `Game.Composition` | `Game.UI.Shell.Ecs` | 2 | 2 | 1 |
| `Game.Configs` | `Game.Catalog.Contracts` | 18 | 5 | 7 |
| `Game.Configs` | `Game.Components` | 139 | 29 | 9 |
| `Game.Configs` | `Game.Missions.Contracts` | 21 | 5 | 4 |
| `Game.Configs` | `Game.Narrative.Contracts` | 7 | 2 | 4 |
| `Game.Configs` | `Game.Tactical.Contracts` | 4 | 1 | 2 |
| `Game.Editor` | `Game.Authoring` | 598 | 27 | 64 |
| `Game.Editor` | `Game.Catalog.Contracts` | 15 | 4 | 4 |
| `Game.Editor` | `Game.Components` | 1405 | 200 | 89 |
| `Game.Editor` | `Game.Composition` | 301 | 7 | 59 |
| `Game.Editor` | `Game.Configs` | 979 | 87 | 114 |
| `Game.Editor` | `Game.Missions.Contracts` | 6 | 4 | 3 |
| `Game.Editor` | `Game.Narrative.Contracts` | 6 | 2 | 3 |
| `Game.Editor` | `Game.Narrative.Runtime` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Rendering` | 74 | 6 | 13 |
| `Game.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Runtime` | 430 | 83 | 51 |
| `Game.Editor` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Editor` | `Game.Runtime.Pathfinding` | 6 | 2 | 4 |
| `Game.Editor` | `Game.Tactical.Contracts` | 9 | 1 | 5 |
| `Game.Editor` | `Game.UI.Contracts` | 128 | 30 | 25 |
| `Game.Editor` | `Game.UI.Runtime` | 1859 | 136 | 92 |
| `Game.Editor` | `Game.UI.ScenarioLab.Runtime` | 22 | 2 | 3 |
| `Game.Editor` | `Game.UI.Shell.Contracts.Ecs` | 99 | 5 | 10 |
| `Game.Editor` | `Game.UI.Shell.Ecs` | 1 | 1 | 1 |
| `Game.Missions.Contracts` | `Game.Narrative.Contracts` | 2 | 1 | 1 |
| `Game.Narrative.Runtime` | `Game.Catalog.Contracts` | 3 | 1 | 2 |
| `Game.Narrative.Runtime` | `Game.Narrative.Contracts` | 2 | 2 | 1 |
| `Game.Rendering` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Rendering` | `Game.Components` | 862 | 114 | 47 |
| `Game.Rendering` | `Game.Configs` | 19 | 6 | 5 |
| `Game.Rendering` | `Game.Missions.Contracts` | 0 | 0 | 0 |
| `Game.Rendering` | `Game.Narrative.Contracts` | 0 | 0 | 0 |
| `Game.Rendering` | `Game.Rendering.Contracts` | 4 | 3 | 3 |
| `Game.Rendering` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.Runtime` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Runtime` | `Game.Components` | 12337 | 525 | 468 |
| `Game.Runtime` | `Game.Configs` | 321 | 58 | 56 |
| `Game.Runtime` | `Game.Missions.Contracts` | 55 | 8 | 13 |
| `Game.Runtime` | `Game.Narrative.Contracts` | 6 | 2 | 5 |
| `Game.Runtime` | `Game.Rendering.Contracts` | 3 | 2 | 1 |
| `Game.Runtime` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Runtime` | `Game.Runtime.Pathfinding` | 10 | 4 | 6 |
| `Game.Runtime` | `Game.Tactical.Contracts` | 155 | 3 | 32 |
| `Game.Runtime` | `Game.UI.Contracts` | 139 | 17 | 25 |
| `Game.Runtime.Combat` | `Game.Components` | 14 | 3 | 1 |
| `Game.Runtime.Pathfinding` | `Game.Components` | 29 | 11 | 4 |
| `Game.Tests.Editor` | `Game.Authoring` | 253 | 20 | 37 |
| `Game.Tests.Editor` | `Game.Catalog.Contracts` | 5 | 4 | 5 |
| `Game.Tests.Editor` | `Game.Components` | 14276 | 511 | 250 |
| `Game.Tests.Editor` | `Game.Composition` | 312 | 42 | 38 |
| `Game.Tests.Editor` | `Game.Configs` | 1637 | 123 | 126 |
| `Game.Tests.Editor` | `Game.Editor` | 770 | 128 | 69 |
| `Game.Tests.Editor` | `Game.Missions.Contracts` | 56 | 12 | 17 |
| `Game.Tests.Editor` | `Game.Narrative.Contracts` | 17 | 7 | 6 |
| `Game.Tests.Editor` | `Game.Narrative.Runtime` | 41 | 6 | 3 |
| `Game.Tests.Editor` | `Game.Rendering` | 457 | 70 | 30 |
| `Game.Tests.Editor` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Runtime` | 3660 | 458 | 203 |
| `Game.Tests.Editor` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.Runtime.Pathfinding` | 9 | 2 | 2 |
| `Game.Tests.Editor` | `Game.Tactical.Contracts` | 113 | 7 | 10 |
| `Game.Tests.Editor` | `Game.UI.Contracts` | 484 | 87 | 57 |
| `Game.Tests.Editor` | `Game.UI.Runtime` | 1406 | 139 | 73 |
| `Game.Tests.Editor` | `Game.UI.ScenarioLab.Runtime` | 0 | 0 | 0 |
| `Game.Tests.Editor` | `Game.UI.Shell.Contracts.Ecs` | 598 | 28 | 42 |
| `Game.Tests.Editor` | `Game.UI.Shell.Ecs` | 102 | 25 | 35 |
| `Game.Tests.PlayMode` | `Game.Authoring` | 2 | 2 | 1 |
| `Game.Tests.PlayMode` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Components` | 1213 | 181 | 22 |
| `Game.Tests.PlayMode` | `Game.Composition` | 60 | 10 | 7 |
| `Game.Tests.PlayMode` | `Game.Configs` | 31 | 6 | 4 |
| `Game.Tests.PlayMode` | `Game.Missions.Contracts` | 14 | 4 | 2 |
| `Game.Tests.PlayMode` | `Game.Narrative.Contracts` | 1 | 1 | 1 |
| `Game.Tests.PlayMode` | `Game.Narrative.Runtime` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Rendering` | 9 | 3 | 4 |
| `Game.Tests.PlayMode` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Runtime` | 213 | 76 | 22 |
| `Game.Tests.PlayMode` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Runtime.Pathfinding` | 0 | 0 | 0 |
| `Game.Tests.PlayMode` | `Game.Tactical.Contracts` | 2 | 1 | 1 |
| `Game.Tests.PlayMode` | `Game.UI.Contracts` | 5 | 5 | 2 |
| `Game.Tests.PlayMode` | `Game.UI.Runtime` | 15 | 6 | 6 |
| `Game.Tests.PlayMode` | `Game.UI.Shell.Contracts.Ecs` | 16 | 6 | 2 |
| `Game.Tests.PlayMode` | `Game.UI.Shell.Ecs` | 4 | 3 | 2 |
| `Game.UI.Contracts` | `Game.Tactical.Contracts` | 23 | 7 | 6 |
| `Game.UI.Runtime` | `Game.Catalog.Contracts` | 53 | 4 | 9 |
| `Game.UI.Runtime` | `Game.Missions.Contracts` | 0 | 0 | 0 |
| `Game.UI.Runtime` | `Game.Narrative.Contracts` | 10 | 2 | 2 |
| `Game.UI.Runtime` | `Game.Tactical.Contracts` | 99 | 7 | 18 |
| `Game.UI.Runtime` | `Game.UI.Contracts` | 673 | 122 | 99 |
| `Game.UI.ScenarioLab.Runtime` | `Game.Runtime` | 50 | 9 | 2 |
| `Game.UI.ScenarioLab.Runtime` | `Game.UI.Contracts` | 0 | 0 | 0 |
| `Game.UI.Shell.Contracts.Ecs` | `Game.Tactical.Contracts` | 0 | 0 | 0 |
| `Game.UI.Shell.Contracts.Ecs` | `Game.UI.Contracts` | 33 | 19 | 3 |
| `Game.UI.Shell.Ecs` | `Game.Catalog.Contracts` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Components` | 1137 | 142 | 56 |
| `Game.UI.Shell.Ecs` | `Game.Configs` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Missions.Contracts` | 3 | 3 | 2 |
| `Game.UI.Shell.Ecs` | `Game.Narrative.Contracts` | 7 | 2 | 2 |
| `Game.UI.Shell.Ecs` | `Game.Rendering.Contracts` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Runtime` | 21 | 3 | 2 |
| `Game.UI.Shell.Ecs` | `Game.Runtime.Combat` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Runtime.Pathfinding` | 0 | 0 | 0 |
| `Game.UI.Shell.Ecs` | `Game.Tactical.Contracts` | 13 | 2 | 4 |
| `Game.UI.Shell.Ecs` | `Game.UI.Contracts` | 299 | 81 | 31 |
| `Game.UI.Shell.Ecs` | `Game.UI.Runtime` | 17 | 5 | 5 |
| `Game.UI.Shell.Ecs` | `Game.UI.Shell.Contracts.Ecs` | 565 | 43 | 49 |

## Top Cross-Domain Type References

| Rank | Source | Target | Type | Occurrences | Source files |
|---:|---|---|---|---:|---:|
| 1 | `Game.Runtime` | `Game.Components` | `Game.Components.GridConfig` | 741 | 158 |
| 2 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitGrid` | 466 | 65 |
| 3 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.Faction` | 460 | 69 |
| 4 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitGrid` | 407 | 100 |
| 5 | `Game.Runtime` | `Game.Components` | `Game.Components.Faction` | 382 | 101 |
| 6 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitHealth` | 370 | 62 |
| 7 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 353 | 18 |
| 8 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RuntimeBuildingEntity` | 352 | 18 |
| 9 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitHealth` | 350 | 93 |
| 10 | `Game.Editor` | `Game.UI.Runtime` | `Game.UI.Runtime.V3GradientGraphic` | 346 | 32 |
| 11 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridConfig` | 342 | 64 |
| 12 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 284 | 22 |
| 13 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitFootprint` | 245 | 59 |
| 14 | `Game.Tests.Editor` | `Game.Configs` | `Game.Configs.OperationMapDefinition` | 244 | 36 |
| 15 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitFootprint` | 234 | 34 |
| 16 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMove` | 223 | 33 |
| 17 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTransportPassengerElement` | 222 | 6 |
| 18 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.CampaignMissionRuntimeComponent` | 206 | 36 |
| 19 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.SelectedUnitTag` | 202 | 30 |
| 20 | `Game.Runtime` | `Game.Components` | `Game.Components.GridWalkable` | 197 | 48 |
| 21 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.ResourceExchangeQueueComponent` | 197 | 17 |
| 22 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.EngageTarget` | 193 | 31 |
| 23 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.GridWalkable` | 193 | 25 |
| 24 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RuntimeGameplayStateComponent` | 187 | 14 |
| 25 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.RtsSelectionCommandResultElement` | 185 | 15 |
| 26 | `Game.Runtime` | `Game.Components` | `Game.Components.RtsSelectionCommandIntentRequestElement` | 179 | 22 |
| 27 | `Game.Runtime` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 174 | 62 |
| 28 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.FactionEconomy` | 174 | 33 |
| 29 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.FactionTacticalMaterialsComponent` | 170 | 33 |
| 30 | `Game.Editor` | `Game.UI.Runtime` | `Game.UI.Runtime.MainMenuV3SectionLayoutView` | 162 | 34 |
| 31 | `Game.Runtime` | `Game.Components` | `Game.Components.BuildingResourceStorageComponent` | 162 | 27 |
| 32 | `Game.Runtime` | `Game.Components` | `Game.Components.CampaignMissionRuntimeComponent` | 160 | 43 |
| 33 | `Game.Editor` | `Game.Configs` | `Game.Configs.OperationMapDefinition` | 154 | 32 |
| 34 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitPathRequest` | 150 | 56 |
| 35 | `Game.Runtime` | `Game.Components` | `Game.Components.EngageTarget` | 150 | 48 |
| 36 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitAirComponent` | 150 | 17 |
| 37 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.OperationMapBlob` | 147 | 24 |
| 38 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitPathRequest` | 145 | 23 |
| 39 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitTarget` | 143 | 22 |
| 40 | `Game.Runtime` | `Game.Components` | `Game.Components.FactionEconomy` | 142 | 24 |
| 41 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.DynamicBlockerComponent` | 139 | 27 |
| 42 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.CampaignMissionCatalogBlob` | 138 | 21 |
| 43 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.CampaignMissionAttemptFactsComponent` | 135 | 25 |
| 44 | `Game.Tests.Editor` | `Game.Runtime` | `Game.Runtime.RtsSelectionInputCompositionSystemHelper` | 134 | 7 |
| 45 | `Game.Runtime` | `Game.Components` | `Game.Components.UnitTarget` | 133 | 46 |
| 46 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitMovementBehavior` | 130 | 30 |
| 47 | `Game.Runtime` | `Game.Components` | `Game.Components.FactionTacticalMaterialsComponent` | 130 | 23 |
| 48 | `Game.Tests.Editor` | `Game.Configs` | `Game.Configs.ScenarioSetupConfig` | 130 | 22 |
| 49 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.AudioPlaybackRequestElement` | 130 | 17 |
| 50 | `Game.Tests.Editor` | `Game.Components` | `Game.Components.UnitCombat` | 129 | 28 |

## External Declared References

These are retained so every reference declared by a first-party asmdef remains auditable; they are not first-party domain edges.

| Source | Declared reference | Kind |
|---|---|---|
| `Game.Authoring` | `Unity.Collections` | externalName |
| `Game.Authoring` | `Unity.Entities` | externalName |
| `Game.Authoring` | `Unity.Entities.Graphics` | externalName |
| `Game.Authoring` | `Unity.Entities.Hybrid` | externalName |
| `Game.Authoring` | `Unity.Mathematics` | externalName |
| `Game.Authoring` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Authoring` | `Unity.RenderPipelines.Core.Runtime` | externalName |
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
| `Game.Composition` | `Unity.ResourceManager` | externalName |
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
| `Game.Editor` | `RTLTMPro` | externalName |
| `Game.Editor` | `Unity.Addressables` | externalName |
| `Game.Editor` | `Unity.Addressables.Editor` | externalName |
| `Game.Editor` | `Unity.Collections` | externalName |
| `Game.Editor` | `Unity.Entities` | externalName |
| `Game.Editor` | `Unity.Entities.Build` | externalName |
| `Game.Editor` | `Unity.Entities.Graphics` | externalName |
| `Game.Editor` | `Unity.Entities.Hybrid` | externalName |
| `Game.Editor` | `Unity.InputSystem` | externalName |
| `Game.Editor` | `Unity.Mathematics` | externalName |
| `Game.Editor` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Editor` | `Unity.RenderPipelines.Universal.Editor` | externalName |
| `Game.Editor` | `Unity.ResourceManager` | externalName |
| `Game.Editor` | `Unity.Scenes` | externalName |
| `Game.Editor` | `Unity.Scenes.Editor` | externalName |
| `Game.Editor` | `Unity.TextMeshPro` | externalName |
| `Game.Editor` | `Unity.Transforms` | externalName |
| `Game.Editor` | `UnityEngine.UI` | externalName |
| `Game.Editor` | `sniveler-code.gpu-animation` | externalName |
| `Game.Editor` | `sniveler-code.gpu-animation.Editor` | externalName |
| `Game.Rendering` | `Unity.Burst` | externalName |
| `Game.Rendering` | `Unity.Collections` | externalName |
| `Game.Rendering` | `Unity.Entities` | externalName |
| `Game.Rendering` | `Unity.Entities.Graphics` | externalName |
| `Game.Rendering` | `Unity.Entities.Hybrid` | externalName |
| `Game.Rendering` | `Unity.Mathematics` | externalName |
| `Game.Rendering` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Rendering` | `Unity.Transforms` | externalName |
| `Game.Rendering` | `sniveler-code.gpu-animation` | externalName |
| `Game.Rendering.Contracts` | `Unity.Collections` | externalName |
| `Game.Rendering.Contracts` | `Unity.Entities` | externalName |
| `Game.Runtime` | `Unity.Burst` | externalName |
| `Game.Runtime` | `Unity.Collections` | externalName |
| `Game.Runtime` | `Unity.Entities` | externalName |
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
| `Game.Tests.Editor` | `Unity.Scenes` | externalName |
| `Game.Tests.Editor` | `Unity.TextMeshPro` | externalName |
| `Game.Tests.Editor` | `Unity.Transforms` | externalName |
| `Game.Tests.Editor` | `UnityEngine.UI` | externalName |
| `Game.Tests.Editor` | `sniveler-code.gpu-animation` | externalName |
| `Game.Tests.Editor` | `sniveler-code.gpu-animation.Editor` | externalName |
| `Game.Tests.PlayMode` | `Unity.Addressables` | externalName |
| `Game.Tests.PlayMode` | `Unity.Collections` | externalName |
| `Game.Tests.PlayMode` | `Unity.Entities` | externalName |
| `Game.Tests.PlayMode` | `Unity.Entities.Graphics` | externalName |
| `Game.Tests.PlayMode` | `Unity.Mathematics` | externalName |
| `Game.Tests.PlayMode` | `Unity.Mathematics.Extensions` | externalName |
| `Game.Tests.PlayMode` | `Unity.ResourceManager` | externalName |
| `Game.Tests.PlayMode` | `Unity.Scenes` | externalName |
| `Game.Tests.PlayMode` | `Unity.TextMeshPro` | externalName |
| `Game.Tests.PlayMode` | `Unity.Transforms` | externalName |
| `Game.Tests.PlayMode` | `UnityEngine.UI` | externalName |
| `Game.Tests.PlayMode` | `sniveler-code.gpu-animation` | externalName |
| `Game.UI.Runtime` | `RTLTMPro` | externalName |
| `Game.UI.Runtime` | `Unity.Addressables` | externalName |
| `Game.UI.Runtime` | `Unity.InputSystem` | externalName |
| `Game.UI.Runtime` | `Unity.RenderPipelines.Universal.Runtime` | externalName |
| `Game.UI.Runtime` | `Unity.ResourceManager` | externalName |
| `Game.UI.Runtime` | `Unity.TextMeshPro` | externalName |
| `Game.UI.Runtime` | `UnityEngine.UI` | externalName |
| `Game.UI.ScenarioLab.Runtime` | `UnityEngine.UI` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Collections` | externalName |
| `Game.UI.Shell.Contracts.Ecs` | `Unity.Entities` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Burst` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Collections` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Entities` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Mathematics` | externalName |
| `Game.UI.Shell.Ecs` | `Unity.Transforms` | externalName |
| `Game.UI.Shell.Ecs` | `sniveler-code.gpu-animation` | externalName |
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
