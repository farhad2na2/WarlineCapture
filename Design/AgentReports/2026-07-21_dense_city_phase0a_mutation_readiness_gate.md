# Dense City Phase 0A Mutation Readiness Gate

## Scope

- Non-mutating gate over completed Phase 0A inventories/classification.
- API: `OperationMapEntityPresentationMigrationEditor.TryEvaluateMutationReadiness`
- Tests: `OperationMapEntityPresentationMutationReadinessTests` + `OperationMapPresentationKindContractTests`

## Live evidence evaluation

| Input | Value |
|---|---:|
| GameplayBuilding | 432 |
| GameplayVehicle | 22 |
| RenderOnlyEntity | 9,090 |
| RejectedUnresolved | 0 |
| Vehicle already ready | 22 |
| Vehicle cleanup required | 0 |
| Attachment orphans / shared / dual-state | 0 / 0 / 0 |

Result: `CandidateTransactionReadyPendingMutation`

## Scaffolding already in tree (fail-closed)

- `OperationMapPresentationKind` + `EntityScene` content-reference validation
- `OperationMapCanonicalPresentationMode.EntityScene` rejected by SceneView / StaticMapPresentationOwnership until accepted migration
- Immutable `OperationMapEntityPresentationMigrationRecord` + dry-run planner
- `OperationMapEntityPresentationRootAuthoring` type (SubScene roots not created yet)

## Still GPT-only

1. Create candidate SubScene protected hierarchy roots (first ownership mutation)
2. Copy/migrate render-only + gameplay owners into candidate SubScene
3. Building ECS conversion replacing managed `RuntimeBuildingEntity`
4. Flip production definition / canonical mode to `EntityScene` after Editor + Android acceptance
