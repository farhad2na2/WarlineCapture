# Operations P0 shared-seam blockers

Owner: Game PM / integration lead. P0 did not edit these files.

**2026-09-22 integration clarification:** the restrictions below describe the historical P0 scope, not a permanent exclusion from Operations. These shared changes are required work in [P4R: O001 player-ready implementation](O001_PLAYER_READY_IMPLEMENTATION.md). The integration owner coordinates and validates them; leaving them untouched cannot satisfy a shipping mission gate. This document records no implementation of those changes.

## Required existing-assembly references

`Game.Operations.Contracts` is a new no-engine assembly with empty `references`. ARCHITECTURE asks existing assemblies to take a directional reference to it. That is a shared asmdef edit and is blocked here.

| Assembly | Path | Why the reference is required |
|---|---|---|
| `Game.Configs` | `Assets/Game/Scripts/Configs/Game.Configs.asmdef` | ScriptableObject wrappers for Operations schemas and shared ID validation |
| `Game.Components` | `Assets/Game/Scripts/Components/Game.Components.asmdef` | Named enums on ECS components |
| `Game.Runtime` | `Assets/Game/Scripts/Game.Runtime.asmdef` | P1/P2 systems consume payloads |
| `Game.UI.Contracts` | `Assets/Game/Scripts/UI/Contracts/Game.UI.Contracts.asmdef` | Shipping `IUiOperationsGateway` |
| `Game.UI.Shell.Ecs` | `Assets/Game/Scripts/UI/Shell/Ecs/Game.UI.Shell.Ecs.asmdef` | Projection / ARIA intents |
| `Game.Composition` | `Assets/Game/Scripts/Composition/Game.Composition.asmdef` | Catalog and save composition |
| `Game.Editor` | `Assets/Game/Scripts/Editor/Game.Editor.asmdef` | Catalog builders |
| `Game.Tests.Editor` | `Assets/Tests/Editor/Game.Tests.Editor.asmdef` | Optional; P0 already has `Game.Operations.Tests.Editor` |

`Assets/Tests/Editor/ScriptArchitectureAlignmentContractTests.cs` allowlists first-party references for `Game.Runtime.*` splits. If a later runtime-domain assembly references `Game.Operations.Contracts`, Game PM must add that name to `RuntimeDomainAllowedGameAssemblyReferences`. That test file is shared.

## Shared source blockers

| File | Required change | Needed by |
|---|---|---|
| `OperationMapIdentityRules.cs` | Accept `opmap.operations.<slug>` and `scenario.operations.o###` without loosening arbitrary strings | Any authored Operations map/scenario asset |
| `SaveDataModel.cs` / `PlayerProfileSaveData` | Optional versioned `operations` envelope; migration must not reset Campaign or wallets | P1 profile transactions |
| `SaveService.cs` / `JsonSaveRepository.cs` | Revision-checked serialized profile commit | P1/P3 crash protocol |
| `MatchSceneView.OperationMapLaunch.cs` | Mode-tagged Operations launch dispatch and exclusivity | P3 |
| `UiShellEcsGateway.Mission.cs` | Operations gateway, not cached Campaign roots | P3 |
| Localization catalog | EN/FA keys for new actions/objectives | P4 content |
| `DistrictDetailActionsScreenView` | Binder to typed gateway; keep existing enum ordinals | P3 |

## Intentionally not stolen

P0 ships `IUiOperationsGateway` and `OperationsSaveData` inside `Game.Operations.Contracts` so later shared placement is a move/reference, not a rewrite. Dashboard aliases, Campaign `MissionContracts`, and Skirmish files stay untouched.
