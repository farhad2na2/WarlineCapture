Lane: Gameplay

Task: Step 23-25 - delete `BuildingPlacementSystem` facade, remove temporary architecture allowances, and run focused validation.

Files changed:
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs` deleted.
- `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs.meta` deleted.
- `Assets/Tests/Editor/GameplayArchitectureContractTests.cs`
- `Design/Architecture/buildingplacement_retirement_audit.md`
- `Design/Architecture/gameplay_solid_ecs_contract.md`

Contracts touched:
- Building domain architecture contract now states `BuildingPlacementSystem` must not exist.
- Retirement audit moved from migration inventory/drift guard to completed deletion record.
- Architecture tests now reject the facade file and exact production/test type references.

User-visible behavior:
- No intended gameplay behavior change. Runtime composition continues through `BuildingGameplaySystem` and narrow building systems.

Validation run:
- `git diff --check`
- `rg --pcre2 -n "\bBuildingPlacementSystem\b(?!Config)" Assets/Game/Scripts -g '*.cs'`
- `rg --pcre2 -n "\bBuildingPlacementSystem\b(?!Config)" Assets/Tests -g '*.cs' --glob '!GameplayArchitectureContractTests.cs'`
- Unity EditMode: `GameplayArchitectureContractTests`
- Unity EditMode: `BuildingRuntimeBoundaryValidationTests.RuntimeSpawnRequestCompletionSurvivesSpawnStructuralChanges`
- Unity PlayMode: `BootstrapAndMenuPlayModeTests.GameBootstrap_AwakeDoesNotInitializeGameplayBeforePlayRequest`
- Unity PlayMode graphics-enabled: `GameSceneIsolationPlayModeTests.GameScene_PlayUsesPromotedDefaultCanvasWithoutOld2DRoute`
- Unity EditMode: `AIEndToEndValidationTests`
- Attempted Unity PlayMode graphics-enabled: `GameSceneTransportBoardingPlayModeTests.GameScene_NearbySoldierClickingTransportHelipadArea_WalksAndBoards`

Validation result:
- Passed: diff check.
- Passed: no exact production facade references outside config asset type names.
- Passed: no exact test facade references outside the architecture guard file and config asset type names.
- Passed: `GameplayArchitectureContractTests` 97/97.
- Passed: building runtime boundary spawn request validation 1/1.
- Passed: bootstrap/menu PlayMode smoke 1/1.
- Passed: Game scene load/play smoke 1/1 with graphics enabled.
- Passed: AI end-to-end validation 1/1, covering ECS boundary building/unit production flow.
- Failed: transport boarding PlayMode validation did not find the expected initial transport helicopter in the validation clone after syncing scripts/tests/scenes/configs/prefabs.

Known gaps:
- The transport-specific runtime spawn validation remains red because the current validation clone `Game` scene does not spawn `Unit_Veh_Helicopter_Transport` for that test setup. This should be investigated separately from facade deletion if the transport scenario is still required as the canonical runtime spawn proof.
- `GameplayArchitectureContractTests.cs` still contains many guard strings naming the retired facade so it can reject old patterns. Production scripts and non-architecture tests do not reference the exact facade type.

Cross-lane impacts:
- Other lanes must not recreate `Assets/Game/Scripts/UI/BuildingPlacementSystem.cs`.
- Config asset type names `BuildingPlacementSystemConfig` and `BuildingPlacementSystemSceneConfigAsset` remain valid and are explicitly allowed.

Next recommended task:
- Decide whether `GameSceneTransportBoardingPlayModeTests.GameScene_NearbySoldierClickingTransportHelipadArea_WalksAndBoards` should be updated to the current default `Game` scene spawn contract or whether the scene should restore the expected transport helicopter spawn.
